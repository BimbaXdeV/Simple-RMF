using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public abstract class RmfConfig : IConfig
    {
        public abstract bool ValidateFields(out string? exceptionMessage);
        public abstract int LoadFields(Dictionary<string, object> configDict);

        protected static void ApplyValue<T>(IReadOnlyDictionary<string, object> configDict, string key, ref int loadCounter, Action<T> setter)
        {
            if (configDict.TryGetValue(key, out object? value) && value != null)
            {
                if (value is T typedValue)
                {
                    setter(typedValue);
                    loadCounter++;
                    return;
                }
                try
                {
                    T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                    setter(convertedValue);
                    loadCounter++;
                }
                catch
                {
                }
            }
        }
    }
}
