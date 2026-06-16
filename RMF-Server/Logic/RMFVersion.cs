using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class RMFVersion
    {
        public static Version? App => Assembly.GetEntryAssembly()?.GetName().Version;
        public static Version? Core => typeof(Packet).Assembly.GetName().Version;
    }
}
