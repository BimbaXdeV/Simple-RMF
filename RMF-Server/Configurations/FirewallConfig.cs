using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class FirewallConfig
    {
        public int MaxConnections = 1;
        public int MaxConnectionsPerIP = 1;
        public int MinPacketLengthKB = 0;
        public int MaxPacketLengthKB = 1;
        public int MaxPacketRate = 1;
        public bool EnableBlacklistSaving = false;
    }
}
