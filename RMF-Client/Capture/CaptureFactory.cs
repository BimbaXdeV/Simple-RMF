using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal static class CaptureFactory
    {
        private static IScreenProvider? Provider;

        public static void CheckForUpdates()
        {
            // The denser the forest... If else, if else :D
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Provider?.GetType() != typeof(DXGICapturer))
            {
                Provider = new DXGICapturer();
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Provider?.GetType() != typeof(X11Capturer))
            {
                Provider = new X11Capturer();
                return;
            }
        }

        public static IScreenProvider? GetActualProvider(bool UpdateIfNullable = false)
        {
            if (UpdateIfNullable && Provider == null)
            {
                CheckForUpdates();  // If you are writing a looping periodic checker, you do not need this call
            }
            return Provider;
        }
    }
}
