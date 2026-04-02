using RMF_Client.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private static string ToolbarTemplate = "Nothing to do...";
        private static readonly Dictionary<string, string> ToolbarContent = [];

        private static void InitializeToolbarTemplate(ToolbarItem[] items)
        {
            if (items.Length == 0)
            {
                Console.WriteLine($"Failed to load toolbar, file {ToolbarPath} has been corrupted");
                return;
            }

            int maxNameLength = items.Max(x => x.Name.Length);
            ToolbarTemplate = string.Join(Environment.NewLine, items.Select(x => $"[{x.Key ?? " "}] {string.Format($"{{0,-{maxNameLength}}}", x.Name)} : {{{x.Link}}}"));
        }

        private static void InitializeToolbarContent(ToolbarItem[] items)
        {
            if (ToolbarContent.Count > 0)
            {
                ToolbarContent.Clear();
            }

            foreach (ToolbarItem i in items)
            {
                ToolbarContent[i.Link] = i.DefaultValue;
            }
        }

        public static ToolbarItem[] GetToolbarItems()
        {
            if (!File.Exists(ToolbarPath))
            {
                Console.WriteLine($"Failed to load toolbar, file not found: {ToolbarPath}");
                return [];
            }

            XDocument toolbarDoc = XDocument.Load(ToolbarPath);
            ToolbarItem[]? toolbarItems = toolbarDoc.Element("Toolbar")?
                .Elements("add")
                .Select(x => new ToolbarItem(x.Attribute("link")?.Value, x.Attribute("name")?.Value, x.Attribute("key")?.Value, x.Attribute("default")?.Value))
                .ToArray() ?? [];
            return toolbarItems;
        }

        private static string FillToolbarBody()
        {
            StringBuilder toolbarBody = new(ToolbarTemplate);
            foreach (var (key, value) in ToolbarContent)
            {
                toolbarBody.Replace("{" + key + "}", value);
            }
            return toolbarBody.ToString();
        }

        public static void LoadToolbar()
        {
            ToolbarItem[] toolbarItems = GetToolbarItems();
            InitializeToolbarTemplate(toolbarItems);
            InitializeToolbarContent(toolbarItems);
        }

        public static void ReplaceToolbarContent(Dictionary<string, string> content, bool autoUpdate = true)
        {
            bool isReplaced = false;
            foreach (var (key, value) in content)
            {
                if (ToolbarContent.ContainsKey(key))
                {
                    ToolbarContent[key] = value;
                    isReplaced |= true;
                }
            }
            if (autoUpdate && isReplaced)
            {
                DisplayToolbar();
            }
        }

        public static void DisplayLogo()
        {
            Console.WriteLine(ClientLogo);
        }

        public static void DisplayToolbar()
        {
            string toolbarBody = FillToolbarBody();
            string[] toolbarLines = toolbarBody.Split(Environment.NewLine);

            Console.SetCursorPosition(0, ClientLogoHeight);
            foreach (string l in toolbarLines)
            {
                Console.WriteLine(l.PadRight(Console.WindowWidth - 1));
            }
        }

        public static void SetTitle(string newTitle)
        {
            if (string.IsNullOrEmpty(newTitle))
            {
                return;
            }

            if (newTitle.Length > MaxTitleLength)
            {
                return;
            }

            Console.Title = newTitle;
        }

        public static async Task Curtain(float delaySecs)
        {
            for (int i = Console.GetCursorPosition().Top; i >= 0; i--)
            {
                await Task.Delay((int)(delaySecs * 1000));
                Console.SetCursorPosition(0, i);
                Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            }
        }
    }
}
