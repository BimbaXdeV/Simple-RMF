using RMF.Core.Network;
using RMF.Core.Packets;
using System;
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

        private async Task PacketListener()
        {
            CancellationTokenSource cts = new();

            NetworkStream stream = this.Client!.GetStream();
            byte[] headerBuffer = new byte[6];  // ID (2) + Length (4)

            while (this.Client.Connected && !cts.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, cts.Token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    short id = BitConverter.ToInt16(headerBuffer, 0);          // Bytes 0, 1
                    int packetLength = BitConverter.ToInt32(headerBuffer, 2);  // Bytes 2, 3, 4, 5
                    byte[] payload = await PayloadReader.ReadAsync(stream, packetLength);

                    Packet? packet = PacketsAssembler.GetPacket(id);
                    if (packet != null)
                    {
                        ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, packetLength);
                        SpanReader payloadReader = new(payloadSpan);

                        packet.Deserialize(ref payloadReader);
                        PacketsProcessor.SwitchHandle(packet);  // When scaling, a new case needs to be added
                    }

                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    cts.Cancel();
                }
            }
        }

        public async Task Connect(CancellationToken token)
        {
            IPAddress? ip = IPAddress.TryParse(IPAddress.Any.ToString(), out IPAddress? parsedIP) ? parsedIP : null;
            if (ip == null)
            {
                //Console.WriteLine("The connection IPAddress is corrupted. Check your configuration");
                return;
            }

            //Console.WriteLine($"Attempting to connect to {ip}:8000...");
            this.Client ??= new TcpClient();

            try
            {
                await this.Client.ConnectAsync(ip, 8000, token);
                _ = Task.Factory.StartNew(PacketListener, TaskCreationOptions.LongRunning);
                //Console.WriteLine("Successfully connected to RMF-Server!");
            }
            catch (OperationCanceledException)
            {
                //Console.WriteLine("Received session end signal, disconnecting...");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed to connect to the server: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
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
