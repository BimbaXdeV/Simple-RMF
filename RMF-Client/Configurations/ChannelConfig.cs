using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
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

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<int>(configDict, nameof(ChannelPacketsCapacity), v => ChannelPacketsCapacity = v);
        }
    }
}
