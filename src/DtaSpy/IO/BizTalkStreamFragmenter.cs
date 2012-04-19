using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace DtaSpy
{
    public class BizTalkStreamFragmenter : Stream
    {
        private const int MaxBlockSize = 35840;
        private IBlockWriter blockWriter;

        private MemoryStream writeBuffer;
        private MemoryStream deflateBuffer;

        private bool isOutputStarted;
        private bool isClosed;

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public BizTalkStreamFragmenter(Stream output)
            : this(new BizTalkFragmentBlockWriter(output))
        {
        }

        public BizTalkStreamFragmenter(IBlockWriter blockWriter)
        {
            if (blockWriter == null)
                throw new ArgumentNullException("blockWriter");

            this.blockWriter = blockWriter;
            this.writeBuffer = new MemoryStream();
            this.deflateBuffer = new MemoryStream();
        }

        public override void Flush()
        {
            this.blockWriter.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
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
                    this.blockWriter.WriteBlock(this.deflateBuffer.GetBuffer(), 0, (int)this.deflateBuffer.Length, true, (int)this.writeBuffer.Length);
                    this.isOutputStarted = true;
                    ClearBuffer();
                    return;
                }
            }

            this.blockWriter.WriteBlock(this.writeBuffer.GetBuffer(), 0, (int)this.writeBuffer.Length, false, (int)this.writeBuffer.Length);
            this.isOutputStarted = true;
            ClearBuffer();
        }

        private void ClearBuffer()
        {
            this.writeBuffer.SetLength(0);
        }

        public override void Close()
        {
            base.Close();

            if (this.isClosed)
                return;

            this.isClosed = true;

            this.FlushBuffer();

            if (isOutputStarted)
                this.blockWriter.WriteBlock(new byte[] { }, 0, 0, false, 0);

            this.blockWriter.Close();
        }
    }
}
