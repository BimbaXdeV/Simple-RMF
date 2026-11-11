using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public class RmfConfigProvider : IConfigViewer
    {
        private readonly IReadOnlyDictionary<string, string> _cachedRawConfig;
        private readonly Dictionary<Type, IConfig> _configInstances;

        public int RegisteredCategoriesCount => _configInstances.Count;
        public int RegisteredFieldsCount => _configInstances.Values.Sum(c => c.LoadedFields);

        public RmfConfigProvider(IReadOnlyDictionary<string, string> configDict)
        {
            _cachedRawConfig = configDict;
            _configInstances = [];
        }

        public T GetConfig<T>() where T : IConfig, new()
        {
            if (_configInstances.TryGetValue(typeof(T), out IConfig? instance))
            {
                return (T)instance;
            }
            else
            {
                T newInstance = new();
                newInstance.LoadFields(_cachedRawConfig);
                _configInstances[typeof(T)] = newInstance;
                return newInstance;
            }
        }
    }
}
