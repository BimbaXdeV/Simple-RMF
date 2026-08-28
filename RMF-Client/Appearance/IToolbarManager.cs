using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Appearance
{
    internal interface IToolbarManager
    {
        void ReplaceToolbarContent(Dictionary<string, string> content, bool autoUpdate = true);
        void DisplayToolbar();
    }
}
