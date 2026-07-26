using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public static class ReflectionPacketLoader
    {
        public static (Dictionary<short, Type> Data, int Total) Load(ILoggingEngine? logger = null)
        {
            Type basePacketType = typeof(Packet);

            Type[] foundPacketTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(basePacketType) && !t.IsAbstract)
                .ToArray();

            Dictionary<short, Type> packetTypes = [];
            foreach (Type packetType in foundPacketTypes)
            {
                short? packetId = (short?)(packetType.GetProperty("ID")?.GetValue(null));
                if (packetId == null)
                {
                    logger?.Warning($"Failed to load {packetType.Name}: the packet type must contains property \"ID\"");
                    continue;
                }

                packetTypes.TryAdd((short)packetId, packetType);
            }
            return (packetTypes, foundPacketTypes.Length);
        }
    }
}
