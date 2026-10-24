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
        public int ConnectionRequestIntervalSecs = 1;
        public bool EnableForceShutdown = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (Port < 1 || Port > 65535)
            {
                exceptionMessage = "Port must be between 1 and 65535";
                return false;
            }
            if (ConnectionRequestIntervalSecs <= 0)
            {
                exceptionMessage = "ConnectionRequestIntervalSecs must be greater than 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<IPAddress>(configDict, nameof(IPAddress), ref loadedFields, v => IPAddress = v);
            ApplyValue<int>(configDict, nameof(Port), ref loadedFields, v => Port = v);
            ApplyValue<int>(configDict, nameof(ConnectionRequestIntervalSecs), ref loadedFields, v => ConnectionRequestIntervalSecs = v);
            ApplyValue<bool>(configDict, nameof(EnableForceShutdown), ref loadedFields, v => EnableForceShutdown = v);
            return loadedFields;
        }
    }
}
