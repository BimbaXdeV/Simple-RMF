using RMF.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Storage
{
    internal class ClientSession
    {
        public TcpClient Client { get; set; }
        public EventController Events { get; } = new();
        public string EndPoint { get; set; }
        public string IPAddress { get; set; } = "127.0.0.1";
        public ushort Port { get; set; } = 0;

        public int PacketsCount { get; private set; }
        public DateTime HandleStartTime { get; private set; }
        public DateTime LastTransferTime { get; private set; }

        public byte[]? LastFrame { get; set; }
        public DateTime? LastUpdate { get; set; }

        public ClientSession(TcpClient client)
        {
            this.Client = client;

            //string[] endPointElements = endPoint.Split(':');
            //this.IPAddress = endPointElements[0];
            //this.Port = ushort.Parse(endPointElements[1]);

            if (client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                this.EndPoint = remoteEndPoint.ToString();
                this.IPAddress = remoteEndPoint.Address.ToString();
                this.Port = (ushort)remoteEndPoint.Port;
            }

            this.LastTransferTime = DateTime.UtcNow;
        }

        public bool IsRateLimitExceed(int maxRate)
        {
            DateTime currentTime = DateTime.UtcNow;
            this.LastTransferTime = currentTime;

            if ((currentTime - this.HandleStartTime).TotalSeconds >= 1.0f)
            {
                this.PacketsCount = 0;
                this.HandleStartTime = currentTime;
            }

            this.PacketsCount++;
            return this.PacketsCount > maxRate;
        }
    }
}
