using Newtonsoft.Json.Linq;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Exceptions;
using RMF_Server.Packets;
using RMF_Server.Storage;
using System;
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

        private async Task ClientDowntime(TcpClient client, string endPoint, int connectionDurationSecs)
        {
            await Task.Delay(connectionDurationSecs * 1000);
            SessionManager.Disconnect(client, endPoint);
        }

        private async Task ClientHandler(TcpClient client, string endPoint)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                BinaryReader reader = NetworkBuffer.GetBinaryReader(stream);

                byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)
                ClientSession? session = SessionManager.Connections.GetValueOrDefault(endPoint);
                while (client.Connected && session != null)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length);
                        if (bytesRead == 0)
                        {
                            Logging.Output($"The client {endPoint} has disconnected");
                            break;
                        }

                        if (session.IsRateLimitExceed(ConfigurationManager.MaxPacketRate))
                        {
                            Logging.Warning($"The client {endPoint} has exceeded the allowed packet rate limit");
                            session.Client.Close();
                            SessionManager.Connections.TryRemove(endPoint, out _);
                            Firewall.Ban(session.IPAddress);
                            break;
                        }

                        short id = reader.ReadInt16();
                        int length = reader.ReadInt32();
                        byte[] payload = await PacketsHandler.ReadPayload(endPoint, stream, length);

                        try
                        {
                            Packet? packet = PacketsAssembler.GetPacket(id);
                            if (packet != null)
                            {
                                BinaryReader payloadReader = NetworkBuffer.GetBinaryReader();

                                packet.Deserialize(payloadReader);
                                PacketsHandler.SwitchHandle(packet, endPoint);  // When scaling, a new case needs to be added
                            }
                        }
                        catch (Exception logicEx)
                        {
                            Logging.Error($"Error in client handler for {endPoint}: {logicEx}");
                        }
                    }
                    catch (Exception falalEx) when (!(falalEx is OperationCanceledException))
                    {
                        Logging.Error($"Fatal connection error from {endPoint}, disconnecting...");
                        break;
                    }
                }
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
