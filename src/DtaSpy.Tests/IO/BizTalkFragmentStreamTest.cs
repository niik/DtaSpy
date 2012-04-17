using DtaSpy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DtaSpy.Tests
{
    [TestClass]
    public class BizTalkFragmentStreamTest
    {
        /// <summary>
        /// A byte array representing a fragment stream consisting of a single 'X' char.
        /// </summary>
        private static byte[] singleXBuffer = new byte[] {
            0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // Block #1 header
            0x58,                                                                   // Block #1 content
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Empty block header
        };


        /// <summary>
        /// A byte array representing a fragment stream consisting of a 1024 'X' characters
        /// in one block.
        /// </summary>
        private static byte[] singleBlock1024XBuffer = new byte[] {
            0x01, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, // Block #1 header
            0x78, 0xDA, 0x8B, 0x88, 0x18, 0x05, 0xA3, 0x60, 0x14, 0x8C, 0x54, 0x00, // Block #1 content
            0x00, 0xDD, 0x40, 0x60, 0x10,                                           // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // Empty block header
        };

        /// <summary>
        /// A byte array representing a fragment stream consisting of a 65k 'X' characters
        /// in two blocks
        /// </summary>
        private static byte[] doubleBlock65535XBuffer = new byte[] {
            0x01, 0x00, 0x00, 0x00, 0x00, 0x8C, 0x00, 0x00, 0x3A, 0x00, 0x00, 0x00, // Block #1 header
            0x78, 0xDA, 0xED, 0xC1, 0x81, 0x00, 0x00, 0x00, 0x00, 0xC3, 0x20, 0xCD, // Block #1 content
            0xF9, 0x93, 0x1C, 0xE4, 0x55, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ...
            0x00, 0x00, 0x00, 0xC0, 0x8F, 0x01, 0x0C, 0x42, 0x22, 0xD1,             // ...
            0x01, 0x00, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x35, 0x00, 0x00, 0x00, // Block #2 header
            0x78, 0xDA, 0xED, 0xC1, 0x81, 0x00, 0x00,                               // Block #2 content
            0x00, 0x00, 0xC3, 0x20, 0xCD, 0xF9, 0x93, 0x9C, 0xE0, 0x06, 0x55, 0x01, // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ...
            0x00, 0x00, 0x00, 0x00, 0xCF, 0x00, 0xF0, 0x55, 0xE2, 0x4A,             // ...
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // Empty block header
        };

        [TestMethod]
        public void UncompressedSingleBlockStreamTest()
        {
            using (var ms = new MemoryStream(singleXBuffer))
            using (var bs = new BizTalkFragmentStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(bs))
            {
                Assert.AreEqual("X", sr.ReadToEnd());
            }
        }

        [TestMethod]
        public void CompressedSingleBlockStreamTest()
        {
            using (var ms = new MemoryStream(singleBlock1024XBuffer))
            using (var bs = new BizTalkFragmentStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(bs))
            {
                string content = sr.ReadToEnd();

                Assert.AreEqual(1024, content.Length);

                for (int i = 0; i < content.Length; i++)
                    Assert.AreEqual('X', content[i], "Char at offset " + i + " was not X as expected");
            }
        }

        [TestMethod]
        public void CompressedSingleBlockStructureTest()
        {
            var reader = new BizTalkFragmentBlockReader(singleBlock1024XBuffer);

            var b1 = reader.ReadBlock();
            Assert.IsNotNull(b1);
            Assert.AreEqual(1024, b1.UncompressedLength);

            var b2 = reader.ReadBlock();
            Assert.IsNotNull(b2);
            Assert.AreEqual(0, b2.UncompressedLength);
            Assert.IsTrue(b2.IsEmpty);
        }

        [TestMethod]
        public void CompressedMultiBlockStructureTest()
        {
            var reader = new BizTalkFragmentBlockReader(doubleBlock65535XBuffer);

            var b1 = reader.ReadBlock();
            Assert.IsNotNull(b1);
            Assert.IsTrue(b1.Compressed);
            Assert.AreEqual(58, b1.Length);
            Assert.AreEqual(35840, b1.UncompressedLength);

            var b2 = reader.ReadBlock();
            Assert.IsNotNull(b2);
            Assert.IsTrue(b2.Compressed);
            Assert.AreEqual(53, b2.Length);
            Assert.AreEqual(29696, b2.UncompressedLength);

            var b3 = reader.ReadBlock();
            Assert.IsNotNull(b3);
            Assert.IsFalse(b3.Compressed);
            Assert.AreEqual(0, b3.UncompressedLength);
            Assert.IsTrue(b3.IsEmpty);
        }

        [TestMethod]
        public void WriteUncompressedTest()
        {
            // To small content to get compressed
            var content = new string('X', 100);
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                using (var bz = new BizTalkFragmentStream(ms, CompressionMode.Compress))
                using (var sw = new StreamWriter(bz))
                {
                    sw.Write(content);
                }

                buffer = ms.ToArray();
            }

            // Compression bit should not be set
            Assert.AreEqual(0, buffer[0]);

            using (var ms = new MemoryStream(buffer))
            using (var bz = new BizTalkFragmentStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(bz))
            {
                Assert.AreEqual(content, sr.ReadToEnd());
            }

        }

        [TestMethod]
        public void WriteCompressedTest()
        {
            // To small content to get compressed
            var content = new string('X', 1024);
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                using (var bz = new BizTalkFragmentStream(ms, CompressionMode.Compress))
                using (var sw = new StreamWriter(bz))
                {
                    sw.Write(content);
                }

                buffer = ms.ToArray();
            }

            // Compression bit should not be set
            Assert.AreEqual(1, buffer[0]);

            using (var ms = new MemoryStream(buffer))
            using (var bz = new BizTalkFragmentStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(bz))
            {
                Assert.AreEqual(content, sr.ReadToEnd());
            }

        }
    }
}
