using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public enum PartingStatusCodes : byte
    {
        Success = 0,
        Failed = 1,
        Restart = 2
    }
}
