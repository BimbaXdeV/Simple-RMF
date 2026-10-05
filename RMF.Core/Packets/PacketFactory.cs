using RMF.Core.Packets.Client;
using RMF.Core.Packets.Server;
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
        private readonly Dictionary<short, (Func<IPacket>, PacketOrigin)> _factories;
        private short[]? _cachedClientPacketIds;

        public PacketFactory(IEnumerable<IPacket> packets)
        {
            _factories = new Dictionary<short, (Func<IPacket>, PacketOrigin)>()
            {
                // [!] Client-side
                // 1. General information packets
                { 100, (() => new HeartbeatPacket(), PacketOrigin.Client) },
                { 101, (() => new ClientInfoPacket(), PacketOrigin.Client) },
                { 102, (() => new ClientVersionPacket(), PacketOrigin.Client) },
                { 103, (() => new PartingPacket(), PacketOrigin.Client) },
                { 104, (() => new EndOfStreamingPacket(), PacketOrigin.Client) },

                // 2. Streaming packets (hot paths)
                { 200, (() => new DesktopFramePacket(), PacketOrigin.Client) },
                { 201, (() => new StreamFramePacket(), PacketOrigin.Client) },

                // [!] Server-side
                // X. Uncategorized
                { 300, (() => new HandshakePacket(), PacketOrigin.Server) },
                { 301, (() => new ClientPingRequest(), PacketOrigin.Server) },
                { 302, (() => new ClientInfoRequest(), PacketOrigin.Server) },
                { 303, (() => new ClientVersionRequest(), PacketOrigin.Server) },
                { 304, (() => new EndOfEventsRequest(), PacketOrigin.Server) },
                { 305, (() => new ScreenshotRequest(), PacketOrigin.Server) },
                { 306, (() => new StreamingRequest(), PacketOrigin.Server) }
            };
        }

        public short[] GetClientPacketIDs()
        {
            _cachedClientPacketIds ??= _factories.Where(f => f.Value.Item2 == PacketOrigin.Client)
                                                 .Select(f => f.Key)
                                                 .ToArray();
            return _cachedClientPacketIds;
        }

        public IPacket? CreatePacket(short id)
        {
            return _factories.TryGetValue(id, out (Func<IPacket>, PacketOrigin) factory)
                ? (IPacket?)factory.Item1()
                : null;
        }
    }
}
