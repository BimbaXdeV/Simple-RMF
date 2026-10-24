using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class FirewallConfig : RmfConfig
    {
        public int MaxConnections = int.MaxValue;
        public int MaxConnectionsPerIP = int.MaxValue;
        public int MaxPacketBufferKB = int.MaxValue;
        public int MaxPacketRate = int.MaxValue;
        public bool EnableBlacklistSaving = false;
        public string BlacklistFilePath = string.Empty;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (MaxConnections <= 0)
            {
                exceptionMessage = "MaxConnections must be greater than 0";
                return false;
            }
            if (MaxConnectionsPerIP <= 0)
            {
                exceptionMessage = "MaxConnectionsPerIP must be greater than 0";
                return false;
            }
            if (MaxConnectionsPerIP > MaxConnections)
            {
                exceptionMessage = "MaxConnectionsPerIP must be less than or equal to MaxConnections";
                return false;
            }
            if (MaxPacketBufferKB <= 0)
            {
                exceptionMessage = "MaxPacketBufferKB must be greater than 0";
                return false;
            }
            if (MaxPacketRate < 1)
            {
                exceptionMessage = "MaxPacketRate must be greater than or equal to 1";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<int>(configDict, nameof(MaxConnections), ref loadedFields, v => MaxConnections = v);
            ApplyValue<int>(configDict, nameof(MaxConnectionsPerIP), ref loadedFields, v => MaxConnectionsPerIP = v);
            ApplyValue<int>(configDict, nameof(MaxPacketBufferKB), ref loadedFields, v => MaxPacketBufferKB = v);
            ApplyValue<int>(configDict, nameof(MaxPacketRate), ref loadedFields, v => MaxPacketRate = v);
            ApplyValue<bool>(configDict, nameof(EnableBlacklistSaving), ref loadedFields, v => EnableBlacklistSaving = v);
            ApplyValue<string>(configDict, nameof(BlacklistFilePath), ref loadedFields, v => BlacklistFilePath = v);
            return loadedFields;
        }
    }
}
