using RMF.Core.Appearance;
using RMF_Client.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Client.Appearance
{
    internal class AppearanceManager : IWindowManager, IToolbarManager, IWindowEffects
    {
        private readonly AppearanceConfig _appearanceConfig;

        // Inilialization things
        private const byte _maxTitleLength = 48;
        private const string _clientLogo = @"
'||''|.   '||    ||' '||''''|      ..|'''.| '||   ||                     .   
 ||   ||   |||  |||   ||  .      .|'     '   ||  ...    ....  .. ...   .||.  
 ||''|'    |'|..'||   ||''|      ||          ||   ||  .|...||  ||  ||   ||   
 ||   |.   | '|' ||   ||         '|.      .  ||   ||  ||       ||  ||   ||   
.||.  '|' .|. | .||. .||.         ''|....'  .||. .||.  '|...' .||. ||.  '|.' 
";
        private readonly int _clientLogoHeight = _clientLogo.Count(c => c == '\n') + 1;

        // Toolbar items will be loaded from "~\RMF-Client\toolbar.xml" file
        // <add key="" link="" name=""/>
        private readonly string _toolbarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "toolbar.xml");
        private string _toolbarTemplate = "Nothing to do...";
        private readonly Dictionary<string, string> _toolbarContent = [];

        public AppearanceManager(AppearanceConfig appearanceConfig)
        {
            this._appearanceConfig = appearanceConfig;
        }

        private void InitializeToolbarTemplate(ToolbarItem[] items)
        {
            if (items.Length == 0)
            {
                Console.WriteLine($"Failed to load toolbar, file {_toolbarPath} has been corrupted");
                return;
            }

            int maxNameLength = items.Max(x => x.Name.Length);
            this._toolbarTemplate = string.Join(Environment.NewLine, items.Select(x => $"[{x.Key ?? " "}] {string.Format($"{{0,-{maxNameLength}}}", x.Name)} : {{{x.Link}}}"));
        }

        private void InitializeToolbarContent(ToolbarItem[] items)
        {
            if (this._toolbarContent.Count > 0)
            {
                this._toolbarContent.Clear();
            }

            foreach (ToolbarItem i in items)
            {
                this._toolbarContent[i.Link] = i.DefaultValue;
            }
        }

        private string FillToolbarBody()
        {
            StringBuilder toolbarBody = new(this._toolbarTemplate);
            foreach (var (key, value) in this._toolbarContent)
            {
                toolbarBody.Replace("{" + key + "}", value);
            }
            return toolbarBody.ToString();
        }

        public void LoadToolbar(ToolbarItem[] toolbarItems)
        {
            InitializeToolbarTemplate(toolbarItems);
            InitializeToolbarContent(toolbarItems);
        }

        public void ReplaceToolbarContent(Dictionary<string, string> content, bool autoUpdate = true)
        {
            bool isReplaced = false;
            foreach (var (key, value) in content)
            {
                if (this._toolbarContent.ContainsKey(key))
                {
                    this._toolbarContent[key] = value;
                    isReplaced |= true;
                }
            }
            if (autoUpdate && isReplaced)
            {
                DisplayToolbar();
            }
        }

        public void DisplayToolbar()
        {
            string toolbarBody = FillToolbarBody();
            string[] toolbarLines = toolbarBody.Split(Environment.NewLine);

            Console.SetCursorPosition(0, this._clientLogoHeight);
            foreach (string l in toolbarLines)
            {
                Console.WriteLine(l.PadRight(Console.WindowWidth));
            }
        }

        public void UpdateTitleStatus(string newStatus)
        {
            int titleHeaderLength = this._appearanceConfig.AppTitle.Length + 11;  // "<Title> | Online: "
            if (newStatus.Length <= 0 || titleHeaderLength + newStatus.Length > _maxTitleLength)
            {
                Console.Title = this._appearanceConfig.AppTitle;
                return;
            }

            Console.Title = this._appearanceConfig.AppTitle + " | " + newStatus;
        }

        public void DisplayLogo()
        {
            Console.WriteLine(_clientLogo);
        }

        public async Task Curtain()
        {
            for (int i = Console.GetCursorPosition().Top; i >= 0; i--)
            {
                await Task.Delay(this._appearanceConfig.CurtainStepDelayMsecs);
                Console.SetCursorPosition(0, i);
                Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            }
        }
    }
}
