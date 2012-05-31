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
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DtaSpy
{
    /// <summary>
    /// Fragment streams consists of one or more of what I've come to call blocks which
    /// in turn consists of a header describing the block, it's compression mode (on/off),
    /// it's length (compressed and uncompressed).
    /// </summary>
    public sealed class FragmentBlock
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
        public byte[] Content { get; set; }

        public bool IsEmpty
        {
            get { return (this.Length == 0) && (this.UncompressedLength == 0); }
        }

        public FragmentBlock()
        {
        }

        public FragmentBlock(bool compressed, int length, int uncompressedLength, byte[] content)
        {
            this.Compressed = compressed;
            this.Length = length;
            this.UncompressedLength = uncompressedLength;
            this.Content = content;
        }
    }
}
