using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal class CaptureFactory : ICaptureFactory
    {
        private IScreenProvider? _provider;

        public void CheckForUpdates()
        {
            // The denser the forest... If else, if else :D
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && this._provider?.GetType() != typeof(DXGICapturer))
            {
                this._provider = new DXGICapturer();
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && this._provider?.GetType() != typeof(X11Capturer))
            {
                this._provider = new X11Capturer();
                return;
            }
        }

        public IScreenProvider? GetActualProvider(bool updateIfNullable = false)
        {
            if (updateIfNullable && this._provider == null)
            {
                CheckForUpdates();  // If you are writing a looping periodic checker, you do not need this call
            }
            return this._provider;
        }
    }
}
