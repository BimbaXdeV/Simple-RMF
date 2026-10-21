using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public class XmlConfigProvider : IConfigViewer
    {
        private readonly Dictionary<Type, object> _configInstances;
        private readonly int _loadedFields;

        public int RegisteredCategoriesCount => _configInstances.Count;
        public int RegisteredFieldsCount => _loadedFields;

        public XmlConfigProvider(Dictionary<Type, object> configInstances, int loadedFields)
        {
            _configInstances = configInstances;
            _loadedFields = loadedFields;
        }

        public T GetConfig<T>() where T : new()
        {
            if (_configInstances.TryGetValue(typeof(T), out object? instance))
            {
                return (T)instance;
            }
            else
            {
                T newInstance = new();
                _configInstances[typeof(T)] = newInstance;
                return newInstance;
            }
        }
    }
}
