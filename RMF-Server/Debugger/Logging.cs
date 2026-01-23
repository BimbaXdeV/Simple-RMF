using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal static class Logging
    {
        public static byte[] DatetimeColorRGB = { 193, 255, 128 };
        public static byte[] WarningColorRGB = { 255, 187, 51 };
        public static byte[] ErrorColorRGB = { 255, 94, 94 };
        public static string? DefaultLogEnding = "";

        public static char ConsoleSeparator = '-';
        public static int ConsoleSeparatorLength = 24;

        private static readonly int MaxMethodNameLength = GetMaxMethodNameLength();

        private static int GetMaxMethodNameLength()
        {
            var methodNames = typeof(Logging).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(void) &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(string))
                .Select(m => m.Name.Length);
            return methodNames.Any() ? methodNames.Max() : 10;
        }

        public static void Output(string message)
        {
            Console.WriteLine($"{Colorist.ColoredFilterRGB(DatetimeColorRGB[0], DatetimeColorRGB[1], DatetimeColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {Colorist.ResetColor()}{message}{DefaultLogEnding}");
        }

        public static void Warning(string message)
        {
            Console.WriteLine($"{Colorist.ColoredFilterRGB(WarningColorRGB[0], WarningColorRGB[1], WarningColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}");
        }

        public static void Error(string message)
        {
            Console.WriteLine($"{Colorist.ColoredFilterRGB(ErrorColorRGB[0], ErrorColorRGB[1], ErrorColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}");
        }

        public static void Separator()
        {
            Console.WriteLine(string.Join("", Enumerable.Repeat(ConsoleSeparator.ToString(), ConsoleSeparatorLength)));
        }

        public static void ClearConsole()
        {
            Console.WriteLine("\u001b[2J\u001b[H");
        }
    }
}
