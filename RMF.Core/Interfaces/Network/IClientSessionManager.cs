using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IClientSessionManager
    {
        bool IsConnected { get; }

        void StartSession(INetworkConnection connection);
        IConnectionClientSession? GetRunningSession();
        void StopSession();
    }
}
