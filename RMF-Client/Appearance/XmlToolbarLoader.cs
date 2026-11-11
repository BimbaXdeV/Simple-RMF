using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Client.Appearance
{
    internal static class XmlToolbarLoader
    {
        public static ToolbarItem[] Load(string toolbarPath)
        {
            if (!File.Exists(toolbarPath))
            {
                throw new FileNotFoundException($"The toolbar file was not found at path: {toolbarPath}");
            }

            XDocument toolbarDoc = XDocument.Load(toolbarPath);
            ToolbarItem[] toolbarItems = toolbarDoc.Element("Toolbar")?
                .Elements("add")
                .Select(x => new ToolbarItem(
                        x.Attribute("link")?.Value,
                        x.Attribute("name")?.Value,
                        x.Attribute("key")?.Value,
                        x.Attribute("default")?.Value
                    ))
                .ToArray() ?? [];

            return toolbarItems;
        }
    }
}
