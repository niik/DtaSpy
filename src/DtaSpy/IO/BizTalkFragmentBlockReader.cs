using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Diagnostics;

namespace DtaSpy
{
    public class BizTalkFragmentBlockReader
    {
        private Stream source;

        public BizTalkFragmentBlockReader(Stream source)
        {
            this.source = source;
        }

        public BizTalkFragmentBlockReader(byte[] buffer)
            : this(buffer, 0, buffer.Length)
        {
        }

        public BizTalkFragmentBlockReader(byte[] buffer, int offset, int count)
        {
            this.source = new MemoryStream(buffer, offset, count, false);
        }

        public FragmentBlock ReadBlock()
        {
            bool compressed = this.ReadHeaderBoolean();

            // I don't know what these bytes represent
            this.ReadAssumedEmptyHeaderByte();
            this.ReadAssumedEmptyHeaderByte();
            this.ReadAssumedEmptyHeaderByte();

            // 16bit little endian
            int uncompressedLength = this.ReadUInt16LE();

            // I don't know what these bytes represent
            this.ReadAssumedEmptyHeaderByte();
            this.ReadAssumedEmptyHeaderByte();

            // 16bit little endian
            int length = this.ReadUInt16LE();

            // I don't know what these bytes represent
            this.ReadAssumedEmptyHeaderByte();
            this.ReadAssumedEmptyHeaderByte();

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

        private ushort ReadUInt16LE()
        {
            return (ushort)(this.source.ReadByte() + (this.source.ReadByte() << 8));
        }

        private bool ReadHeaderBoolean()
        {
            return ReadHeaderByte(0, 1) == 1;
        }

        private byte ReadHeaderByte(params byte[] validValues)
        {
            byte b = ReadHeaderByte();

            if (validValues != null && Array.IndexOf(validValues, b) == -1)
                throw new FormatException("Malformed block header");

            return (byte)b;
        }

        private byte ReadHeaderByte()
        {
            int b = this.source.ReadByte();

            if (b == -1)
                throw new EndOfStreamException();

            return (byte)b;
        }

        private void ReadAssumedEmptyHeaderByte()
        {
            byte b = ReadHeaderByte();
            Debug.Assert(b == 0, "This byte is not yet mapped by DtaSpy and is expected to be zero. Please get in touch or build in release mode to disable this assert.");
        }
    }
}
