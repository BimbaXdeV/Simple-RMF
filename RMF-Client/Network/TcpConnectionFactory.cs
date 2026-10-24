using RMF.Core.Network;
using RMF_Client.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Network
{
    internal class TcpConnectionFactory : IConnectionFactory
    {
        private readonly ConnectionConfig _connectionConfig;

        public TcpConnectionFactory(ConnectionConfig connectionConfig)
        {
            _connectionConfig = connectionConfig;
        }

        public INetworkConnection CreateConnection()
        {
            IPAddress ip = _connectionConfig.IPAddress;
            int port = _connectionConfig.Port;

            TcpClient tcpClient = new(ip.ToString(), port);
            return new TcpConnection(tcpClient);
        }
    }
}
