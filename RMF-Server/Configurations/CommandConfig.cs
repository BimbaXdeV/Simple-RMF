using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class CommandConfig
    {
        public char InlineCommandDefautSign = char.MinValue;
        public bool InlineSuggestionsEnabled = false;
        public int InlineSuggestionsMinChars = 1;
    }
}
