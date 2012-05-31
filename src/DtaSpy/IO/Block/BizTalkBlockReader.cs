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
