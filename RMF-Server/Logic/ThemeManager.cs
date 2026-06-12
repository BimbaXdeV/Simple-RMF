using Avalonia.Logging;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Logic
{
    internal class ThemeManager
    {
        private static readonly string ThemePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "theme.xml");
        private static readonly byte[] DefaultColor = [255, 255, 255, 255];

        public static byte[] SuccessfullyLoadedCounter = [.. DefaultColor];
        public static byte[] PartiallyLoadedCounter = [.. DefaultColor];
        public static byte[] FailedToLoadCounter = [.. DefaultColor];

        public static byte[] AdminInput = [.. DefaultColor];
        public static byte[] AdminSuggestion = [.. DefaultColor];

        public static byte[] CommandName = [.. DefaultColor];
        public static byte[] ParameterName = [.. DefaultColor];

        public static byte[] OutputDatetime = [.. DefaultColor];
        public static byte[] WarningLog = [.. DefaultColor];
        public static byte[] ErrorLog = [.. DefaultColor];
        public static byte[] Separator = [.. DefaultColor];

        public static (int, int) Load()
        {
            if (!File.Exists(ThemePath))
            {
                Logging.Error($"Unable to load theme on path: {ThemePath}");
                return (0, 0);
            }

            XDocument themeDoc = XDocument.Load(ThemePath);
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
                Logging.Error($"The theme file has been corrupted. Please check its integrity on path: {ThemePath}");
                return (0, 0);
            }

            Type type = typeof(ThemeManager);
            FieldInfo[] staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            int initializedFieldsCounter = 0;
            foreach (FieldInfo field in staticFields)
            {
                if (!themeDict.TryGetValue(field.Name, out Dictionary<string, string?>? colorDict))
                {
                    Console.WriteLine($"Color for field \"{field.Name}\" is not defined in the theme file. Using default color.");
                    continue;
                }

                byte[] colorArray = ParseColor(colorDict);
                field.SetValue(null, colorArray);
                initializedFieldsCounter++;
            }

            return (initializedFieldsCounter, staticFields.Length);
        }

        private static byte[] ParseColor(Dictionary<string, string?> colorComponents)
        {
            byte rChannel = colorComponents.TryGetValue("R", out string? rawR) && byte.TryParse(rawR, out byte r) ? r : DefaultColor[0];
            byte gChannel = colorComponents.TryGetValue("G", out string? rawG) && byte.TryParse(rawG, out byte g) ? g : DefaultColor[1];
            byte bChannel = colorComponents.TryGetValue("B", out string? rawB) && byte.TryParse(rawB, out byte b) ? b : DefaultColor[2];
            byte aChannel = colorComponents.TryGetValue("A", out string? rawA) && byte.TryParse(rawA, out byte a) ? a : DefaultColor[3];
            return [rChannel, gChannel, bChannel, aChannel];
        }
    }
}
