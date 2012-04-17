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
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace DtaSpy
{
    /// <summary>
    /// Reads and if-need-be decompresses BizTalk fragment streams.
    /// </summary>
    public class BizTalkFragmentStream : Stream
    {
        private const int MaxBlockSize = 35840;

        private bool isOutputStarted;
        private bool isClosed;
        private bool isDone;

        private FragmentBlock currentBlock;
        private MemoryStream readBuffer;

        private MemoryStream writeBuffer;
        private MemoryStream deflateBuffer;

        private BizTalkFragmentBlockWriter writer;
        private BizTalkFragmentBlockReader reader;

        private Stream innerStream;
        private CompressionMode compressionMode;
        private int currentBlockRead;

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
        /// Gets the length in bytes of the stream. Not supported by BizTalkFragmentStream.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public override long Length { get { throw new NotSupportedException(); } }

        /// <summary>
        /// Gets or sets the position within the current stream. Not supported by BizTalkFragmentStream.
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
            this.innerStream = stream;
            this.compressionMode = mode;

            if (mode == CompressionMode.Compress)
            {
                this.writer = new BizTalkFragmentBlockWriter(this.innerStream);

                this.writeBuffer = new MemoryStream();
                this.deflateBuffer = new MemoryStream();
            }
            else if (mode == CompressionMode.Decompress)
            {
                this.reader = new BizTalkFragmentBlockReader(stream);
                this.readBuffer = new MemoryStream();
            }
            else
            {
                throw new ArgumentException("Unknown compression mode specified: " + mode, "mode");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.compressionMode != CompressionMode.Decompress)
                throw new InvalidOperationException("Cannot read from compression stream");

            if (this.isDone)
                return 0;

            if (this.currentBlock == null)
                this.ReadBlock();

            if (this.currentBlock.IsEmpty)
            {
                this.isDone = true;
                this.currentBlock = null;
                return 0;
            }

            int read = this.readBuffer.Read(buffer, offset, count);

            if (read == 0)
            {
                if (this.currentBlock.Compressed && (this.currentBlockRead != this.currentBlock.UncompressedLength))
                    throw new IOException("Uncompressed block size did not match actual content length");

                this.currentBlock = null;
                this.currentBlockRead = 0;

                return this.Read(buffer, offset, count);
            }

            this.currentBlockRead += read;

            return read;
        }

        private void ReadBlock()
        {
            this.currentBlock = this.reader.ReadBlock();

            if (this.currentBlock.IsEmpty)
            {
                this.readBuffer = new MemoryStream();
                return;
            }

            if (this.currentBlock.Compressed)
            {
                byte[] buffer = new byte[this.currentBlock.UncompressedLength];

                var inflater = new Inflater();

                inflater.SetInput(this.currentBlock.Content);
                inflater.Inflate(buffer);

                this.readBuffer = new MemoryStream(buffer);
            }
            else
            {
                this.readBuffer = new MemoryStream(this.currentBlock.Content);
            }
        }

        public override void Close()
        {
            base.Close();

            if (this.isClosed)
                return;

            if (this.compressionMode == CompressionMode.Decompress)
            {
                if (this.currentBlock != null)
                    this.currentBlock = null;
            }
            else if (this.compressionMode == CompressionMode.Compress)
            {
                this.FlushBuffer();

                if (isOutputStarted)
                    WriteBlock(false, 0, 0, new byte[0] { });

                this.innerStream.Flush();
            }

            this.isClosed = true;
        }

        public override void Flush()
        {
            if (this.compressionMode != CompressionMode.Compress)
                throw new InvalidOperationException("Cannot flush decompression stream");

            FlushBuffer();

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

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be a negative number");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Count cannot be a negative number");

            if ((buffer.Length - offset) < count)
                throw new ArgumentException("Offset and count would exceed buffer lenght");

            while (count > 0)
            {
                int written = 0;

                if (this.writeBuffer.Length + count > MaxBlockSize)
                {
                    this.writeBuffer.Write(buffer, offset, MaxBlockSize);
                    written = MaxBlockSize;
                }
                else
                {
                    this.writeBuffer.Write(buffer, offset, count);
                    written = count;
                }

                count -= written;
                offset += written;

                if (this.writeBuffer.Length == MaxBlockSize)
                    FlushBuffer();
            }
        }

        private void FlushBuffer()
        {
            if (this.writeBuffer == null)
                return;

            if (this.writeBuffer.Length == 0)
                return;

            // Brute force testing indicates that BizTalk never compressed content under 513 bytes
            if (this.writeBuffer.Length > 512)
            {
                this.deflateBuffer.SetLength(0);

                // Important, we wan't to match the RFC 1950 header used by BizTalk
                // see http://stackoverflow.com/questions/1316357/zlib-decompression-in-python
                var deflater = new Deflater(9);

                using (var deflateStream = new DeflaterOutputStream(this.deflateBuffer, deflater) { IsStreamOwner = false })
                    this.writeBuffer.WriteTo(deflateStream);

                // Don't use the deflated content if it won't save space (ie random data). This is mostly an 
                // optimization (which BizTalk performs as well) but it's also important that we don't exceed the
                // maximum block size.
                if (deflateBuffer.Length < this.writeBuffer.Length)
                {
                    WriteBlock(true, (int)this.deflateBuffer.Length, (int)this.writeBuffer.Length, this.deflateBuffer.ToArray());
                    ClearBuffer();
                    return;
                }
            }

            WriteBlock(false, (int)this.writeBuffer.Length, (int)this.writeBuffer.Length, this.writeBuffer.ToArray());
            ClearBuffer();
        }

        private void ClearBuffer()
        {
            this.writeBuffer.SetLength(0);
        }

        private void WriteBlock(bool compressed, int length, int uncompressedLength, byte[] buffer)
        {
            if (!this.isOutputStarted)
                this.isOutputStarted = true;

            var block = new FragmentBlock(compressed, length, uncompressedLength, buffer);

            this.writer.WriteBlock(block);
        }
    }
}
