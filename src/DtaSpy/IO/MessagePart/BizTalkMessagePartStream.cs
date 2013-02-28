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
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace DtaSpy
{
    /// <summary>
    /// A compression stream for reading or writing from/to BizTalk message part fragment format.
    /// </summary>
    public class BizTalkMessagePartStream : Stream
    {
        private const int MaxBlockSize = 35840;

        private bool isClosed;
        private bool isDone;

        private FragmentBlock currentBlock;
        private MemoryStream readBuffer;

        private BizTalkBlockStream writeStream;
        private BizTalkBlockReader reader;

        private Stream innerStream;
        private StreamMode streamMode;
        private int currentBlockRead;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return !isClosed && !isDone && streamMode == StreamMode.Read; }
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
            get { return !isClosed && innerStream.CanWrite && streamMode == StreamMode.Write; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream. Not supported by BizTalkFragmentStream.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public override long Length
        {
            get
            {
                return innerStream.Length; // throw new NotSupportedException(); 
            }
        }
        /// <summary>
        /// Gets or sets the position within the current stream. Not supported by BizTalkFragmentStream.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking.</exception>
        public override long Position
        {
            get { return innerStream.Position; }
            set
            {
                innerStream.Position = value;// throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkMessagePartStream"/> class.
        /// </summary>
        /// <param name="stream">The stream from which to read (or write if in compression mode).</param>
        /// <param name="mode">The compression mode.</param>
        public BizTalkMessagePartStream(Stream stream, StreamMode mode)
        {
            this.innerStream = stream;
            this.streamMode = mode;

            if (mode == StreamMode.Write)
            {
                this.writeStream = new BizTalkBlockStream(this.innerStream);
            }
            else if (mode == StreamMode.Read)
            {
                this.reader = new BizTalkBlockReader(stream);
                this.readBuffer = new MemoryStream();
            }
            else
            {
                throw new ArgumentException("Unknown stream mode specified: " + mode, "mode");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.streamMode != StreamMode.Read)
                throw new InvalidOperationException("Cannot read from write stream");

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

            if (this.streamMode == StreamMode.Read)
            {
                if (this.currentBlock != null)
                    this.currentBlock = null;
            }
            else if (this.streamMode == StreamMode.Write)
            {
                this.writeStream.Close();
            }

            this.isClosed = true;
        }

        public override void Flush()
        {
            if (this.streamMode != StreamMode.Write)
                throw new InvalidOperationException("Cannot flush read stream");

            this.writeStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(0, SeekOrigin.Begin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length in compression streams");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.streamMode != StreamMode.Write)
                throw new InvalidOperationException("Cannot write to read stream");

            this.writeStream.Write(buffer, offset, count);
        }
    }
}
