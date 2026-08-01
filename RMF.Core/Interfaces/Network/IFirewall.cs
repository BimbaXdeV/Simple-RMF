using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IFirewall
    {
        bool TryLoadBlacklist();
        bool TrySaveBlacklist();
        bool IsBanned(string ipAddress);
        string[] GetBannedIPs(int? limit = null);
        void Ban(string? ipAddress);
    }
}
