using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
{
    public interface IConnectionClientSession : ISession
    {
        DateTime ConnectedTime { get; }

        bool IsEventRunning(string eventName);
        void StopEvent(string eventName);
        void StopAllEvents();
    }
}
