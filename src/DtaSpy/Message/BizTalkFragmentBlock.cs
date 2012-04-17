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
