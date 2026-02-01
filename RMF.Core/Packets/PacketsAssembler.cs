using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public class PacketsAssembler
    {
        private static readonly Dictionary<short, Type> PacketTypes = [];

        // Automatically register packet types from the project
        public PacketsAssembler()
        {
            Type basePacketType = typeof(Packet);

            Type[] foundPacketTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(basePacketType) && !t.IsAbstract)
                .ToArray();

            foreach (Type packetType in foundPacketTypes)
            {
                Packet? packetInstance = (Packet?)Activator.CreateInstance(packetType);
                if (packetInstance != null)
                {
                    PacketTypes[packetInstance.ID] = packetType;
                }
            }
        }

        public static Packet? GetPacket(short id)
        {
            return PacketTypes.TryGetValue(id, out Type? packetType) ? (Packet?)Activator.CreateInstance(packetType) : null;
        }
    }
}
