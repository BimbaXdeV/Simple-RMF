using Newtonsoft.Json.Linq;
using RMF_Packets.Shared;
using RMF_Server.Debugger;
using RMF_Server.Exceptions;
using RMF_Server.Packets;
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
                    _ = Task.Run(() => ClientHandler(client, endPoint));
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
            if (SessionManager.Connections.Count > 0)
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
                BinaryReader reader = new BinaryReader(stream);

                while (client.Connected)
                {
                    try
                    {
                        if (stream.DataAvailable)
                        {
                            short id = reader.ReadInt16();
                            short length = reader.ReadInt16();

                            int payloadSize = length - 4;
                            byte[] payload = await PacketsHandler.ReadPayload(endPoint, stream, payloadSize);

                            try
                            {
                                Packet? packet = PacketsAssembler.GetPacket(id);
                                if (packet != null)
                                {
                                    using MemoryStream ms = new MemoryStream(payload);
                                    using BinaryReader payloadReader = new BinaryReader(ms);

                                    packet.Deserialize(payloadReader);
                                    PacketsHandler.SwitchHandle(packet, endPoint);  // When scaling, a new case needs to be added
                                }
                            }
                            catch (Exception logicEx)
                            {
                                Logging.Error($"Error in client handler for {endPoint}: {logicEx}");
                            }
                        }
                    }
                    catch (Exception falalEx) when (!(falalEx is OperationCanceledException))
                    {
                        Logging.Error($"Fatal connection error from {endPoint}, disconnecting...");
                        break;
                    }
                    await Task.Delay(ConfigurationManager.PacketsListenDelayMsecs);
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
