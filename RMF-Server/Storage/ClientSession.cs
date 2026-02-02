using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Storage
{
    internal class ClientSession
    {
        public TcpClient Client { get; set; }
        public string EndPoint { get; set; }
        public string IPAddress { get; set; }
        public ushort Port { get; set; }

        public int PacketsCount { get; set; }
        public DateTime LastTransferTime { get; set; }

        public byte[]? LastFrame { get; set; }
        public DateTime? LastUpdate { get; set; }

        public ClientSession(TcpClient client, string endPoint)
        {
            this.Client = client;
            this.EndPoint = endPoint;

            string[] endPointElements = endPoint.Split(':');
            this.IPAddress = endPointElements[0];
            this.Port = ushort.Parse(endPointElements[1]);

            this.LastTransferTime = DateTime.UtcNow;
        }

        public bool IsRateLimitExceed(int maxRate)
        {
            DateTime currentTime = DateTime.UtcNow;
            int timeDifference = (int)(currentTime - this.LastTransferTime).TotalSeconds;

            if (timeDifference >= 1)
            {
                this.PacketsCount = 0;
            }
            return this.PacketsCount > maxRate;
        }
    }
}
