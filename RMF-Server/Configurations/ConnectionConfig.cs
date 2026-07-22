using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ConnectionConfig
    {
        public string? IPAddress;
        public int Port;
        public int ReceiveTimeoutSecs;
        public bool EnableForceShutdown;
    }
}
