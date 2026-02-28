using RMF.Core.Events;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Client.Logic;
using RMF_Client.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Network
{
    internal class EntryTCP
    {
        private async Task PacketListener(CancellationToken token)
        {
            AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Connected");

            NetworkStream stream = ConnectionSession.Client!.GetStream();
            byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

            while (ConnectionSession.Client.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, token);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    short id = BitConverter.ToInt16(headerBuffer, 0);          // Bytes 0, 1
                    int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5
                    Console.WriteLine($"Packet ID: {id}, Length: {packetLength} bytes");
                    byte[] payload = await PayloadReader.ReadAsync(stream, packetLength, token);

                    Packet? packet = PacketsAssembler.GetPacket(id);
                    if (packet != null)
                    {
                        Console.WriteLine(packet.GetType().Name + " received");
                        ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, packetLength);
                        SpanReader payloadReader = new(payloadSpan);

                        packet.Deserialize(ref payloadReader);
                        PacketsProcessor.SwitchHandle(packet);  // When scaling, a new case needs to be added
                        ArrayPool<byte>.Shared.Return(payload);
                    }

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
        }

        public async Task Connect(CancellationToken token)
        {
            AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Waiting...");
            string ip = (ConfigurationManager.IPAddress == "Any" || string.IsNullOrEmpty(ConfigurationManager.IPAddress)) ? "127.0.0.1" : ConfigurationManager.IPAddress;
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            try
            {
                ConnectionSession.NewSession(ip, port);
                AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointIP", ip.ToString() },
                    { "endpointPort", port.ToString() }
                });

                ConnectionSession.Events?.ToggleEvent(ConnectionSession.Client!.GetStream(), "HeartbeatEvent", new Dictionary<string, object>
                {
                    {"IntervalSecs", 10 }
                });
                await PacketListener(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Task.Delay(5000, token);
            }
            finally
            {
                ConnectionSession.Disconnect();
                AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Offline");
            }
        }
    }
}
