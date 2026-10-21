using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF.Core.Loaders
{
    public static class XmlConfigLoader
    {
        public static (Dictionary<Type, object> Instances, int LoadedFields) Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"The config file was not found at path: {configPath}");
            }

            XDocument configDoc = XDocument.Load(configPath);
            Dictionary<string, string> configDict = configDoc.Element("Settings")?
                .Elements("add")
                .ToDictionary(
                    x => x.Attribute("key")?.Value ?? "",
                    x => x.Attribute("value")?.Value ?? ""
                ) ?? [];

            if (configDict.Count == 0)
            {
                throw new InvalidOperationException("No configuration settings found in the config file");
            }


            IEnumerable<Type>? configTypes = (Assembly.GetEntryAssembly()?
                                                      .GetTypes()
                                                      .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Config")))
                                                      ?? throw new InvalidOperationException("No configuration classes found in the assembly");

            Dictionary<Type, object> configInstances = [];
            int loadedFieldsCount = 0;
            foreach (Type configType in configTypes)
            {
                object? configInstance = Activator.CreateInstance(configType) ?? throw new InvalidOperationException($"Failed to create an instance of the configuration class: {configType.FullName}");
                FieldInfo[] configFields = configType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in configFields)
                {
                    if (configDict.TryGetValue(field.Name, out string? rawValue))
                    {
                        try
                        {
                            object processedValue = Convert.ChangeType(rawValue, field.FieldType);
                            field.SetValue(configInstance, processedValue);
                            loadedFieldsCount++;
                        }
                        catch
                        {
                            // Just skip and continue next iteration
                        }
                    }
                }
                configInstances.TryAdd(configType, configInstance);
            }

            return (configInstances, loadedFieldsCount);
        }
    }
}
