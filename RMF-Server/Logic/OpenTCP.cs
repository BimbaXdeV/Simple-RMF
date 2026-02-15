using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Channels;
using RMF_Server.Debugger;
using RMF_Server.Exceptions;
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
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1"); ;
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
                    string? endPoint = client.Client.RemoteEndPoint?.ToString();

                    if (string.IsNullOrEmpty(endPoint))
                    {
                        Logging.Warning($"Connection attempt from unknown address, access denied");
                        client.Close();
                        continue;
                    }

                    if (Firewall.IsBanned(endPoint.Split(":")[0]))
                    {
                        Logging.Warning($"A banned client {endPoint} attempted to connect, access denied");
                        client.Close();
                        continue;
                    }

                    if (!SessionManager.NewConnection(client, endPoint))
                    {
                        Logging.Output($"A duplicate connection to the server was detected, the duplicated client {endPoint} was disconnected");
                        client.Close();
                        continue;
                    }

                    AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionManager.Connections.Count}");
                    Logging.Output($"Registered new connection from {endPoint}");
                    _ = Task.Factory.StartNew(() => ClientHandler(client, endPoint), TaskCreationOptions.LongRunning);
                }
            }

            catch (OperationCanceledException)
            {
                Logging.Separator();
                Logging.Warning("Cancellation requested, stopping server...");
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

        public void Shutdown()
        {
            Logging.Output("The server is shutting down...");
            if (!SessionManager.Connections.IsEmpty)
            {
                SessionManager.ClearConnections();
            }
            this.Server?.Stop();
            Logging.Output("The server successfully stoped");
        }

        //private static async Task ClientDowntime(TcpClient client, string endPoint, int connectionDurationSecs)
        //{
        //    await Task.Delay(connectionDurationSecs * 1000);
        //    SessionManager.Disconnect(client, endPoint);
        //}

        private static async Task ClientHandler(TcpClient client, string endPoint)
        {
            CancellationTokenSource cts = new();

            try
            {
                NetworkStream stream = client.GetStream();

                byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)
                ClientSession? session = SessionManager.Connections.GetValueOrDefault(endPoint);

                cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));
                while (client.Connected && session != null)
                {
                    int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, cts.Token);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                        
                    cts.CancelAfter(TimeSpan.FromSeconds(ConfigurationManager.ReceiveTimeoutSecs));  // Time bomb :D
                    if (session.IsRateLimitExceed(ConfigurationManager.MaxPacketRate))
                    {
                        Logging.Warning($"The client {endPoint} has exceeded the allowed packet rate limit");
                        Firewall.Ban(session.IPAddress);
                        break;
                    }

                    short id = BitConverter.ToInt16(headerBuffer, 0);          // Bytes 0, 1
                    if (!ChannelDispatcher.IsChannelExists(id / 100))  // It is needed to save memory and reject a packet directly based on its ID
                    {
                        Logging.Warning($"Received a packet with unknown id \"{id}\" from the client {endPoint}");
                        break;
                    }
                    int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5
                    byte[] payload = await PacketsHandler.ReadPayload(endPoint, stream, packetLength);

                    try
                    {
                        PacketContext context = new(endPoint, id, packetLength, payload);
                        await ChannelDispatcher.SendPacket(context);  // The packet will be processed in the channel, so we can immediately start waiting for the next packet without worrying about the processing time of the current
                    }
                    catch (Exception ex)
                    {
                        Logging.Error($"Fatal connection error when trying to handle incoming packet from {endPoint}, disconnecting...\n{ex}");
                        ArrayPool<byte>.Shared.Return(payload);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logging.Warning($"Client {endPoint} timed out waiting for packets, disconnecting...");
            }

            catch (PayloadBufferOverflow)
            {
                Logging.Error($"Payload buffer overflow detected from client {endPoint}, disconnecting...");
            }

            catch (Exception ex)
            {
                Logging.Error($"Failed to start client event loop: {ex}");
            }
            finally
            {
                SessionManager.Disconnect(client, endPoint);
            }
        }
    }
}
