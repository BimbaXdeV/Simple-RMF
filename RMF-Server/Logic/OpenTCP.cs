using RMF.Core.Bases;
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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class OpenTCP
    {
        private TcpListener? Server;

        public async Task RunServer(CancellationToken token)
        {
            Logging.Output("Starting TCP server...");
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1");
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            this.Server ??= new TcpListener(ip, port);

            try
            {
                this.Server.Start();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionManager.Connections.Count}");
                Logging.Output($"Server successfully started listening at {ip}:{port}");

                while (!token.IsCancellationRequested)
                {
                    TcpClient client = await this.Server.AcceptTcpClientAsync(token);
                    IPEndPoint? ipEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string? endPoint = ipEndPoint?.ToString();

                    if (SessionManager.Connections.Count >= ConfigurationManager.MaxConnections)
                    {
                        Logging.Warning($"A client {endPoint} attempted to connect to server with maximum capacity ({ConfigurationManager.MaxConnections}), access denied");
                        client.Close();
                        continue;
                    }

                    if (ipEndPoint == null || string.IsNullOrEmpty(endPoint))
                    {
                        Logging.Warning($"Connection attempt from unknown address, access denied");
                        client.Close();
                        continue;
                    }

                    if (Firewall.IsBanned(ipEndPoint.Address.ToString()))
                    {
                        Logging.Warning($"A banned client {endPoint} attempted to connect, access denied");
                        client.Close();
                        continue;
                    }

                    ServerClientSession? session = SessionManager.NewConnection(client, endPoint, token);
                    if (session == null)
                    {
                        Logging.Output($"A duplicate connection to the server was detected, the duplicated client {endPoint} was disconnected");
                        client.Close();
                        continue;
                    }

                    AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionManager.Connections.Count}");
                    Logging.Output($"Registered new connection from {endPoint}");

                    if (ConfigurationManager.EnableWelcomeHandshake)
                    {
                        DateTime connectionTime = DateTime.UtcNow;
                        HandshakePacket handshakePacket = new()
                        {
                            ConnectionTimestamp = new DateTimeOffset(connectionTime).ToUnixTimeMilliseconds(),
                            SessionID = SessionManager.GetSessionID(endPoint),
                            RemoteIP = ipEndPoint.Address.ToString(),
                            RemotePort = ipEndPoint.Port,
                            SendBufferSize = session.Client.Client.SendBufferSize,
                            ReceiveBufferSize = session.Client.Client.ReceiveBufferSize,
                        };
                        await StreamManager.SendPacketAsync(session.Client.GetStream(), handshakePacket, token);
                    }

                    if (ConfigurationManager.EnableCollectingClientInfo)
                    {
                        ClientInfoRequest clientInfoRequest = new();
                        await StreamManager.SendPacketAsync(session.Client.GetStream(), clientInfoRequest, token);
                    }
                    
                    if (ConfigurationManager.EnableClientHeartbeat)
                    {
                        session.Events.StartEvent(session, "HeartbeatEvent", new Dictionary<string, object>
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
                Logging.Error($"The server return an exception: {ex}");
            }
            finally
            {
                Shutdown();
            }
        }

        private static async Task ClientHandler(ServerClientSession session, CancellationToken token)
        {
            CancellationTokenSource cts = new();

            try
            {
                NetworkStream stream = session.Client.GetStream();

                byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

                cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));
                while (session.Client.Connected)
                {
                    await stream.ReadExactlyAsync(headerBuffer.AsMemory(0, headerBuffer.Length), cts.Token);
                        
                    cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));  // Time bomb :D
                    if (session.IsRateLimitExceed(ConfigurationManager.MaxPacketRate))
                    {
                        Logging.Warning($"The client {session.EndPoint} has exceeded the allowed packet rate limit");
                        Firewall.Ban(session.EndPoint?.Address.ToString());
                        break;
                    }

                    short id = BitConverter.ToInt16(headerBuffer, 0);          // Bytes 0, 1
                    if (!ChannelDispatcher.IsChannelExists(id / 100))  // It is needed to save memory and reject a packet directly based on its ID
                    {
                        Logging.Warning($"Received a packet with unknown id \"{id}\" from the client {session.EndPoint}");
                        break;
                    }
                    int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5
                    byte[] payload = await PayloadReader.ReadAsync(stream, packetLength, token);

                    try
                    {
                        PacketContext context = new(session.EndPoint!, id, packetLength, payload);
                        await ChannelDispatcher.SendPacket(context);  // The packet will be processed in the channel, so we can immediately start waiting for the next packet without worrying about the processing time of the current

                        if (session.CollectingStats)
                        {
                            session.IncrementReceivedPackets();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error($"Fatal connection error when trying to handle incoming packet from {session.EndPoint}, disconnecting...\n{ex}");
                        ArrayPool<byte>.Shared.Return(payload);
                        break;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                Logging.Warning($"Client {session.EndPoint} has closed the connection");
            }

            catch (OverflowException)
            {
                Logging.Error($"Payload buffer overflow detected from client {session.EndPoint}, disconnecting...");
            }

            catch (Exception ex) when (ex is IOException || ex is SocketException)
            {
                // Client disconnected abruptly, or there was a network error, we just disconnect it
            }

            catch (Exception ex)
            {
                Logging.Error($"Failed to handle client event loop: {ex}");
            }
            finally
            {
                SessionManager.Disconnect(session.EndPoint?.ToString() ?? "");
            }
        }

        public void Shutdown()
        {
            this.Server?.Stop();
            Logging.Output("The server successfully stoped");
        }
    }
}
