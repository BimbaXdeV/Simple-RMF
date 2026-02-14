using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF_Server.Debugger;
using RMF_Server.Exceptions;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets
{
    internal static class PacketsHandler
    {
        public static async Task<byte[]> ReadPayload(string endPoint, NetworkStream stream, int size)
        {
            Console.WriteLine(size);
            long bytesLimit = ConfigurationManager.MaxPacketLengthKB * 1024;
            if (size > bytesLimit || size < 0)
            {
                throw new PayloadBufferOverflow("The payload size exceeds the allowed buffer limit");
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            int totalBytesRead = 0;
            while (totalBytesRead < size)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, size - totalBytesRead);
                if (bytesRead == 0)
                {
                    throw new Exception("Connection closed unexpectedly");
                }
                totalBytesRead += bytesRead;
            }
            return buffer;
        }

        // Manual method, but lightning fast to execute
        public static void SwitchHandle(Packet packet, string endPoint)
        {
            switch (packet)
            {
                case SystemInfoPacket systemInfoPacket:
                    ProcessSystemInfoPacket(systemInfoPacket, endPoint);
                    break;

                case RemoteDesktopPacket remoteDesktopPacket:
                    _ = Task.Run(() => ProcessRemoteDesktopPacket(remoteDesktopPacket, endPoint));
                    break;
            }
        }

        // This handle method is too slow for streaming production, but it's here if you need it for scaling purposes
        public static void SearchHandle(Packet packet, string endPoint)
        {
            //Type packetType = packet.GetType();
            //var method = typeof(PacketsHandler).GetMethod("Process" + packetType.Name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            //if (method != null)
            //{
            //    method.Invoke(null, new object[] { packet, endPoint });
            //}
        }

        private static void ProcessSystemInfoPacket(SystemInfoPacket packet, string endPoint)
        {
            Console.WriteLine($"Info about {endPoint} - OS: {packet.OS}, CPU: {packet.CPU}, GPU: {packet.GPU}, Username: {packet.Username}");
        }

        private static async Task ProcessRemoteDesktopPacket(RemoteDesktopPacket packet, string endPoint)
        {
            if (SessionManager.Connections.TryGetValue(endPoint, out ClientSession? session))
            {
                session.LastFrame = packet.ImageData;
                session.LastUpdate = DateTime.Now;
            }
        }
    }
}
