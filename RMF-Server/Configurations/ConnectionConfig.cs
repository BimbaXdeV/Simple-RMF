using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ConnectionConfig : RmfConfig
    {
        public IPAddress IPAddress = new([0, 0, 0, 0]);
        public int Port = 1000;
        public int ReceiveTimeoutSecs = int.MaxValue;
        public bool EnableForceShutdown = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (Port < IPEndPoint.MinPort || Port > IPEndPoint.MaxPort)
            {
                exceptionMessage = $"Port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}";
                return false;
            }
            if (ReceiveTimeoutSecs <= 0)
            {
                exceptionMessage = "ReceiveTimeoutSecs must be greater than 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<IPAddress>(configDict, nameof(IPAddress), ref loadedFields, v => IPAddress = v);
            ApplyValue<int>(configDict, nameof(Port), ref loadedFields, v => Port = v);
            ApplyValue<int>(configDict, nameof(ReceiveTimeoutSecs), ref loadedFields, v => ReceiveTimeoutSecs = v);
            ApplyValue<bool>(configDict, nameof(EnableForceShutdown), ref loadedFields, v => EnableForceShutdown = v);
            return loadedFields;
        }
    }
}
