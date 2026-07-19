using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Server;
using RMF_Server.Channels;
using RMF_Server.Debugger;
using RMF_Server.Packets;
using RMF_Server.Storage;
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
    internal class OpenTCP
    {
        private readonly IConnectionListener _listener;
        private readonly IServerSessionManager _sessionManager;
        private readonly ITlsManager _tlsManager;

        private readonly IFirewall? _firewall;
        private readonly ILoggingEngine? _logger;

        public OpenTCP(
            IConnectionListener listener,
            IServerSessionManager sessionManager,
            ITlsManager tlsManager,
            IFirewall? firewall = null,
            ILoggingEngine? logger = null
        )
        {
            this._listener = listener;
            this._tlsManager = tlsManager;
            this._sessionManager = sessionManager;

            this._firewall = firewall;
            this._logger = logger;
        }

        public async Task RunServer(CancellationToken token)
        {
            this._logger?.Output("Starting TCP server...");
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1");
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;
            X509Certificate2 serverCertificate = this._tlsManager.GetOrCreateCertificate();

            string bannedIPsPath = PathManager.GetResolvedPath("BannedIPs", "blacklist", "txt");
            this._firewall?.TryLoadFrom(bannedIPsPath);

            try
            {
                _listener.Start();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this._sessionManager.TotalConnections}");
                this._logger?.Output($"Server successfully started listening at {ip}:{port}");

                while (!token.IsCancellationRequested)
                {
                    INetworkConnection connection = await _listener.AcceptConnectionAsync(token);
                    IPEndPoint? ipEndPoint = connection.RemoteEndPoint as IPEndPoint;
                    string? endPoint = ipEndPoint?.ToString();

                    if (this._sessionManager.TotalConnections >= ConfigurationManager.MaxConnections)
                    {
                        this._logger?.Warning($"A client {endPoint} attempted to connect to server with maximum capacity ({ConfigurationManager.MaxConnections}), access denied");
                        connection.Close();
                        continue;
                    }

                    if (ipEndPoint == null || string.IsNullOrEmpty(endPoint))
                    {
                        this._logger?.Warning($"Connection attempt from unknown address, access denied");
                        connection.Close();
                        continue;
                    }

                    if (this._firewall != null && this._firewall.IsBanned(ipEndPoint.Address.ToString()))
                    {
                        this._logger?.Warning($"A banned client {endPoint} attempted to connect, access denied");
                        connection.Close();
                        continue;
                    }

                    if (this._sessionManager.GetConnectionsFromIP(ipEndPoint.Address) >= ConfigurationManager.MaxConnectionsPerIP)
                    {
                        this._logger?.Warning($"A client {endPoint} attempted to exceed the connection limit ({ConfigurationManager.MaxConnectionsPerIP}) from a single IP address, access denied");
                        connection.Close();
                        continue;
                    }

                    SslStream sslStream = new(connection.GetNetworkStream(), false);
                    try
                    {
                        await sslStream.AuthenticateAsServerAsync(serverCertificate).WaitAsync(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs), token);
                    }
                    catch (Exception ex)
                    {
                        this._logger?.Error($"TLS handshake failed with client {endPoint}, disconnecting...{Environment.NewLine}{ex}");
                        sslStream.Dispose();
                        this._sessionManager.Disconnect(endPoint);
                        continue;
                    }

                    SecureConnectionAdapter tlsConnection = new(connection, sslStream);
                    IServerClientSession? session = this._sessionManager.NewConnection(tlsConnection, token);
                    if (session == null)
                    {
                        this._logger?.Output($"A duplicate connection to the server was detected, the duplicated client {endPoint} was disconnected");
                        connection.Close();
                        continue;
                    }

                    AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this._sessionManager.TotalConnections}");
                    this._logger?.Output($"Registered new connection from {endPoint}");

                    if (ConfigurationManager.EnableWelcomeHandshake)
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

                    if (ConfigurationManager.EnableBuildComparison)
                    {
                        ClientVersionRequest versionRequest = new();
                        session.SendPacket(versionRequest);
                    }

                    if (ConfigurationManager.EnableCollectingClientInfo)
                    {
                        ClientInfoRequest clientInfoRequest = new();
                        session.SendPacket(clientInfoRequest);
                    }

                    if (ConfigurationManager.EnableClientHeartbeat)
                    {
                        session.StartEvent("HeartbeatEvent", new Dictionary<string, object>
                        {
                            { "IntervalSecs", ConfigurationManager.ClientHeartbeatIntervalSecs }
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
                this._logger?.Error($"The server return an exception: {ex}");
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
                Stream stream = session.GetStream();

                byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

                cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));
                while (session.IsRunning)
                {
                    await stream.ReadExactlyAsync(headerBuffer.AsMemory(0, headerBuffer.Length), cts.Token);

                    cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));  // Time bomb :D
                    if (session.IsRateLimitExceed(ConfigurationManager.MaxPacketRate))
                    {
                        this._logger?.Warning($"The client {session.RemoteEndPoint} has exceeded the allowed packet rate limit");
                        this._firewall?.Ban(session.RemoteEndPoint.Address.ToString());
                        break;
                    }

                    short id = BitConverter.ToInt16(headerBuffer, 0);  // Bytes 0, 1
                    if (!ChannelDispatcher.IsChannelExists(id / 100))  // It is needed to save memory and reject a packet directly based on its ID
                    {
                        this._logger?.Warning($"Received a packet with unknown id \"{id}\" from the client {session.Remo}");
                        break;
                    }
                    int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5
                    byte[] payload = await PayloadReader.ReadAsync(stream, packetLength, token);

                    try
                    {
                        PacketContext context = new(session.RemoteEndPoint, id, packetLength, payload);
                        await ChannelDispatcher.SendPacket(context);  // The packet will be processed in the channel, so we can immediately start waiting for the next packet without worrying about the processing time of the current

                        if (session.CollectingStats)
                        {
                            session.IncrementReceivedPackets();
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger?.Error($"Fatal connection error when trying to handle incoming packet from {session.EndPoint}, disconnecting...{Environment.NewLine}{ex}");
                        ArrayPool<byte>.Shared.Return(payload);
                        break;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                this._logger?.Warning($"Client {session.RemoteEndPoint} has closed the connection");
            }

            catch (OverflowException)
            {
                this._logger?.Error($"Payload buffer overflow detected from client {session.RemoteEndPoint}, disconnecting...");
            }

            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                // Client disconnected abruptly, or there was a network error, we just disconnect it
            }

            catch (Exception ex)
            {
                this._logger?.Error($"Failed to handle client event loop: {ex}");
            }
            finally
            {
                this._sessionManager.Disconnect(session.RemoteEndPoint.ToString());
            }
        }

        public void Shutdown()
        {
            this._listener.Stop();
            this._logger?.Output("The server successfully stoped");
        }
    }
}
