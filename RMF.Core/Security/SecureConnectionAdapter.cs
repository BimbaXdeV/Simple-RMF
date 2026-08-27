using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Security
{
    public class SecureConnectionAdapter : INetworkConnection
    {
        private readonly INetworkConnection _baseConnection;
        private readonly SslStream _sslStream;

        public SecureConnectionAdapter(INetworkConnection baseConnection, SslStream sslStream)
        {
            _baseConnection = baseConnection;
            _sslStream = sslStream;
        }

        public IPEndPoint RemoteEndPoint => _baseConnection.RemoteEndPoint;
        public int SendBufferSize => _baseConnection.SendBufferSize;
        public int ReceiveBufferSize => _baseConnection.ReceiveBufferSize;

        public Stream GetNetworkStream()
        {
            return _sslStream;
        }

        public void Close()
        {
            _sslStream.Close();
            _baseConnection.Close();
        }

        public void Dispose()
        {
            _sslStream.Dispose();
            _baseConnection.Dispose();
        }
    }
}
