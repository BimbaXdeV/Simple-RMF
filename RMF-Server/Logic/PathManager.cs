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

        private static string CachedDate = string.Empty;
        private static DateTime LastDateUpdated = DateTime.Now;
        private static readonly Lock CachedDateLock = new();

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

        private static void UpdateDate()
        {
            lock (CachedDateLock)
            {
                DateTime actualDateTime = DateTime.Now;
                if (actualDateTime.Date != LastDateUpdated)
                {
                    CachedDate = actualDateTime.ToString("yyyy_MM_dd");
                    LastDateUpdated = actualDateTime.Date;
                }
            }
        }

        public static string GetResolvedPath(string key, string? fileName = null, string? fileFormat = null, string? endPoint = null, bool UpdateCachedDate = false)
        {
            if (!ExternalPaths.TryGetValue(key, out string? rawPath) || string.IsNullOrEmpty(rawPath))
            {
                return DefaultStoragePath;
            }

            if (UpdateCachedDate && DateTime.Now.Date != LastDateUpdated)
            {
                UpdateDate();  // You don`t need to constantly convert the same long-lived object
            }

            string? fullFilePath;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string format = string.IsNullOrWhiteSpace(fileFormat) ? "txt" : fileFormat.TrimStart('.');
                fullFilePath = Path.Combine(rawPath, $"{fileName}.{format}");
            }
            else
            {
                fullFilePath = rawPath;
            }

            StringBuilder resolvedPath = new(fullFilePath);
            resolvedPath.Replace("%date%", CachedDate)
                        .Replace("%time%", DateTime.Now.ToString("HH_mm_ss"))
                        .Replace("%datetime%", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                        .Replace("%endPoint%", string.IsNullOrEmpty(endPoint) ? "Unknown" : endPoint)

                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);

            return resolvedPath.ToString();
        }
    }
}
