using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Logic
{
    internal static class ConfigurationManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "config.xml");

        public static string? AppTitle;
        public static string? WindowTitle;
        public static string? WindowTheme;
        public static int WindowPriority;
        public static int WindowWidth;
        public static int WindowHeight;

        public static string? IPAddress;
        public static int Port;
        public static int MaxConnections;
        public static int MinPacketBufferKB;
        public static int MaxPacketLengthKB;
        public static int MaxPacketRate;
        public static int ReceiveTimeoutSecs;

        public static bool EnableWelcomeHandshake;
        public static bool EnableClientHeartbeat;
        public static int ClientHeartbeatIntervalSecs;

        public static int ChannelPacketsCapacity;

        public static int ScreenshotFrameFormat;
        public static int ScreenshotQualityPercentage;
        public static int StreamingFrameFormat;
        public static int StreamingQualityPercentage;
        public static int StreamingFrameUpdateRate;
        public static int StreamingTargetFPS;
        public static bool EnableStreamingStatsOverlay;

        public static string? InlineCommandDefautSign;
        public static bool InlineSuggestionsEnabled;
        public static int InlineSuggestionsMinChars;

        public static int LoggingHistoryLength;
        public static int LoggingHandlerDelayMsecs;
        public static int InputListenerDelayMsecs;

        // You don't need to parse all the configs from "~\RMF-Server\Storage\config.xml" manually, this method will do it for you;
        // To scale, simply add empty fields with "public" and "static" flags  ;)
        public static (int, int) Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Logging.Error($"Unable to load configuration on path: {ConfigPath}");
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
                Logging.Error($"The configuration file has been corrupted. Please check its integrity on path: {ConfigPath}");
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
                Logging.Warning("No static fields were found for config entry");
                return (0, staticFields.Length);
            }

            return (initializedFieldsCounter, staticFields.Length);
        }
    }
}
