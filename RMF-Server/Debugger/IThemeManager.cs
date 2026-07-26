using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal interface IThemeManager
    {
        ThemeColor GetColor(string colorKey);
    }
}
