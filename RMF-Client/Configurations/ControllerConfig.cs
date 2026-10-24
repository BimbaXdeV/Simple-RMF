using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class ControllerConfig : RmfConfig
    {
        public bool EnableCollectingSessionStats = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<bool>(configDict, nameof(EnableCollectingSessionStats), ref loadedFields, v => EnableCollectingSessionStats = v);
            return loadedFields;
        }
    }
}
