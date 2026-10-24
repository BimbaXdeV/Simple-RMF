using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class CommandConfig : RmfConfig
    {
        public char InlineCommandDefautSign = char.MinValue;
        public bool InlineSuggestionsEnabled = false;
        public int InlineSuggestionsMinChars = 1;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (InlineSuggestionsMinChars < 1)
            {
                exceptionMessage = "InlineSuggestionsMinChars must be greater than or equal to 1";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<char>(configDict, nameof(InlineCommandDefautSign), ref loadedFields, v => InlineCommandDefautSign = v);
            ApplyValue<bool>(configDict, nameof(InlineSuggestionsEnabled), ref loadedFields, v => InlineSuggestionsEnabled = v);
            ApplyValue<int>(configDict, nameof(InlineSuggestionsMinChars), ref loadedFields, v => InlineSuggestionsMinChars = v);
            return loadedFields;
        }
    }
}
