using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ChannelConfig : RmfConfig
    {
        public int ChannelPacketsCapacity = 1;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (ChannelPacketsCapacity <= 0)
            {
                exceptionMessage = $"ChannelPacketsCapacity must be greater than 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<int>(configDict, nameof(ChannelPacketsCapacity), ref loadedFields, v => ChannelPacketsCapacity = v);
            return loadedFields;
        }
    }
}
