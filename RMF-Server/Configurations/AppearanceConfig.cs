using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class AppearanceConfig : RmfConfig
    {
        private const short _minWindowWidth = 100;
        private const short _minWindowHeight = 100;

        public string AppTitle = "Server";
        public string WindowTitle = "Streamer";
        public short WindowWidth = 800;
        public short WindowHeight = 450;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (string.IsNullOrWhiteSpace(AppTitle))
            {
                exceptionMessage = "AppTitle cannot be null or empty";
                return false;
            }
            if (string.IsNullOrWhiteSpace(WindowTitle))
            {
                exceptionMessage = "WindowTitle cannot be null or empty";
                return false;
            }
            if (WindowWidth <= _minWindowWidth)
            {
                exceptionMessage = $"WindowWidth must be greater than or equal to {_minWindowWidth} px";
                return false;
            }
            if (WindowHeight <= _minWindowHeight)
            {
                exceptionMessage = $"WindowHeight must be greater than or equal to {_minWindowHeight} px";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<string>(configDict, nameof(AppTitle), ref loadedFields, v => AppTitle = v);
            ApplyValue<string>(configDict, nameof(WindowTitle), ref loadedFields, v => WindowTitle = v);
            ApplyValue<short>(configDict, nameof(WindowWidth), ref loadedFields, v => WindowWidth = v);
            ApplyValue<short>(configDict, nameof(WindowHeight), ref loadedFields, v => WindowHeight = v);
            return loadedFields;
        }
    }
}
