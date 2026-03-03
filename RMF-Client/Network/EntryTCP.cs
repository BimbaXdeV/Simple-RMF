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

            NetworkStream? stream = SessionManager.Connection?.Client.GetStream();
            if (stream == null)
            {
                return;
            }

            byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

            while (SessionManager.Connection?.Client.Connected == true)
            {
                int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, token);
                if (bytesRead == 0)
                {
                    break;
                }
                short id = BitConverter.ToInt16(headerBuffer, 0);          // Bytes 0, 1
                int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5

                byte[] payload = await PayloadReader.ReadAsync(stream, packetLength, token);

                try
                {
                    Packet? packet = PacketsAssembler.GetPacket(id);
                    if (packet == null)
                    {
                        await Task.Delay(5000, token);
                        continue;
                    }

                    ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, packetLength);
                    SpanReader payloadReader = new(payloadSpan);

                    packet.Deserialize(ref payloadReader);
                    PacketsProcessor.SwitchHandle(packet);  // When scaling, a new case needs to be added
                    ArrayPool<byte>.Shared.Return(payload);
                }
                catch (Exception)
                {
                    ArrayPool<byte>.Shared.Return(payload);
                    throw;
                }
            }
        }

        public async Task Connect(CancellationToken token)
        {
            if (SessionManager.Connection?.Client == null)
            {
                SessionManager.StartSession(new TcpClient(), token);
            }

            AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Waiting...");
            string ip = (ConfigurationManager.IPAddress == "Any" || string.IsNullOrEmpty(ConfigurationManager.IPAddress)) ? "127.0.0.1" : ConfigurationManager.IPAddress;
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            try
            {
                await SessionManager.Connection!.Client.ConnectAsync(ip, port, token);
                SessionManager.Connection.RunProcessing(token);
                AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointIP", ip.ToString() },
                    { "endpointPort", port.ToString() }
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
                SessionManager.ClearSession();
                AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Offline");
            }
        }
    }
}
