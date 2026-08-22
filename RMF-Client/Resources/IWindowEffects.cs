using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Storage
{
    internal interface IWindowEffects
    {
        void DisplayLogo();
        Task Curtain(float delaySecs);
    }
}
