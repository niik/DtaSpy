using System;
using System.IO;

namespace DtaSpy
{
    public class BizTalkBlockReader
    {
        private Stream source;
        private BinaryReader reader;

        public BizTalkBlockReader(Stream source)
        {
            this.source = source;
            this.reader = new BinaryReader(source);
        }

        public BizTalkBlockReader(byte[] buffer)
            : this(buffer, 0, buffer.Length)
        {
        }

        public BizTalkBlockReader(byte[] buffer, int offset, int count)
        {
            this.source = new MemoryStream(buffer, offset, count, false);
            this.reader = new BinaryReader(this.source);
        }

        public FragmentBlock ReadBlock()
        {
            bool compressed = this.ReadHeaderBoolean();
            int uncompressedLength = this.reader.ReadInt32();
            int length = this.reader.ReadInt32();

            byte[] blockBuffer = new byte[length];

            if (length > 0)
            {
                int read;
                int offset = 0;

                do
                {
                    read = this.source.Read(blockBuffer, offset, blockBuffer.Length - offset);
                    offset += read;
                }
                while (read > 0);
            }

            return new FragmentBlock(compressed, length, uncompressedLength, blockBuffer);
        }

        private bool ReadHeaderBoolean()
        {
            int v = this.reader.ReadInt32();

            if (v == 0)
                return false;
            else if (v == 1)
                return true;

            throw new FormatException("Malformed block header");
        }
    }
}
