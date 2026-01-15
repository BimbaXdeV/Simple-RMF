using Newtonsoft.Json.Linq;
using RMF_Server.Debugger;
using RMF_Server.Exceptions;
using RMF_Server.Packets;
using RMF_Server.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class OpenTCP
    {
        private TcpListener? server;

        public async Task RunServer(CancellationToken token)
        {
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1"); ;
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            this.server ??= new TcpListener(ip, port);

            try
            {
                this.server.Start();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionManager.SessionsConnected()}");
                Logging.Output($"Server successfully started listening at {ip}:{port}");
                Logging.Separator();

                while (!token.IsCancellationRequested)
                {
                    TcpClient client = await this.server.AcceptTcpClientAsync(token);
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

                    AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionManager.SessionsConnected()}");
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
            if (SessionManager.SessionsConnected() > 0)
            {
                SessionManager.ClearConnections();
            }
            this.server?.Stop();
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
                    if (stream.DataAvailable)
                    {
                        short id = reader.ReadInt16();
                        short length = reader.ReadInt16();

                        int payloadSize = length - 4;
                        byte[] payload = await PacketsHandler.ReadPayload(endPoint, stream, payloadSize);

                        Packet? packet = PacketsAssembler.GetPacket(id);
                        if (packet != null)
                        {
                            using MemoryStream ms = new MemoryStream(payload);
                            using BinaryReader payloadReader = new BinaryReader(ms);

                            packet.Deserialize(payloadReader);
                            PacketsHandler.SwitchHandle(packet, endPoint);
                        }
                    }
                    await Task.Delay(ConfigurationManager.PacketsListenDelayMsecs);
                }
            }
            catch (PayloadBufferOverflow)
            {
            }

            catch (Exception ex)
            {
                Logging.Error($"Error in client handler for {endPoint}: {ex}");
            }
            finally
            {
                SessionManager.Disconnect(client, endPoint);
            }
        }
    }
}
