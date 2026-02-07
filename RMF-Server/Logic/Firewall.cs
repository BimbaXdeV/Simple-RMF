using RMF_Server.Debugger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class Firewall
    {
        private static ConcurrentDictionary<string, byte> bannedIPs = new();

        public static bool IsBanned(string ipAddress)
        {
            return bannedIPs.ContainsKey(ipAddress);
        }

        public static string[] GetBannedIPs(int? limit = null)
        {
            ICollection<string> keys = bannedIPs.Keys;
            return limit == null ? keys.ToArray() : keys.Take(limit.Value).ToArray();
        }

        public static void Ban(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                bannedIPs.TryAdd(ipAddress, 0);
                Logging.Warning($"The suspicious IP \"{ipAddress}\" has been banned");
            }
        }
    }
}
