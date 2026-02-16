using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class AppearanceManager
    {
        private static readonly int MaxTitleLength = 48;

        public static void SetTitle(string newTitle)
        {
            if (string.IsNullOrEmpty(newTitle))
            {
                Logging.Warning("Failed to update application title, received an empty string");
                return;
            }

            if (newTitle.Length > MaxTitleLength)
            {
                Logging.Warning($"Failed to update application title, received too long string (max length: {MaxTitleLength})");
                return;
            }

            Console.Title = newTitle;
        }
    }
}
