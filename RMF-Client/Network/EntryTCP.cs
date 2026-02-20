using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Client.Logic;
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
        private TcpClient? Client;

        private async Task PacketListener(CancellationToken token)
        {
            AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Connected");

            NetworkStream stream = this.Client!.GetStream();
            byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

            while (this.Client.Connected)
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
                    byte[] payload = await PayloadReader.ReadAsync(stream, packetLength, token);

                    Packet? packet = PacketsAssembler.GetPacket(id);
                    if (packet != null)
                    {
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
            IPAddress ip = ConfigurationManager.IPAddress == "Any" ? IPAddress.Any : IPAddress.Parse(ConfigurationManager.IPAddress ?? "127.0.0.1");
            int port = (ConfigurationManager.Port >= 1000 && ConfigurationManager.Port <= 9999) ? ConfigurationManager.Port : 8000;

            this.Client ??= new TcpClient();

            try
            {
                await this.Client.ConnectAsync(ip, port, token);
                AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointIP", ip.ToString() },
                    { "endpointPort", $"{port.ToString()} ({GetRemotePort()})" }
                });
                await PacketListener(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                Disconnect();
                AppearanceManager.SetTitle(ConfigurationManager.AppTitle + " | Offline");
            }
        }

        public int GetRemotePort()
        {
            IPEndPoint? remoteEndpoint = this.Client?.Client.RemoteEndPoint as IPEndPoint;
            return remoteEndpoint?.Port ?? -1;
        }

        public void Disconnect()
        {
            if (this.Client != null && this.Client.Connected)
            {
                this.Client.Close();
            }
            this.Client = null;
        }
    }
}
