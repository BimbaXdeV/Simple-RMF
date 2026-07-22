using RMF.Core.Interfaces.Logic;
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
    internal class AppearanceManager : IWindowManager, IToolbarManager, IWindowEffects
    {
        // Inilialization things
        private const byte MaxTitleLength = 48;
        private const string ClientLogo = @"
'||''|.   '||    ||' '||''''|      ..|'''.| '||   ||                     .   
 ||   ||   |||  |||   ||  .      .|'     '   ||  ...    ....  .. ...   .||.  
 ||''|'    |'|..'||   ||''|      ||          ||   ||  .|...||  ||  ||   ||   
 ||   |.   | '|' ||   ||         '|.      .  ||   ||  ||       ||  ||   ||   
.||.  '|' .|. | .||. .||.         ''|....'  .||. .||.  '|...' .||. ||.  '|.' 
";
        private readonly int ClientLogoHeight = ClientLogo.Count(c => c == '\n') + 1;

        // Toolbar items will be loaded from "~\RMF-Client\toolbar.xml" file
        // <add key="" link="" name=""/>
        private readonly string ToolbarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "toolbar.xml");
        private string ToolbarTemplate = "Nothing to do...";
        private readonly Dictionary<string, string> ToolbarContent = [];

        private void InitializeToolbarTemplate(ToolbarItem[] items)
        {
            if (items.Length == 0)
            {
                Console.WriteLine($"Failed to load toolbar, file {this.ToolbarPath} has been corrupted");
                return;
            }

            int maxNameLength = items.Max(x => x.Name.Length);
            ToolbarTemplate = string.Join(Environment.NewLine, items.Select(x => $"[{x.Key ?? " "}] {string.Format($"{{0,-{maxNameLength}}}", x.Name)} : {{{x.Link}}}"));
        }

        private void InitializeToolbarContent(ToolbarItem[] items)
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

        public ToolbarItem[] GetToolbarItems()
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

        private string FillToolbarBody()
        {
            StringBuilder toolbarBody = new(ToolbarTemplate);
            foreach (var (key, value) in ToolbarContent)
            {
                toolbarBody.Replace("{" + key + "}", value);
            }
            return toolbarBody.ToString();
        }

        public void LoadToolbar()
        {
            ToolbarItem[] toolbarItems = GetToolbarItems();
            InitializeToolbarTemplate(toolbarItems);
            InitializeToolbarContent(toolbarItems);
        }

        public void ReplaceToolbarContent(Dictionary<string, string> content, bool autoUpdate = true)
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

        public void DisplayLogo()
        {
            Console.WriteLine(ClientLogo);
        }

        public void DisplayToolbar()
        {
            string toolbarBody = FillToolbarBody();
            string[] toolbarLines = toolbarBody.Split(Environment.NewLine);

            Console.SetCursorPosition(0, ClientLogoHeight);
            foreach (string l in toolbarLines)
            {
                Console.WriteLine(l.PadRight(Console.WindowWidth));
            }
        }

        public void SetTitle(string newTitle)
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

        public async Task Curtain(float delaySecs)
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
