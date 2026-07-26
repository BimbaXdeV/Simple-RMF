using RMF.Core.Packets.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public class PacketFactory : IPacketFactory
    {
        private readonly Dictionary<short, Type> _packetTypes;
        private readonly short[] _clientPacketIds;

        public PacketFactory(Dictionary<short, Type> packetTypes)
        {
            this._packetTypes = packetTypes;

            this._clientPacketIds = _packetTypes.Where(item => item.Value.Namespace == typeof(HeartbeatPacket).Namespace!)
                                                .Select(item => item.Key)
                                                .ToArray();
        }

        public short[] GetClientPacketsIDs()
        {
            return this._clientPacketIds;
        }

        public Packet? CreatePacket(short id)
        {
            return _packetTypes.TryGetValue(id, out Type? packetType) ? (Packet?)Activator.CreateInstance(packetType) : null;
        }
    }
}
