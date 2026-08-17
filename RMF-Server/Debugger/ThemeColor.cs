using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal readonly record struct ThemeColor(byte R, byte G, byte B)
    {
        private const string AnsiColorFormat = "\u001b[38;2;{0};{1};{2}m";
        public const string AnsiReset = "\u001b[0m";

        public override string ToString()
        {
            if (this.R == byte.MaxValue && this.G == byte.MaxValue && this.B == byte.MaxValue)
            {
                // Due to the large number of standard color pins, there is no need to overload the console with color formats
                return string.Empty;
            }

            return string.Format(AnsiColorFormat, this.R, this.G, this.B);
        }
    }
}
