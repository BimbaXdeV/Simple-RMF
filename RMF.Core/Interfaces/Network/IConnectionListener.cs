using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IConnectionListener
    {
        void Start();
        void Stop();

        Task<INetworkConnection> AcceptConnectionAsync(CancellationToken token);
    }
}
