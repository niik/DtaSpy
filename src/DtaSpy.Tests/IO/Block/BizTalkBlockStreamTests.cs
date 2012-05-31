using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DtaSpy.Tests.IO
{
    /// <summary>
    /// Tests with sample blockwriter illustrating how to get hold of individual fragments
    /// </summary>
    [TestClass]
    public class BizTalkBlockStreamTests
    {
        /// <summary>
        /// Sample event/callback based blockwriter which instead of writing to a stream (like
        /// the default writer) passes along the blocks to the supplied callback.
        /// </summary>
        private class ObjectBlockWriter : IBlockWriter
        {
            private Action<FragmentBlock> callback;

            public ObjectBlockWriter(Action<FragmentBlock> callback)
            {
                this.callback = callback;
            }

            public void WriteBlock(FragmentBlock block)
            {
                this.callback(block);
            }

            public void WriteBlock(byte[] buffer, int offset, int count, bool compressed, int uncompressedLength)
            {
                // Make a copy of the buffer
                byte[] buf = new byte[count];
                Array.Copy(buffer, offset, buf, 0, count);

                this.WriteBlock(new FragmentBlock(compressed, count, uncompressedLength, buf));
            }

            public void Flush() { }
            public void Close() { }
            public void Dispose() { }
        }

        [TestMethod]
        public void SingleBlockTest()
        {
            var content = new string('X', 100);

            var blocks = new List<FragmentBlock>();
            var bw = new ObjectBlockWriter(b => blocks.Add(b));

            using (var s = new BizTalkBlockStream(bw))
            using (var sw = new StreamWriter(s))
            {
                sw.Write(content);
            }

            Assert.AreEqual(2, blocks.Count);
            Assert.IsTrue(blocks.Last().IsEmpty);

            Assert.AreEqual(100, blocks[0].UncompressedLength);
        }

        [TestMethod]
        public void DoubleBlockTest()
        {
            var content = new string('X', 65536);

            var blocks = new List<FragmentBlock>();
            var bw = new ObjectBlockWriter(b => blocks.Add(b));

            using (var s = new BizTalkBlockStream(bw))
            using (var sw = new StreamWriter(s))
            {
                sw.Write(content);
            }

            Assert.AreEqual(3, blocks.Count);
            Assert.IsTrue(blocks.Last().IsEmpty);

            Assert.AreEqual(35840, blocks[0].UncompressedLength);
            Assert.AreEqual(29696, blocks[1].UncompressedLength);

            Assert.IsTrue(blocks[0].Compressed);
            Assert.IsTrue(blocks[1].Compressed);
        }
    }
}
