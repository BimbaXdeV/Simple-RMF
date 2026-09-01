using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public class XmlConfigProvider
    {
        private readonly Dictionary<Type, object> _configInstances;

        public XmlConfigProvider(Dictionary<Type, object> configInstances)
        {
            this._configInstances = configInstances;
        }

        public T GetConfig<T>() where T : new()
        {
            if (this._configInstances.TryGetValue(typeof(T), out object? instance))
            {
                return (T)instance;
            }
            else
            {
                T newInstance = new();
                this._configInstances[typeof(T)] = newInstance;
                return newInstance;
            }
        }
    }
}
