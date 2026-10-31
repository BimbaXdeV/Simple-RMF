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
        public static Dictionary<string, string> Load(string configPath)
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
                )
                ?? throw new InvalidOperationException("No configuration settings found in the config file");

            return configDict;
        }
    }
}
