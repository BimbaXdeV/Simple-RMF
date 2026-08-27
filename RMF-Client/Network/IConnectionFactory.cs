using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Network
{
    internal interface IConnectionFactory
    {
        INetworkConnection CreateConnection();
    }
}
