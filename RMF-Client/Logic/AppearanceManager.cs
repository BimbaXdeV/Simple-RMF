using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal static class AppearanceManager
    {
        private static readonly int maxTitleLength = 48;

        public static void SetTitle(string newTitle)
        {
            if (string.IsNullOrEmpty(newTitle))
            {
                Console.WriteLine("Failed to update application title, received an empty string");
                return;
            }

            if (newTitle.Length > maxTitleLength)
            {
                Console.WriteLine($"Failed to update application title, received too long string (max length: {maxTitleLength})");
                return;
            }

            Console.Title = newTitle;
        }
    }
}
