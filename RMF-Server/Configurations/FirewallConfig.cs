using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class FirewallConfig
    {
        public int MaxConnections = int.MaxValue;
        public int MaxConnectionsPerIP = int.MaxValue;
        public int MinPacketLengthKB = 0;
        public int MaxPacketLengthKB = int.MaxValue;
        public int MaxPacketRate = int.MaxValue;
        public bool EnableBlacklistSaving = false;
        public string BlacklistFilePath = string.Empty;
    }
}
