using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
{
    public interface IServerClientSession : ISession
    {
        bool IsRateLimitExceed(int maxRate);
    }
}
