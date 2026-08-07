using Avalonia.Data;
using SkiaSharp;
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

        public static string GetColoredFilterRGB(ThemeColor color)
        {
            if (color.R == byte.MaxValue && color.G == byte.MaxValue && color.B == byte.MaxValue)
            {
                // Due to the large number of standard color pins, there is no need to overload the console with color formats
                return string.Empty;
            }

            return string.Format(ColorPref, color.R, color.G, color.B);
        }

        //public static string ApplyGradientRGB(string message, ThemeColor start, ThemeColor end, GradientDirection direction = default)
        //{
        //    if ((start.R == byte.MaxValue && start.G == byte.MaxValue && start.B == byte.MaxValue &&
        //        end.R == byte.MaxValue && end.G == byte.MaxValue && end.B == byte.MaxValue) ||
        //        (start.R == end.R && start.G == end.G && start.B == end.B))
        //    {
        //        return message;
        //    }

        //    string result = string.Empty;
        //    switch (direction)
        //    {
        //        case GradientDirection.Horizontal:
        //            break;

        //        case GradientDirection.Vertical:
        //            string[] lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        //            for (int i = 0; i < lines.Length; i++)
        //            {
        //                float ratio = (float)i / (lines.Length - 1);
        //                byte r = (byte)(start.R + (end.R - start.R) * ratio);
        //                byte g = (byte)(start.G + (end.G - start.G) * ratio);
        //                byte b = (byte)(start.B + (end.B - start.B) * ratio);

        //                string colorPref = GetColoredFilterRGB(new ThemeColor(r, g, b, 255));
        //                Console.WriteLine(colorPref + lines[i] + ResetColor());
        //            }
        //            break;
        //    }
        //    return !string.IsNullOrEmpty(result) ? result : message;
        //}

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
