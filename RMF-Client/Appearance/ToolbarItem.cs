using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Appearance
{
    internal class ToolbarItem
    {
        public string Link { get; }
        public string Name { get; }
        public string? Key { get; }
        public string DefaultValue { get; }

        public ToolbarItem(string? link, string? name, string? key, string? defaultValue)
        {
            Link = link ?? "unknownLink";
            Name = name ?? "Unknown";
            Key = string.IsNullOrEmpty(key) ? null : key;
            DefaultValue = string.IsNullOrEmpty(defaultValue) ? "Not found" : defaultValue;
        }
    }
}
