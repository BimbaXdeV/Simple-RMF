using Avalonia.Utilities;
using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Loaders;
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
        public static Dictionary<string, ThemeColor> Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The theme file was not found at path: {path}");
            }

            XDocument themeDoc = XDocument.Load(path);
            Dictionary<string, Dictionary<string, string?>>? themeDict = (themeDoc.Element("ColorTheme")?
                .Elements("add")
                .ToDictionary(
                    x => x.Attribute("key")?.Value ?? "",
                    x => new Dictionary<string, string?>
                    {
                        { "R", x.Attribute("R")?.Value },
                        { "G", x.Attribute("G")?.Value },
                        { "B", x.Attribute("B")?.Value }
                    }
                ))
                ?? throw new InvalidOperationException("No color theme settings found in the theme file");

            Dictionary<string, ThemeColor> theme = [];
            foreach (KeyValuePair<string, Dictionary<string, string?>> color in themeDict)
            {
                Dictionary<string, string?> channels = color.Value;
                if (byte.TryParse(channels["R"], out byte r) &&
                    byte.TryParse(channels["G"], out byte g) &&
                    byte.TryParse(channels["B"], out byte b))
                {
                    theme.TryAdd(color.Key, new ThemeColor(r, g, b));
                }
            }

            return theme;
        }
    }
}
