/*
 * Copyright (c) 2012 Markus Olsson
 * var mail = string.Join(".", new string[] {"j", "markus", "olsson"}) + string.Concat('@', "gmail.com");
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish, 
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace DtaSpy
{
    /// <summary>
    /// Reads and if-need-be decompresses BizTalk fragment streams.
    /// </summary>
    public class BizTalkFragmentStream : Stream
    {
        /// <summary>
        /// Fragment streams consists of one or more of what I've come to call blocks which
        /// in turn consists of a header describing the block, it's compression mode (on/off),
        /// it's length (compressed and uncompressed).
        /// </summary>
        private sealed class Block : IDisposable
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="Block"/> is compressed.
            /// </summary>
            public bool Compressed { get; set; }

            /// <summary>
            /// Gets or sets the length of the block in bytes
            /// </summary>
            public int Length { get; set; }

            /// <summary>
            /// Gets or sets the length of the block contents. If this block is not 
            /// compressed this will be the same as the Length of the block and if it
            /// is compressed it will be the uncompressed length.
            /// </summary>
            public int UncompressedLength { get; set; }

            /// <summary>
            /// Gets or sets the content part of this block (ie, the block bytes excluding it's header)
            /// </summary>
            public Stream Content { get; set; }

            /// <summary>
            /// Gets or sets the number of read bytes in this block
            /// </summary>
            public int Read { get; set; }

            public bool IsEmpty
            {
                get { return (this.Length == 0) && (this.UncompressedLength == 0); }
            }

            public Block(bool compressed, int length, int uncompressedLength, Stream content)
            {
                this.Compressed = compressed;
                this.Length = length;
                this.UncompressedLength = uncompressedLength;
                this.Content = content;
            }

            public void Dispose()
            {
                this.Content.Dispose();
            }
        }

        private bool isClosed;
        private bool isDone;

        private Block currentBlock;

        private Stream innerStream;
        private CompressionMode compressionMode;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return !isClosed && !isDone && compressionMode == CompressionMode.Decompress; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return !isClosed && innerStream.CanWrite && compressionMode == CompressionMode.Compress; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream. Not supported by BizTalkCompressionStream.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public override long Length { get { throw new NotSupportedException(); } }

        /// <summary>
        /// Gets or sets the position within the current stream. Not supported by BizTalkCompressionStream.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking.</exception>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkFragmentStream"/> class.
        /// </summary>
        /// <param name="stream">The stream from which to read (or write if in compression mode).</param>
        /// <param name="mode">The compression mode.</param>
        public BizTalkFragmentStream(Stream stream, CompressionMode mode)
        {
            if (mode == CompressionMode.Compress)
                throw new NotSupportedException("Compression is not yet supported by this stream");

            if (mode != CompressionMode.Decompress)
                throw new ArgumentException("Unknown compression mode specified: " + mode, "mode");

            this.innerStream = stream;
            this.compressionMode = mode;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.compressionMode != CompressionMode.Decompress)
                throw new InvalidOperationException("Cannot read from compression stream");

            if (this.isDone)
                return 0;

            if (this.currentBlock == null)
                this.currentBlock = this.ReadBlock();

            if (this.currentBlock.IsEmpty)
            {
                this.isDone = true;
                this.currentBlock = null;
                return 0;
            }

            int read = this.currentBlock.Content.Read(buffer, offset, count);

            if (read == 0)
            {
                if (this.currentBlock.Compressed && (this.currentBlock.Read != this.currentBlock.UncompressedLength))
                    throw new IOException("Uncompressed block size did not match actual content length");

                this.currentBlock.Dispose();
                this.currentBlock = null;

                return this.Read(buffer, offset, count);
            }

            this.currentBlock.Read += read;

            return read;
        }

        private Block ReadBlock()
        {
            bool compressed = this.innerStream.ReadByte() == 1;

            // I don't know what these bytes represent
            this.innerStream.ReadByte();
            this.innerStream.ReadByte();
            this.innerStream.ReadByte();

            // 16bit little endian
            int uncompressedLength = this.innerStream.ReadByte() + (this.innerStream.ReadByte() << 8);

            // I don't know what these bytes represent
            this.innerStream.ReadByte();
            this.innerStream.ReadByte();

            // 16bit little endian
            int length = this.innerStream.ReadByte() + (this.innerStream.ReadByte() << 8);

            // I don't know what these bytes represent
            this.innerStream.ReadByte();
            this.innerStream.ReadByte();

            Stream contentStream = null;

            if (length > 0)
            {
                int read;
                byte[] blockBuffer = new byte[length];
                int total = 0;

                do
                {
                    read = this.innerStream.Read(blockBuffer, total, blockBuffer.Length - total);
                    total += read;
                }
                while (read > 0);

                contentStream = new MemoryStream(blockBuffer);

                if (compressed)
                    contentStream = new InflaterInputStream(contentStream);
            }

            return new Block(compressed, length, uncompressedLength, contentStream);
        }

        public override void Close()
        {
            base.Close();

            if (this.currentBlock != null)
                this.currentBlock.Dispose();

            this.isClosed = true;
        }

        public override void Flush()
        {
            if (this.compressionMode != CompressionMode.Compress)
                throw new InvalidOperationException("Cannot flush decompression stream");

            this.innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Cannot seek in compression streams");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length in compression streams");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.compressionMode != CompressionMode.Compress)
                throw new InvalidOperationException("Cannot write to compression stream");

            throw new NotImplementedException();
        }
    }
}
