using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Diagnostics;

namespace DtaSpy
{
    public class BizTalkFragmentBlockWriter
    {
        private Stream source;
        private static readonly byte[] zeroBuf = new byte[] { 0, 0, 0};

        public BizTalkFragmentBlockWriter(Stream output)
        {
            this.output = output;
        }

        public void WriteBlock(FragmentBlock block)
        {
            var bw = new BinaryWriter(this.output);

            bw.Write(block.Compressed);

            // I don't know what these bytes represent
            bw.Write(zeroBuf, 0, 3);

            // Content length (uncompressed length) 16bit little endian
            bw.Write((ushort)block.UncompressedLength);

            // I don't know what these bytes represent
            bw.Write(zeroBuf, 0, 2);

            // 16bit little endian
            bw.Write((ushort)block.Length);

            // I don't know what these bytes represent
            bw.Write(zeroBuf, 0, 2);

            if (block.Length > 0)
                this.output.Write(block.Content, 0, block.Length);
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

        public Stream output { get; set; }
    }
}
