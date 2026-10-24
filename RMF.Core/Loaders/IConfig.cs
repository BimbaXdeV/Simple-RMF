using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public interface IConfig
    {
        bool ValidateFields(out string? exceptionMessage);
        int LoadFields(Dictionary<string, object> configDict);
    }
}
