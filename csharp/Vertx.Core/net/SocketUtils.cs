using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IO.Vertx.Core.net
{
    public class SocketUtils
    {

        public static int ReadInt(BinaryReader reader)
        {
            byte[] fourBytes = new byte[4];
            fourBytes[0] = reader.ReadByte();
            fourBytes[1] = reader.ReadByte();
            fourBytes[2] = reader.ReadByte();
            fourBytes[3] = reader.ReadByte();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fourBytes);
            }
            return BitConverter.ToInt32(fourBytes, 0);
        }

        public static void WriteInt(Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes((UInt32)value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }

    }
}
