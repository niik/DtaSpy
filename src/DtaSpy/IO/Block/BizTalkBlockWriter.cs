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

namespace DtaSpy
{
    public class BizTalkBlockWriter : IBlockWriter
    {
        public Stream output { get; set; }
        private static readonly byte[] zeroBuf = new byte[] { 0, 0, 0 };

        public BizTalkBlockWriter(Stream output)
        {
            this.output = output;
        }

        public void WriteBlock(FragmentBlock block)
        {
            WriteBlock(block.Content, 0, block.Length, block.Compressed, block.UncompressedLength);
        }

        public void WriteBlock(byte[] buffer, int offset, int count, bool compressed, int uncompressedLength)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be a negative number");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Count cannot be a negative number");

            if ((buffer.Length - offset) < count)
                throw new ArgumentException("Offset and count would exceed buffer lenght");

            var bw = new BinaryWriter(this.output);

            bw.Write((int)(compressed ? 1 : 0));

            // Content length (uncompressed length) 32bit little endian
            bw.Write((int)uncompressedLength);

            // 32bit little endian
            bw.Write((int)count);

            if (count > 0)
                this.output.Write(buffer, offset, count);
        }

        public void Flush()
        {
            this.output.Flush();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this.output.Close();
        }

        public void Close()
        {
            this.Dispose(true);
        }
    }
}
