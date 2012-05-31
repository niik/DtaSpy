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
        /// <summary>
        /// Extracts the given tracking data and returns the decompressed content as a byte array
        /// </summary>
        public static byte[] Decompress(byte[] buffer)
        {
            using (var source = new MemoryStream(buffer))
            using (var destination = new MemoryStream())
            {
                DecompressTo(source, destination);
                
                return destination.ToArray();
            }
        }

        /// <summary>
        /// Extracts the tracking data withing the given stream and returns the 
        /// decompressed content as a byte array
        /// </summary>
        public static byte[] Decompress(Stream source)
        {
            using (var ms = new MemoryStream())
            {
                DecompressTo(source, ms);
                
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Extracts the tracking data within the given source stream and writes the
        /// decompressed content into the given destination stream.
        /// </summary>
        public static int DecompressTo(Stream source, Stream destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (destination == null)
                throw new ArgumentNullException("destination");

            using (var decoder = new BizTalkMessagePartStream(source, StreamMode.Read))
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
    }
}
