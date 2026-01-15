using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets
{
    internal abstract class Packet
    {
        public abstract short ID { get; }
        protected byte[]? Payload { get; set; }

        // Needs to override this method in derived classes
        protected abstract byte[] SerializePayload();
        public abstract void Deserialize(BinaryReader reader);

        public byte[] ToByteStream()
        {
            byte[] data = SerializePayload();
            short streamLength = (short)data.Length;

            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(this.ID);      // 2 bytes
            writer.Write(streamLength); // 2 bytes
            writer.Write(data);         // n*4 bytes
            return ms.ToArray();
        }
    }
}
