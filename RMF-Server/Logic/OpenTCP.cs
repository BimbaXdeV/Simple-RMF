using Newtonsoft.Json.Linq;
using RMF_Server.Debugger;
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
        private readonly ConcurrentDictionary<string, TcpClient> connections = [];

        public async Task RunServer(CancellationToken token)
        {
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1"); ;
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            this.server ??= new TcpListener(ip, port);

            try
            {
                this.server.Start();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this.connections.Count}");
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

                    if (!this.connections.TryAdd(endPoint, client))
                    {
                        Logging.Output($"A duplicate connection to the server was detected, the duplicated client {endPoint} was disconnected");
                        client.Close();
                        continue;
                    }

                    AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this.connections.Count}");
                    Logging.Output($"Registered new connection from {endPoint}");
                    _ = Task.Run(() => ClientDowntime(client, endPoint, 10));
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
            if (this.connections.Count > 0)
            {
                ClearConnections();
            }
            this.server?.Stop();
            Logging.Output("The server successfully stoped");
        }

        private void Disconnect(TcpClient client, string endPoint)
        {
            if (this.connections.TryRemove(endPoint, out _))
            {
                client.Close();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this.connections.Count}");
                Logging.Output($"Client {endPoint} was disconnected");
            }
        }

        private void ClearConnections()
        {
            Logging.Output("Connections are being cleared...");

            int disconnectedClientsCount = 0;
            int totalConnectedClients = this.connections.Count;

            foreach (var entry in this.connections)
            {
                entry.Value.Close();
                Logging.Output($"Client {entry.Key} was forced disconnected");
                disconnectedClientsCount++;
            }
            this.connections.Clear();
            Logging.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }

        private async Task ClientDowntime(TcpClient client, string endPoint, int connectionDurationSecs)
        {
            await Task.Delay(connectionDurationSecs * 1000);
            Disconnect(client, endPoint);
        }
    }
}
