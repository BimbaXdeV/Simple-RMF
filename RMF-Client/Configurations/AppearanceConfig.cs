using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class AppearanceConfig : RmfConfig
    {
        public string AppTitle = "Client";
        public int CurtainStepDelayMsecs = 10;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (string.IsNullOrWhiteSpace(AppTitle))
            {
                exceptionMessage = "AppTitle cannot be empty";
                return false;
            }
            if (CurtainStepDelayMsecs < 0)
            {
                exceptionMessage = "CurtainStepDelayMsecs must be greater than or equal to 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<string>(configDict, nameof(AppTitle), ref loadedFields, v => AppTitle = v);
            ApplyValue<int>(configDict, nameof(CurtainStepDelayMsecs), ref loadedFields, v => CurtainStepDelayMsecs = v);
            return loadedFields;
        }
    }
}
