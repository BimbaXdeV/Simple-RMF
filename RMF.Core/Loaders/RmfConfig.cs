using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

        protected void ApplyValue(IReadOnlyDictionary<string, string> configDict, string key, Action<string> setter)
        {
            if (configDict.TryGetValue(key, out string? value) && value != null)
            {
                setter(value);
                _loadedFields++;
            }
        }

        protected void ApplyValue<T>(IReadOnlyDictionary<string, string> configDict, string key, Action<T> setter)
            where T : IParsable<T>
        {
            if (configDict.TryGetValue(key, out string? value) && value != null)
            {
                if (T.TryParse(value, CultureInfo.InvariantCulture, out T? convertedValue))
                {
                    setter(convertedValue);
                    _loadedFields++;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot convert \"{value}\" to type {typeof(T).Name}");
                }
            }
        }

        protected void ApplyEnum<TEnum>(IReadOnlyDictionary<string, string> configDict, string key, Action<TEnum> setter)
            where TEnum : struct, Enum
        {
            if (configDict.TryGetValue(key, out string? value) && value != null)
            {
                if (Enum.TryParse(value, true, out TEnum convertedValue))
                {
                    setter(convertedValue);
                    _loadedFields++;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot convert \"{value}\" to enum type {typeof(TEnum).Name}");
                }
            }
        }
    }
}
