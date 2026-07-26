using Avalonia.Utilities;
using RMF.Core.Interfaces;
using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Debugger
{
    internal static class XmlThemeLoader
    {
        public static (Dictionary<string, ThemeColor> Data, int Total) Load(string path, ILoggingEngine? logger = null)
        {
            if (!File.Exists(path))
            {
                logger?.Error($"Unable to load theme on path: {path}");
                return ([], 0);
            }

            XDocument themeDoc = XDocument.Load(path);
            Dictionary<string, Dictionary<string, string?>>? themeDict = themeDoc.Element("ColorTheme")?
                .Elements("add")
                .ToDictionary(
                    x => x.Attribute("key")?.Value ?? "",
                    x => new Dictionary<string, string?>
                    {
                        { "R", x.Attribute("R")?.Value },
                        { "G", x.Attribute("G")?.Value },
                        { "B", x.Attribute("B")?.Value },
                        { "A", x.Attribute("A")?.Value }
                    }
                );

            if (themeDict == null)
            {
                logger?.Error($"The theme file has been corrupted. Please check its integrity on path: {path}");
                return ([], 0);
            }

            Dictionary<string, ThemeColor> theme = [];
            foreach (KeyValuePair<string, Dictionary<string, string?>> color in themeDict)
            {
                Dictionary<string, string?> channels = color.Value;
                if (byte.TryParse(channels["R"], out byte r) &&
                    byte.TryParse(channels["G"], out byte g) &&
                    byte.TryParse(channels["B"], out byte b) &&
                    byte.TryParse(channels["A"], out byte a))
                {
                    theme.TryAdd(color.Key, new ThemeColor(r, g, b, a));
                }
            }

            return (theme, themeDict.Count);
        }
    }
}
