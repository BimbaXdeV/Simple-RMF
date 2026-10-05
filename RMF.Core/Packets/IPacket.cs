using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public interface IPacket
    {
        short ID { get; }
        void Deserialize(ref SpanReader reader);
        void WriteToStream(BinaryWriter writer);
    }
}
