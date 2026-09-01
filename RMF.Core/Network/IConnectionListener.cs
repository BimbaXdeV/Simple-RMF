using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface IConnectionListener
    {
        IPEndPoint ListenedEndPoint { get; }

        void Start();
        void Stop();

        Task<INetworkConnection> AcceptConnectionAsync(CancellationToken token);
    }
}
