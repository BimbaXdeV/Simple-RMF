using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Server;
using RMF_Server.Channels;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class NetworkEngine : BackgroundService
    {
        private readonly IConnectionListener _listener;
        private readonly IServerSessionManager _sessionManager;
        private readonly IChannelDispatcher _channelDispatcher;
        private readonly ITlsManager _tlsManager;
        private readonly IFirewall _firewall;
        private readonly ILogger<NetworkEngine> _logger;
        private readonly ConnectionConfig _connectionConfig;
        private readonly FirewallConfig _firewallConfig;
        private readonly ControllerConfig _controllerConfig;

        public NetworkEngine(
            IConnectionListener listener,
            IServerSessionManager sessionManager,
            IChannelDispatcher channelDispatcher,
            ITlsManager tlsManager,
            IFirewall firewall,
            ILogger<NetworkEngine> logger,
            ConnectionConfig connectionConfig,
            FirewallConfig firewallConfig,
            ControllerConfig controllerConfig
        )
        {
            this._listener = listener;
            this._sessionManager = sessionManager;
            this._channelDispatcher = channelDispatcher;
            this._tlsManager = tlsManager;
            this._firewall = firewall;
            this._logger = logger;
            this._connectionConfig = connectionConfig;
            this._firewallConfig = firewallConfig;
            this._controllerConfig = controllerConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            this._logger.LogInformation("Starting network server on {ListenerName}...", this._listener.GetType().Name);
            
            X509Certificate2 serverCertificate = this._tlsManager.GetOrCreateCertificate();

            this._firewall.TryLoadBlacklist();

            try
            {
                this._listener.Start();
                IPEndPoint listenedEndPoint = this._listener.ListenedEndPoint;
                this._logger.LogInformation("Server successfully started listening at {IpAddress}:{Port}", listenedEndPoint.Address, listenedEndPoint.Port);

                await Task.Yield();
                while (!token.IsCancellationRequested)
                {
                    INetworkConnection connection = await this._listener.AcceptConnectionAsync(token);
                    IPEndPoint ipEndPoint = connection.RemoteEndPoint;
                    string endPoint = ipEndPoint.ToString();

                    if (this._sessionManager.TotalConnections >= this._firewallConfig.MaxConnections)
                    {
                        this._logger.LogWarning("A client {EndPoint} attempted to connect to server with maximum capacity ({MaxConnectionsCount}), access denied", endPoint, this._firewallConfig.MaxConnections);
                        connection.Close();
                        continue;
                    }

                    if (ipEndPoint == null || string.IsNullOrEmpty(endPoint))
                    {
                        this._logger.LogWarning("Connection attempt from unknown address, access denied");
                        connection.Close();
                        continue;
                    }

                    if (this._firewall.IsBanned(ipEndPoint.Address.ToString()))
                    {
                        this._logger.LogWarning("A banned client {EndPoint} attempted to connect, access denied", endPoint);
                        connection.Close();
                        continue;
                    }

                    if (this._sessionManager.GetConnectionsFromIP(ipEndPoint.Address) >= this._firewallConfig.MaxConnectionsPerIP)
                    {
                        this._logger.LogWarning("A client {EndPoint} attempted to exceed the connection limit ({MaxConnectionsPerIp}) from a single IP address, access denied", endPoint, this._firewallConfig.MaxConnectionsPerIP);
                        connection.Close();
                        continue;
                    }
                    
                    SslStream sslStream = new(connection.GetNetworkStream(), false);
                    try
                    {
                        await sslStream.AuthenticateAsServerAsync(serverCertificate).WaitAsync(TimeSpan.FromSeconds(this._connectionConfig.ReceiveTimeoutSecs), token);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogCritical("TLS handshake failed with client {EndPoint}, disconnecting...\n{Exception}", endPoint, ex);
                        sslStream.Dispose();
                        continue;
                    }

                    SecureConnectionAdapter tlsConnection = new(connection, sslStream);
                    IServerClientSession? session = this._sessionManager.NewConnection(tlsConnection, token);
                    if (session == null)
                    {
                        this._logger.LogInformation("A duplicate connection to the server was detected, the duplicated client {EndPoint} was disconnected", endPoint);
                        connection.Close();
                        continue;
                    }

                    this._logger.LogInformation("Registered new connection from {EndPoint}", endPoint);

                    if (this._controllerConfig.EnableWelcomeHandshake)
                    {
                        DateTime connectionTime = DateTime.UtcNow;
                        HandshakePacket handshakePacket = new()
                        {
                            ConnectionTimestamp = new DateTimeOffset(connectionTime).ToUnixTimeMilliseconds(),
                            SessionID = this._sessionManager.GetSessionID(endPoint) ?? Guid.Empty,
                            RemoteIP = ipEndPoint.Address.ToString(),
                            RemotePort = ipEndPoint.Port,
                            SendBufferSize = session.SendBufferSize,
                            ReceiveBufferSize = session.ReceiveBufferSize
                        };
                        session.SendPacket(handshakePacket);
                    }

                    if (this._controllerConfig.EnableBuildComparison)
                    {
                        ClientVersionRequest versionRequest = new();
                        session.SendPacket(versionRequest);
                    }

                    if (this._controllerConfig.EnableCollectingClientInfo)
                    {
                        ClientInfoRequest clientInfoRequest = new();
                        session.SendPacket(clientInfoRequest);
                    }

                    if (this._controllerConfig.EnableClientHeartbeat)
                    {
                        session.StartEvent("HeartbeatEvent", new Dictionary<string, object>
                        {
                            { "IntervalSecs", this._controllerConfig.ClientHeartbeatIntervalSecs }
                        });
                    }

                    _ = Task.Factory.StartNew(() => ClientHandler(session, token), TaskCreationOptions.LongRunning);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this._logger.LogCritical("The server return an exception: {Exception}", ex);
            }
            finally
            {
                Shutdown();
            }
        }

        private async Task ClientHandler(IServerClientSession session, CancellationToken token)
        {
            CancellationTokenSource cts = new();

            try
            {
                while (session.IsRunning)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(this._connectionConfig.ReceiveTimeoutSecs));  // Time bomb :D

                    PacketHeader header = await session.ReadHeaderAsync(cts.Token);
                    if (session.IsRateLimitExceed(this._firewallConfig.MaxPacketRate))
                    {
                        this._logger.LogWarning("The client {EndPoint} has exceeded the allowed packet rate limit", session.RemoteEndPoint);
                        this._firewall.Ban(session.RemoteEndPoint.Address.ToString());
                        break;
                    }

                    // It is needed to save memory and reject a packet directly based on its ID.
                    // The channel ID to which the packet is routed is determined by the third digit of its ID.
                    // For example: 202 => (2)02 => 2
                    if (!this._channelDispatcher.IsChannelExists(header.Id / 100))
                    {
                        this._logger.LogWarning("Received a packet with unknown id \"{PacketId}\" from the client {EndPoint}", header.Id, session.RemoteEndPoint);
                        break;
                    }
                    byte[] payload = await session.ReadPayloadAsync(header.Length, token);

                    try
                    {
                        // The packet will be processed in the channel, so we can immediately start waiting for the next packet
                        // without worrying about the processing time of the current
                        PacketContext context = new(session.RemoteEndPoint, header.Id, header.Length, payload);
                        await this._channelDispatcher.EnqueuePacketAsync(context);
                        session.IncrementReceivedPackets();
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError("Fatal connection error when trying to handle incoming packet from {EndPoint}, disconnecting...\n{Exception}", session.RemoteEndPoint, ex);
                        ArrayPool<byte>.Shared.Return(payload);
                        break;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                this._logger.LogInformation("Client {Endpoint} has closed the connection", session.RemoteEndPoint);
            }

            catch (OverflowException)
            {
                this._logger.LogError("Payload buffer overflow detected from client {EndPoint}, disconnecting...", session.RemoteEndPoint);
            }

            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                // Client disconnected abruptly, or there was a network error, we just disconnect it
            }

            catch (Exception ex)
            {
                this._logger.LogError("Failed to handle client event loop: {Exception}", ex);
            }
            finally
            {
                this._sessionManager.Disconnect(session.RemoteEndPoint.ToString());
            }
        }

        private void Shutdown()
        {
            this._listener.Stop();
            this._logger.LogInformation("The server successfully stoped");
        }
    }
}
