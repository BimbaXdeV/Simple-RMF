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
        public static LoadResult<ToolbarItem[]> Load(string toolbarPath)
        {
            if (!File.Exists(toolbarPath))
            {
                return LoadResult<ToolbarItem[]>.Failure($"Failed to load toolbar, file not found: {toolbarPath}");
            }

            try
            {
                XDocument toolbarDoc = XDocument.Load(toolbarPath);
                ToolbarItem[] toolbarItems = toolbarDoc.Element("Toolbar")?
                    .Elements("add")
                    .Select(x => new ToolbarItem(x.Attribute("link")?.Value, x.Attribute("name")?.Value, x.Attribute("key")?.Value, x.Attribute("default")?.Value))
                    .ToArray() ?? [];

                return LoadResult<ToolbarItem[]>.Success(toolbarItems, toolbarItems.Length, toolbarItems.Length);
            }
            catch (Exception ex)
            {
                return LoadResult<ToolbarItem[]>.Failure(ex.Message);
            }
        }
    }
}
