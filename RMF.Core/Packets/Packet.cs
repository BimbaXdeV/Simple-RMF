using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets
{
    public abstract class Packet
    {
        public abstract short ID { get; }

        // Needs to override this method in derived classes
        public abstract void Deserialize(BinaryReader reader);
        protected abstract void WriteBody(BinaryWriter writer);

        public void WriteToStream(BinaryWriter writer)
        {
            long headerPosition = writer.BaseStream.Position;
            writer.Write(this.ID);  // Short ID: 2 bytes
            writer.Write(0);        // Int Length: 4 bytes

            long payloadStartPosition = writer.BaseStream.Position;
            WriteBody(writer);
            long payloadEndPosition = writer.BaseStream.Position;

            int payloadLength = (int)(payloadEndPosition - payloadStartPosition);
            writer.BaseStream.Position = headerPosition + 2;  // Move back to empty length position and replace it
            writer.Write(payloadLength);
            writer.BaseStream.Position = payloadEndPosition;
        }
    }
}
