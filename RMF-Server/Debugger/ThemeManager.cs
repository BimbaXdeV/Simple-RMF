using Avalonia.Logging;
using Avalonia.Utilities;
using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Debugger
{
    internal class ThemeManager : IThemeManager
    {
        private readonly ThemeColor _defaultColor;
        private readonly Dictionary<string, ThemeColor> _colors;

        public ThemeManager(Dictionary<string, ThemeColor> theme, ThemeColor? defaultColor = null)
        {
            this._defaultColor = defaultColor ?? new ThemeColor(255, 255, 255);
            this._colors = theme;
        }

        public ThemeColor GetColor(string colorKey)
        {
            if (this._colors.TryGetValue(colorKey, out var color))
            {
                return color;
            }
            return this._defaultColor;
        }
    }
}
