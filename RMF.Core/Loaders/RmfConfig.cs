using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public abstract class RmfConfig : IConfig
    {
        private int _loadedFields = 0;
        public int LoadedFields => _loadedFields;

        public abstract bool ValidateFields(out string? exceptionMessage);
        public abstract void LoadFields(IReadOnlyDictionary<string, string> configDict);

        protected void ApplyValue<T>(IReadOnlyDictionary<string, string> configDict, string key, Action<T> setter)
        {
            if (configDict.TryGetValue(key, out string? value) && value != null)
            {
                if (value is T typedValue)
                {
                    setter(typedValue);
                    _loadedFields++;
                    return;
                }

                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(string)))
                {
                    T convertedValue = (T)converter.ConvertFromString(value)!;
                    setter(convertedValue);
                    _loadedFields++;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot convert \"{value}\" to type {typeof(T).Name}");
                }
            }
        }
    }
}
