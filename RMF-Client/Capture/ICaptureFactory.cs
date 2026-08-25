using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal interface ICaptureFactory
    {
        void CheckForUpdates();
        IScreenProvider? GetActualProvider(bool updateIfNullable = false);
    }
}
