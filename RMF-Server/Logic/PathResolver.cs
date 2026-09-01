using Avalonia.Markup.Xaml.MarkupExtensions;
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
    internal static class PathResolver
    {
        public static string GetResolvedPath(
            string filePath,
            string fileName = "unnamed",
            string fileFormat = "txt",
            string endPoint = "unknown"
        )
        {
            if (string.IsNullOrWhiteSpace(filePath) || filePath == "/")
            {
                return fileName + '.' + fileFormat;
            }

            string fullFilePath = Path.Combine(filePath, fileName + '.' + fileFormat);

            StringBuilder resolvedPath = new(fullFilePath);
            DateTime currentTime = DateTime.Now;
            resolvedPath.Replace("%date%", currentTime.ToString("yyyy_MM_dd"))
                        .Replace("%time%", currentTime.ToString("HH_mm_ss"))
                        .Replace("%datetime%", currentTime.ToString("yyyyMMdd_HHmmss"))
                        .Replace("%endPoint%", endPoint)

                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);

            return resolvedPath.ToString();
        }
    }
}
