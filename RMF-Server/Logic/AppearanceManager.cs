using Avalonia.Platform;
using Avalonia.Threading;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF_Server.Debugger;
using RMF_Server.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class AppearanceManager : IWindowManager
    {
        private readonly ILoggingEngine? _logger;

        private const byte MaxTitleLength = 48;

        public AppearanceManager(ILoggingEngine? logger = null)
        {
            this._logger = logger;
        }

        public void SetTitle(string newTitle)
        {
            if (string.IsNullOrEmpty(newTitle))
            {
                this._logger?.Warning("Failed to update application title, received an empty string");
                return;
            }

            if (newTitle.Length > MaxTitleLength)
            {
                this._logger?.Warning($"Failed to update application title, received too long string (max length: {MaxTitleLength})");
                return;
            }

            Console.Title = newTitle;
        }
    }
}
