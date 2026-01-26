using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class Command
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public CommandParameter[]? Parameters { get; set; }
    }
}
