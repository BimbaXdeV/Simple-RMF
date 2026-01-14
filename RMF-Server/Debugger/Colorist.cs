using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class Colorist
    {
        private static readonly string ColorPref = "\u001b[38;2;{0};{1};{2}m";
        private static readonly string ResetSuf = "\u001b[0m";

        public static string ColoredFilterRGB(byte R, byte G, byte B)
        {
            return String.Format(ColorPref, R, G, B);
        }

        public static string ColoredFilterGrayScale(byte W)
        {
            return String.Format(ColorPref, W, W, W);

        }

        public static string ResetColor()
        {
            return ResetSuf;
        }
    }
}
