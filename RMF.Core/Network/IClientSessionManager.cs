using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface IClientSessionManager
    {
        bool IsConnected { get; }

        void StartSession(INetworkConnection connection);
        IConnectionClientSession? GetRunningSession();
        void StopSession();
    }
}
