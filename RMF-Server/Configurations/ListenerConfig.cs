using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ListenerConfig : RmfConfig
    {
        private const int _minListenerDelayMsecs = 10;

        public int ListenerDelayMsecs = 100;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (ListenerDelayMsecs < _minListenerDelayMsecs)
            {
                exceptionMessage = $"ListenerDelayMsecs must be greater than or equal to {_minListenerDelayMsecs}";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<int>(configDict, nameof(ListenerDelayMsecs), ref loadedFields, v => ListenerDelayMsecs = v);
            return loadedFields;
        }
    }
}
