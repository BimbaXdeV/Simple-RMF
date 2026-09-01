using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Loaders;
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
        public static LoadResult<Dictionary<short, Type>> Load()
        {
            Type basePacketType = typeof(Packet);

            Type[] foundPacketTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(basePacketType) && !t.IsAbstract)
                .ToArray();

            Dictionary<short, Type> packetTypes = [];
            foreach (Type packetType in foundPacketTypes)
            {
                // It`s needed purely for the fun of it. Without it, reflection refuses to retrieve the packet ID.
                object packetInstance = Activator.CreateInstance(packetType)!;

                short? packetId = (short?)(packetType.GetProperty("ID")?.GetValue(packetInstance));
                if (packetId == null)
                {
                    return LoadResult<Dictionary<short, Type>>.Failure($"Packet type {packetType.Name} does not have a valid static ID property");
                }

                packetTypes.TryAdd((short)packetId, packetType);
            }
            return LoadResult<Dictionary<short, Type>>.Success(packetTypes, packetTypes.Count, foundPacketTypes.Length);
        }
    }
}
