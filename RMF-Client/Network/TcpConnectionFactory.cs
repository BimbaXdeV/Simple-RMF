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
            this._connectionConfig = connectionConfig;
        }

        public INetworkConnection CreateConnection()
        {
            IPAddress ip = this._connectionConfig.IPAddress != "Any"
                ? IPAddress.Parse(this._connectionConfig.IPAddress ?? "127.0.0.1")
                : IPAddress.Any;

            int port = this._connectionConfig.Port >= IPEndPoint.MinPort && this._connectionConfig.Port <= IPEndPoint.MaxPort
                ? this._connectionConfig.Port
                : 8000;  // Default port if the provided port is invalid

            TcpClient tcpClient = new(ip.ToString(), port);
            return new TcpConnection(tcpClient);
        }
    }
}
