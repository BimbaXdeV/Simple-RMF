using RMF_Client.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Client.Logic
{
    internal static class AppearanceManager
    {
        // Inilialization things
        private static readonly int MaxTitleLength = 48;
        private static readonly string ClientLogo = @"
 ███████████   ██████   ██████ ███████████      █████████  ████   ███                       █████   
░░███░░░░░███ ░░██████ ██████ ░░███░░░░░░█     ███░░░░░███░░███  ░░░                       ░░███    
 ░███    ░███  ░███░█████░███  ░███   █ ░     ███     ░░░  ░███  ████   ██████  ████████   ███████  
 ░██████████   ░███░░███ ░███  ░███████      ░███          ░███ ░░███  ███░░███░░███░░███ ░░░███░   
 ░███░░░░░███  ░███ ░░░  ░███  ░███░░░█      ░███          ░███  ░███ ░███████  ░███ ░███   ░███    
 ░███    ░███  ░███      ░███  ░███  ░       ░░███     ███ ░███  ░███ ░███░░░   ░███ ░███   ░███ ███
 █████   █████ █████     █████ █████          ░░█████████  █████ █████░░██████  ████ █████  ░░█████ 
░░░░░   ░░░░░ ░░░░░     ░░░░░ ░░░░░            ░░░░░░░░░  ░░░░░ ░░░░░  ░░░░░░  ░░░░ ░░░░░    ░░░░░  
";
        private static readonly int ClientLogoHeight = ClientLogo.Count(c => c == '\n') + 1;

        // Toolbar items will be loaded from "~\RMF-Client\toolbar.xml" file
        // <add key="" link="" name=""/>
        private static readonly string ToolbarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "toolbar.xml");
        private static string ToolbarBody = "Nothing to do...";

        public static void LoadToolbar()
        {
            if (!File.Exists(ToolbarPath))
            {
                Console.WriteLine($"Failed to load toolbar, file not found: {ToolbarPath}");
                return;
            }

            XDocument toolbarDoc = XDocument.Load(ToolbarPath);
            ToolbarItem[]? toolbarItems = toolbarDoc.Element("Toolbar")?
                .Elements("add")
                .Select(x => new ToolbarItem(x.Attribute("link")?.Value, x.Attribute("name")?.Value, x.Attribute("key")?.Value))
                .ToArray() ?? [];

            if (toolbarItems.Length == 0)
            {
                Console.WriteLine($"Failed to load toolbar, file {ToolbarPath} has been corrupted");
                return;
            }

            int maxNameLength = toolbarItems.Max(x => x.Name.Length);
            ToolbarBody = string.Join(Environment.NewLine, toolbarItems.Select(x => $"[{x.Key ?? " "}] {string.Format($"{{0,{maxNameLength}}}", x.Name)} : {{{x.Link}}}"));
        }
        
        public static void DisplayLogo()
        {
            Console.WriteLine(ClientLogo);
        }

        public static void DisplayToolbar(Dictionary<string, string> items)
        {
            if (items.Count == 0)
            {
                Console.SetCursorPosition(0, ClientLogoHeight);
                Console.WriteLine(ToolbarBody);
                return;
            }

            string toolbar = ToolbarBody;
            foreach (var (key, value) in items)
            {
                toolbar.Replace("{" + key + "}", value);
            }

            Console.SetCursorPosition(0, ClientLogoHeight);
            Console.WriteLine(toolbar);
        }

        public static void SetTitle(string newTitle)
        {
            if (string.IsNullOrEmpty(newTitle))
            {
                Console.WriteLine("Failed to update application title, received an empty string");
                return;
            }

            if (newTitle.Length > MaxTitleLength)
            {
                Console.WriteLine($"Failed to update application title, received too long string (max length: {MaxTitleLength})");
                return;
            }

            Console.Title = newTitle;
        }
    }
}
