using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public static class NetworkBuffer
    {
        [ThreadStatic]
        private static MemoryStream? CacheStream;

        [ThreadStatic]
        private static BinaryReader? CacheReader;

        [ThreadStatic]
        private static BinaryWriter? CacheWriter;

        public static MemoryStream GetMemoryStream()
        {
            CacheStream ??= new MemoryStream();
            CacheStream.SetLength(0);
            CacheStream.Position = 0;
            return CacheStream;
        }

        public static BinaryReader GetBinaryReader()
        {
            CacheReader ??= new BinaryReader(CacheStream ?? GetMemoryStream());
            return CacheReader;
        }

        public static BinaryWriter GetBinaryWriter()
        {
            CacheWriter ??= new BinaryWriter(CacheStream ?? GetMemoryStream());
            return CacheWriter;
        }
    }
}
