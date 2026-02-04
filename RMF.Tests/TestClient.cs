using System.Net.Sockets;

namespace RMF.Tests
{
    internal class TestClient(string iPAddress, int port)
    {
        private TcpClient? Client;

        private string IPAddress = iPAddress;
        private int Port = port;

        public void Connect()
        {
            this.Client ??= new TcpClient();
            this.Client.Connect(IPAddress, Port);
            Console.WriteLine($"Connected to server at {IPAddress}:{Port}");
        }

        public async Task StartBombing(int totalSecs, float delay = 1f)
        {
            if (this.Client == null || !this.Client.Connected)
            {
                throw new Exception("Client is not connected to the server");
            }

            CancellationTokenSource cts = new();

            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(ms);

                cts.CancelAfter(TimeSpan.FromSeconds(totalSecs));

                while (!cts.Token.IsCancellationRequested)
                {
                    ms.SetLength(0);

                    writer.Write((short)100);
                    writer.Write(0);

                    long startBodyPosition = ms.Position;
                    writer.Write("Lindows");
                    writer.Write("Inter Core i3 01100f");
                    writer.Write("Mediatek Graphics 01");
                    writer.Write("Admin?");
                    long endBodyPosition = ms.Position;

                    // So what if it's a crutch? :D
                    ms.Position = 2;
                    writer.Write((int)(endBodyPosition - startBodyPosition));
                    ms.Position = endBodyPosition;

                    ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
                    await this.Client.GetStream().WriteAsync(buffer , cts.Token);
                    Console.WriteLine($"Packet successfully sent!");

                    await Task.Delay(TimeSpan.FromSeconds(delay), cts.Token);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Bombing stopped");
            }
        }
    }
}
