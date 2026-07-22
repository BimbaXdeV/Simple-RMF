using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Logic
{
    internal class ConfigurationManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "config.xml");

        public string? AppTitle;
        public string? WindowTitle;
        public string? WindowTheme;
        public int WindowPriority;
        public int WindowWidth;
        public static int WindowHeight;

        public string? IPAddress;
        public int Port;
        public int ReceiveTimeoutSecs;
        public bool EnableForceShutdown;

        public int MaxConnections;
        public int MaxConnectionsPerIP;
        public int MinPacketLengthKB;
        public int MaxPacketLengthKB;
        public int MaxPacketRate;
        public bool EnableBlacklistSaving;

        public string? CertificateName;
        public string? CertificateFileName;
        public string? CertificatePassword;
        public int CertificateDurationDays;
        
        public bool EnableCollectingSessionStats;
        public bool EnableWelcomeHandshake;
        public bool EnableBuildComparison;
        public bool EnableCollectingClientInfo;
        public bool EnableClientHeartbeat;
        public int ClientHeartbeatIntervalSecs;
        public bool EnableRelativeParting;
        public int PartingTimeoutSecs;

        public int ChannelPacketsCapacity;

        public int ScreenshotFrameFormat;
        public int ScreenshotQualityPercentage;
        public int StreamingFrameFormat;
        public int StreamingQualityPercentage;
        public int StreamingFrameUpdateRate;
        public int StreamingTargetFPS;
        public bool EnableStreamingStatsOverlay;

        public string? InlineCommandDefautSign;
        public bool InlineSuggestionsEnabled;
        public int InlineSuggestionsMinChars;

        public bool EnableLogSaving;
        public bool EnableMultipleBackup;
        public int MaxLogFileCapacityMB;
        public int LoggingHistoryLength;
        public int LoggingHandlerDelayMsecs;
        public int InputListenerDelayMsecs;

        // You don't need to parse all the configs from "~\RMF-Server\Storage\config.xml" manually, this method will do it for you;
        // To scale, simply add empty fields with "public" and "static" flags  ;)
        public (int, int) Load()
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
            foreach (FieldInfo field in staticFields)
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
