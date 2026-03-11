using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Logic
{
    internal static class PathManager
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "extpaths.xml");
        private static readonly Dictionary<string, string> ExternalPaths = [];
        private static readonly string DefaultStoragePath = "Undefined";

        public static (int, int) Load()
        {
            if (!File.Exists(FilePath))
            {
                Logging.Error($"Unable to load external paths on path: {FilePath}");
                return (0, 0);
            }

            XDocument pathsDoc = XDocument.Load(FilePath);
            var pathsDict = pathsDoc.Element("Paths")?.Elements("add");

            if (pathsDict == null)
            {
                Logging.Error($"The external paths file has been corrupted. Please check its integrity");
                return (0, 0);
            }

            int initializedPathsCounter = 0;
            foreach (var el in pathsDict)
            {
                string? pathKey = el.Attribute("key")?.Value;
                if (pathKey != null)
                {
                    ExternalPaths[pathKey] = el.Attribute("path")?.Value ?? "Undefined";
                    initializedPathsCounter++;
                }
            }
            return (initializedPathsCounter, pathsDict.Count());
        }

        public static string GetResolvedPath(string key, string endPoint)
        {
            if (!ExternalPaths.TryGetValue(key, out string? rawPath) || string.IsNullOrEmpty(rawPath))
            {
                return DefaultStoragePath;
            }

            StringBuilder resolvedPath = new(rawPath);
            resolvedPath.Replace("%date%", DateTime.Now.ToString("yyyy_MM_dd"))
                        .Replace("%time%", DateTime.Now.ToString("HH_mm_ss"))
                        .Replace("%datetime%", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                        .Replace("%endPoint%", string.IsNullOrEmpty(endPoint) ? "Unknown" : endPoint)

                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);

            return resolvedPath.ToString();
        }
    }
}
