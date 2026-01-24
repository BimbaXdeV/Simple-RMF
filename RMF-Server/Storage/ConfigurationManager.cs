using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Storage
{
    internal static class ConfigurationManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "config.xml");

        public static string? AppTitle;

        public static string? IPAddress;
        public static int Port;
        public static int MaxConnections;
        public static int MaxPacketMemoryLimitKB;
        public static int PacketsListenDelayMsecs;

        public static int ScreenshotQualityPercentage;
        public static int DesktopSendingIntervalMsecs;

        public static int LoggingHandlerDelayMsecs;
        public static int InputListenerDelayMsecs;

        // You don't need to parse all the configs from "~\RMF-Server\Storage\config.xml" manually, this method will do it for you;
        // To scale, simply add empty fields with "public" and "static" flags  ;)
        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Logging.Error($"Unable to load configuration on path: {ConfigPath}");
                return;
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
                Logging.Error($"The configuration file has been corrupted. Please check its integrity");
                return;
            }

            Type type = typeof(ConfigurationManager);
            FieldInfo[] staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            int initializedFieldsCounter = 0;
            foreach (var fiend in staticFields)
            {
                if (configDict.TryGetValue(fiend.Name, out string? rawValue))
                {
                    object processedValue = Convert.ChangeType(rawValue, fiend.FieldType);
                    fiend.SetValue(null, processedValue);
                    initializedFieldsCounter++;
                }
            }

            if (initializedFieldsCounter == 0)
            {
                Logging.Warning("No static fields were found for config entry");
                return;
            }

            Logging.Output($"Configuration successfully loaded: {initializedFieldsCounter} / {staticFields.Length} fields filled");
        }
    }
}
