using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF.Core.Loaders
{
    internal class XmlConfigLoader
    {
        private readonly Dictionary<string, string> _cachedConfig;

        public XmlConfigLoader(string configPath)
        {
            XDocument configDoc = XDocument.Load(configPath);
            _cachedConfig = configDoc.Element("Settings")?
                .Elements("add")
                .ToDictionary(
                    x => x.Attribute("key")?.Value ?? "",
                    x => x.Attribute("value")?.Value ?? ""
                 ) ?? [];
        }

        public T GetConfig<T>() where T : new()
        {
            T instance = new();
            Type type = typeof(T);

            FieldInfo[] instanceFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in instanceFields)
            {
                if (_cachedConfig.TryGetValue(field.Name, out string? rawValue))
                {
                    object processedValue = Convert.ChangeType(rawValue, field.FieldType);
                    field.SetValue(instance, processedValue);
                }
            }
            return instance;
        }
    }
}
