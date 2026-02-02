using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public static class NetworkBuffer
    {
        private static readonly AsyncLocal<MemoryStream> CacheStream = new();
        private static readonly AsyncLocal<BinaryReader> CacheReader = new();
        private static readonly AsyncLocal<BinaryWriter> CacheWriter = new();

        public static MemoryStream GetMemoryStream()
        {
            MemoryStream stream = CacheStream.Value ??= new MemoryStream();
            stream.SetLength(0);
            stream.Position = 0;
            return stream;
        }

        public static BinaryReader GetBinaryReader()
        {
            return CacheReader.Value ??= new BinaryReader(GetMemoryStream());
        }

        public static BinaryReader GetBinaryReader(NetworkStream stream)
        {
            return new BinaryReader(stream, Encoding.UTF8);
        }

        public static BinaryWriter GetBinaryWriter()
        {
            return CacheWriter.Value ??= new BinaryWriter(GetMemoryStream());
        }

        public static BinaryWriter GetBinaryWriter(NetworkStream stream)
        {
            return new BinaryWriter(stream, Encoding.UTF8);
        }
    }
}
