using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Client.Logic
{
    internal static class ConfigurationManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "config.xml");

        public static string? AppTitle;
        
        public static string? IPAddress;
        public static int Port;
        public static int ConnectionRequestTimeoutSecs;
        public static int MinPacketBufferKB;
        public static int MaxPacketLengthKB;
        public static bool EnableForceShutdown;

        public static bool EnableCollectingSessionStats;

        public static int ChannelPacketsCapacity;

        public static int MaxProcessorCores;
        public static int MetricsUpdateRate;
        public static int DesktopPatchesBufferSize;

        // You don't need to parse all the configs from "~\RMF-Client\Storage\config.xml" manually, this method will do it for you;
        // To scale, simply add empty fields with "public" and "static" flags  ;)
        public static (int, int) Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine($"Unable to load configuration on path: {ConfigPath}");
                return (0, 0);
            }

            XDocument configDoc = XDocument.Load(ConfigPath);
            Dictionary<string, string>? configDict = configDoc.Element("Settings")?
                .Elements("add")
                .ToDictionary(
                    x => x.Attribute("key")?.Value ?? "",
                    x => x.Attribute("value")?.Value ?? ""
                );

            if (configDict == null)
            {
                Console.WriteLine($"The configuration file has been corrupted. Please check its integrity on path: {ConfigPath}");
                return (0, 0);
            }

            Type type = typeof(ConfigurationManager);
            FieldInfo[] staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            int initializedFieldsCounter = 0;
            foreach (var field in staticFields)
            {
                if (configDict.TryGetValue(field.Name, out string? rawValue))
                {
                    object processedValue = Convert.ChangeType(rawValue, field.FieldType);
                    field.SetValue(null, processedValue);
                    initializedFieldsCounter++;
                }
            }

            if (initializedFieldsCounter == 0)
            {
                Console.WriteLine("No static fields were found for config entry");
                return (0, staticFields.Length);
            }

            return (initializedFieldsCounter, staticFields.Length);
        }
    }
}
