using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class FirewallConfig
    {
        public int MaxConnections;
        public int MaxConnectionsPerIP;
        public int MinPacketLengthKB;
        public int MaxPacketLengthKB;
        public int MaxPacketRate;
        public bool EnableBlacklistSaving;
    }
}
