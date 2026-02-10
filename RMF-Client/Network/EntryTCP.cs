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

        public async Task Connect(CancellationToken token)
        {
            IPAddress? ip = IPAddress.TryParse(IPAddress.Any.ToString(), out IPAddress? parsedIP) ? parsedIP : null;
            if (ip == null)
            {
                Console.WriteLine("The connection IPAddress is corrupted. Check your configuration");
                return;
            }

            Console.WriteLine($"Attempting to connect to {ip}:8000...");
            this.Client ??= new TcpClient();

            try
            {
                await this.Client.ConnectAsync(ip, 8000, token);
                Console.WriteLine("Successfully connected to RMF-Server!");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Received session end signal, disconnecting...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the server: {ex.Message}");
            }
            finally
            {
                if (this.Client.Connected)
                {
                    this.Client.Close();
                }
                this.Client = null;
            }
        }
    }
}
