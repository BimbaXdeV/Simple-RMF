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
        //private readonly Dictionary<string, string> _cachedConfig;

        public static LoadResult<Dictionary<Type, object>> Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return LoadResult<Dictionary<Type, object>>.Failure($"Unable to load config on path: {configPath}");
            }

            try
            {
                XDocument configDoc = XDocument.Load(configPath);
                Dictionary<string, string> configDict = configDoc.Element("Settings")?
                    .Elements("add")
                    .ToDictionary(
                        x => x.Attribute("key")?.Value ?? "",
                        x => x.Attribute("value")?.Value ?? ""
                    ) ?? [];

                if (configDict.Count == 0)
                {
                    return LoadResult<Dictionary<Type, object>>.Failure($"The config file has been corrupted. Please check its integrity on path: {configPath}");
                }


                IEnumerable<Type> configTypes = Assembly.GetExecutingAssembly()
                                                        .GetTypes()
                                                        .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Config"));
                Dictionary<Type, object> configInstances = [];
                int loadedConfigsCount = 0;
                foreach (Type configType in configTypes)
                {
                    object? configInstance = Activator.CreateInstance(configType);
                    if (configInstance == null)
                    {
                        return LoadResult<Dictionary<Type, object>>.Failure($"Failed to create an instance of {configType.Name}");
                    }

                    FieldInfo[] configFields = configType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (FieldInfo field in configFields)
                    {
                        if (configDict.TryGetValue(field.Name, out string? rawValue))
                        {
                            try
                            {
                                object processedValue = Convert.ChangeType(rawValue, field.FieldType);
                                field.SetValue(configInstance, processedValue);
                                loadedConfigsCount++;
                            }
                            catch
                            {
                                // Just skip and continue next iteration
                            }
                        }
                    }
                    configInstances.TryAdd(configType, configInstance);
                }

                return LoadResult<Dictionary<Type, object>>.Success(configInstances, configDict.Count);
            }
            catch (Exception ex)
            {
                return LoadResult<Dictionary<Type, object>>.Failure(ex.Message);
            }
        }
    }
}
