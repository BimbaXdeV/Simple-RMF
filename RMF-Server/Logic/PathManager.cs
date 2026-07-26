using RMF.Core.Interfaces;
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
    internal class PathManager : IPathManager
    {
        private readonly Dictionary<string, string> _externalPaths = [];
        private readonly string _defaultStoragePath = "Undefined";

        private string _cachedDate = string.Empty;
        private DateTime _lastDateUpdated = DateTime.Now;
        private readonly Lock _cachedDateLock = new();

        public PathManager(Dictionary<string, string> externalPaths)
        {
            this._externalPaths = externalPaths;
        }

        private void UpdateDate()
        {
            lock (this._cachedDateLock)
            {
                DateTime actualDateTime = DateTime.Now;
                if (actualDateTime.Date != this._lastDateUpdated)
                {
                    this._cachedDate = actualDateTime.ToString("yyyy_MM_dd");
                    this._lastDateUpdated = actualDateTime.Date;
                }
            }
        }

        public string GetResolvedPath(
            string key,
            string? fileName = null,
            string? fileFormat = null,
            string? endPoint = null,
            bool UpdateCachedDate = false
        )
        {
            if (!this._externalPaths.TryGetValue(key, out string? rawPath) || string.IsNullOrEmpty(rawPath))
            {
                return this._defaultStoragePath;
            }

            if (UpdateCachedDate && DateTime.Now.Date != this._lastDateUpdated)
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
            resolvedPath.Replace("%date%", this._cachedDate)
                        .Replace("%time%", DateTime.Now.ToString("HH_mm_ss"))
                        .Replace("%datetime%", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                        .Replace("%endPoint%", string.IsNullOrEmpty(endPoint) ? "Unknown" : endPoint)

                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);

            return resolvedPath.ToString();
        }
    }
}
