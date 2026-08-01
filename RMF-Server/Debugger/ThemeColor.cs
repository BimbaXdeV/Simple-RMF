using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal readonly record struct ThemeColor(
        byte R,
        byte G,
        byte B,
        byte A
    );
}
