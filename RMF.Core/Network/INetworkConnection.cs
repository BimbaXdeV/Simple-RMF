using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface INetworkConnection : IDisposable
    {
        IPEndPoint RemoteEndPoint { get; }
        int SendBufferSize { get; }
        int ReceiveBufferSize { get; }

        Stream GetNetworkStream();
        void Close();
    }
}
