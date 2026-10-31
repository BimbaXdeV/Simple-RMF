using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public interface IConfig
    {
        int LoadedFields { get; }

        bool ValidateFields(out string? exceptionMessage);
        void LoadFields(IReadOnlyDictionary<string, string> configDict);
    }
}
