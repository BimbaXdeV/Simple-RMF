using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal static class Colorist
    {
        private static readonly string ColorPref = "\u001b[38;2;{0};{1};{2}m";
        private static readonly string ResetSuf = "\u001b[0m";

        public static string ColoredFilterRGB(byte r, byte g, byte b)
        {
            if (r == byte.MaxValue && g == byte.MaxValue && b == byte.MaxValue)
            {
                // Due to the large number of standard color pins, there is no need to overload the console with color formats
                return string.Empty;
            }

            return string.Format(ColorPref, r, g, b);
        }

        public static string ColoredFilterRGB(ThemeColor color)
        {
            if (color.R == byte.MaxValue && color.G == byte.MaxValue && color.B == byte.MaxValue)
            {
                // Due to the large number of standard color pins, there is no need to overload the console with color formats
                return string.Empty;
            }

            return string.Format(ColorPref, color.R, color.G, color.B);
        }

        //public static string ColoredFilterGrayScale(byte w)
        //{
        //    return string.Format(ColorPref, w, w, w);
        //}

        //public static string ColoredFilterGrayScale(ThemeColor color)
        //{
        //    byte w = (byte)((color.R + color.G + color.B) / 3);
        //    return string.Format(ColorPref, w, w, w);
        //}

        public static string ResetColor()
        {
            return ResetSuf;
        }
    }
}
