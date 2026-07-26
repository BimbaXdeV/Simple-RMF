using RMF.Core.Interfaces;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Logic
{
    internal static class XmlPathLoader
    {
        public static (Dictionary<string, string>, int Total) Load(string path, ILoggingEngine? logger = null)
        {
            if (!File.Exists(path))
            {
                logger?.Error($"Unable to load external paths on path: {path}");
                return ([], 0);
            }

            XDocument pathsDoc = XDocument.Load(path);
            var pathsDict = pathsDoc.Element("Paths")?.Elements("add");

            if (pathsDict == null)
            {
                logger?.Error($"The external paths file has been corrupted. Please check its integrity");
                return ([], 0);
            }

            Dictionary<string, string> externalPaths = [];
            foreach (var el in pathsDict)
            {
                string? pathKey = el.Attribute("key")?.Value;
                if (pathKey != null)
                {
                    externalPaths[pathKey] = el.Attribute("path")?.Value ?? "Undefined";
                }
            }
            return (externalPaths, pathsDict.Count());
        }
    }
}
