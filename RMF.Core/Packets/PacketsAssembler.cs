using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public static class PacketsAssembler
    {
        private static readonly Dictionary<short, Type> PacketTypes = [];

        // Automatically register packet types from the project
        public static int RegisterFound()
        {
            Type basePacketType = typeof(Packet);

            Type[] foundPacketTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(basePacketType) && !t.IsAbstract)
                .ToArray();

            int registeredPacketsCount = foundPacketTypes.Length;
            foreach (Type packetType in foundPacketTypes)
            {
                Packet? packetInstance = (Packet?)Activator.CreateInstance(packetType);
                if (packetInstance != null)
                {
                    PacketTypes[packetInstance.ID] = packetType;
                    registeredPacketsCount++;
                }
            }
            return registeredPacketsCount;
        }

        public static Packet? GetPacket(short id)
        {
            return PacketTypes.TryGetValue(id, out Type? packetType) ? (Packet?)Activator.CreateInstance(packetType) : null;
        }
    }
}
