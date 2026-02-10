using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal static class ConfigurationManager
    {
        public static string? AppTitle;
        
        public static string? IPAddress;
        public static int Port;
        public static int ConnectionRequestTimeoutSecs;
    }
}
