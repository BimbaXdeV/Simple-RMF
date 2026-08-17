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
        public static LoadResult<Dictionary<string, ThemeColor>> Load(string path)
        {
            if (!File.Exists(path))
            {
                return LoadResult<Dictionary<string, ThemeColor>>.Failure($"Unable to load theme on path: {path}");
            }

            try
            {
                XDocument themeDoc = XDocument.Load(path);
                Dictionary<string, Dictionary<string, string?>>? themeDict = themeDoc.Element("ColorTheme")?
                    .Elements("add")
                    .ToDictionary(
                        x => x.Attribute("key")?.Value ?? "",
                        x => new Dictionary<string, string?>
                        {
                            { "R", x.Attribute("R")?.Value },
                            { "G", x.Attribute("G")?.Value },
                            { "B", x.Attribute("B")?.Value }
                        }
                    );

                if (themeDict == null)
                {
                    return LoadResult<Dictionary<string, ThemeColor>>.Failure($"The theme file has been corrupted. Please check its integrity on path: {path}");
                }

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

                return LoadResult<Dictionary<string, ThemeColor>>.Success(theme, theme.Count, themeDict.Count);
            }
            catch (Exception ex)
            {
                return LoadResult<Dictionary<string, ThemeColor>>.Failure(ex.Message);
            }
        }
    }
}
