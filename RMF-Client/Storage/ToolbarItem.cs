using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Storage
{
    internal class ToolbarItem
    {
        public string Link { get; }
        public string Name { get; }
        public string? Key { get; }

        public ToolbarItem(string? link, string? name, string? key)
        {
            this.Link = link ?? "unknownLink";
            this.Name = name ?? "Unknown";
            this.Key = string.IsNullOrEmpty(key) ? null : key;
        }
    }
}
