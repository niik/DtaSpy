using System;
using System.IO;
using System.IO.Compression;

namespace DtaSpy
{
    /// <summary>
    /// Utility method for converting from BizTalk block streams.
    /// </summary>
    public static class BizTalkCompressionConvert
    {
        public static byte[] Decompress(byte[] buffer)
        {
            using (var source = new MemoryStream(buffer))
            using (var decoder = new BizTalkFragmentStream(source, CompressionMode.Decompress))
            using (var reader = new BinaryReader(decoder))
            {
                return reader.ReadBytes(buffer.Length * 2);
            }
        }

        public static byte[] Decompress(Stream source)
        {
            using (var decoder = new BizTalkFragmentStream(source, CompressionMode.Decompress))
            using (var reader = new BinaryReader(decoder))
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int read;

                do
                {
                    read = decoder.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, read);
                }
                while (read > 0);

                return ms.ToArray();
            }
        }

        public static int DecompressTo(Stream source, Stream destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (destination == null)
                throw new ArgumentNullException("destination");

            using (var decoder = new BizTalkFragmentStream(source, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[8192];

                int total = 0;
                int read;

                do
                {
                    read = decoder.Read(buffer, 0, buffer.Length);
                    destination.Write(buffer, 0, read);
                    total += read;
                }
                while (read > 0);

                return total;
            }
        }

        public static int DecompressTo(string sourcePath, string destinationPath)
        {
            return DecompressTo(sourcePath, destinationPath, false);
        }

        public static int DecompressTo(string sourcePath, string destinationPath, bool append)
        {
            using (var source = File.OpenRead(sourcePath))
            using (var destination = new FileStream(destinationPath, append ? FileMode.Append : FileMode.Create))
            {
                return DecompressTo(source, destination);
            }
        }
    }
}
