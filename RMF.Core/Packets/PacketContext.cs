using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public readonly record struct PacketContext(
        IPEndPoint EndPoint,
        short Id,
        int Length,
        byte[] Payload
    );
}
