using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class Command
    {
        public required string Category { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public CommandParameter[]? Parameters { get; set; }
    }
}
