using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class ConnectionConfig
    {
        public string IPAddress = "0.0.0.0";
        public int Port = 1000;
        public int ConnectionRequestIntervalSecs = 60;
        public bool EnableForceShutdown = false;
    }
}
