using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public interface IPacketFactory
    {
        Type[] GetRegisteredTypes();
        short[] GetClientPacketIDs();
        Packet? CreatePacket(short id);
    }
}
