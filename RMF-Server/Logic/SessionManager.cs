using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class SessionManager
    {
        public static readonly ConcurrentDictionary<string, ClientSession> Connections = [];

        public static async Task SendPacket(string endPoint, Packet packet)
        {
            if (Connections.TryGetValue(endPoint, out ClientSession? session))
            {
                try
                {
                    if (session == null)
                    {
                        Logging.Error($"Failed to send packet to client {endPoint}, session not found");
                        return;
                    }

                    if (!session.Client.Connected)
                    {
                        Logging.Warning($"Cannot send packet to client {endPoint}, client is disconnected");
                        return;
                    }

                    MemoryStream ms = NetworkBuffer.GetMemoryStream();
                    BinaryWriter writer = NetworkBuffer.GetBinaryWriter();
                    packet.WriteToStream(writer);
                    
                    byte[] payload = ms.GetBuffer();
                    int packetLength = (int)ms.Length;

                    await session.Client.GetStream().WriteAsync(new ReadOnlyMemory<byte>(payload, 0, packetLength));
                }
                catch (Exception ex)
                {
                    Logging.Error($"Failed to send packet to client {endPoint}: {ex.Message}");
                }
            }
        }

        public static async Task BroadcastPacket(Packet packet)
        {
            Task[] tasks = Connections.Values.Select(session => SendPacket(session.EndPoint, packet)).ToArray();
            await Task.WhenAll(tasks);
        }

        public static bool NewConnection(TcpClient client, string endPoint)
        {
            ClientSession session = new ClientSession(client, endPoint);
            return Connections.TryAdd(endPoint, session);
        }

        public static void Disconnect(TcpClient client, string endPoint)
        {
            if (Connections.TryRemove(endPoint, out _))
            {
                client.Close();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count}");
                Logging.Output($"Client {endPoint} was disconnected");
            }
        }

        public static void ClearConnections()
        {
            Logging.Output("Connections are being cleared...");

            int disconnectedClientsCount = 0;
            int totalConnectedClients = Connections.Count();

            foreach (var entry in Connections)
            {
                entry.Value.Client.Close();
                Logging.Output($"Client {entry.Key} was forced disconnected");
                disconnectedClientsCount++;
            }
            Connections.Clear();
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count()}");
            Logging.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }
    }
}
