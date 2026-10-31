using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class RmfMeta
    {
        public static string Author => "BimbaXdeV";
        public static Version? AppVersion => Assembly.GetEntryAssembly()?.GetName().Version;
        public static Version? CoreVersion => typeof(IPacket).Assembly.GetName().Version;
    }
}
