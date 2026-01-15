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
        public string EndPoint { get; set; } = "";
        
        public byte[]? LastFrame { get; set; }
        public DateTime LastUpdate { get; set; }

        public ClientSession(TcpClient client, string endPoint)
        {
            this.Client = client;
            this.EndPoint = endPoint;
        }
    }
}
