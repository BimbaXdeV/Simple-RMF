using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class ConnectionConfig : RmfConfig
    {
        public IPAddress IPAddress = new([0, 0, 0, 0]);
        public int Port = 1000;
        public int MaxConnectionAttempts = int.MaxValue;
        public int ConnectionRequestDelaySecs = 1;
        public bool EnableForceShutdown = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (Port < IPEndPoint.MinPort || Port > IPEndPoint.MaxPort)
            {
                exceptionMessage = "Port must be between 1 and 65535";
                return false;
            }
            if (MaxConnectionAttempts <= 0)
            {
                exceptionMessage = "MaxConnectionAttempts must be greater than 0";
                return false;
            }
            if (ConnectionRequestDelaySecs <= 0)
            {
                exceptionMessage = "ConnectionRequestIntervalSecs must be greater than 0";
                return false;
            }
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<IPAddress>(configDict, nameof(IPAddress), v => IPAddress = v);
            ApplyValue<int>(configDict, nameof(Port), v => Port = v);
            ApplyValue<int>(configDict, nameof(MaxConnectionAttempts), v => MaxConnectionAttempts = v);
            ApplyValue<int>(configDict, nameof(ConnectionRequestDelaySecs), v => ConnectionRequestDelaySecs = v);
            ApplyValue<bool>(configDict, nameof(EnableForceShutdown), v => EnableForceShutdown = v);
        }
    }
}
