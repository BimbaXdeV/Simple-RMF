using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public readonly record struct PacketHeader(short Id, int Length)
    {
        public const int Size = sizeof(short) + sizeof(int);
    }
}
