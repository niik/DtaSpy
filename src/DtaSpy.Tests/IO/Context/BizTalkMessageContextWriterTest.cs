using System.IO;
using System.Linq;
using DtaSpy.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtaSpy.Tests.IO
{
    [TestClass]
    public class BizTalkMessageContextWriterTest
    {
        /// <summary>
        /// The message context for message 9c38fa72-868f-46aa-8a73-8d00da713700 is said to be a good sample of all available
        /// property types.
        /// </summary>
        [TestMethod]
        public void AllPropertiesSerialization()
        {
            var rawContext = ResourceHelper.LoadTestResource("Context/IM.BizTalk.Schemas.All.AllPropertiesSet/1.1/9c38fa72-868f-46aa-8a73-8d00da713700.bin");

            AssertIdenticalSerialization(rawContext);
        }

        /// <summary>
        /// Asserts that a stream which is first deserialized by a context reader and then serialized
        /// with a context writer is byte-wise identical to the raw input stream.
        /// </summary>
        private static void AssertIdenticalSerialization(Stream rawContext)
        {
            using (rawContext)
            using (var msOut = new MemoryStream())
            using (var reader = new BizTalkMessageContextReader(rawContext))
            using (var writer = new BizTalkMessageContextWriter(msOut))
            {
                var properties = reader.ReadContext().ToList();

                writer.WriteContext(properties);

                rawContext.Seek(0, SeekOrigin.Begin);
                msOut.Seek(0, SeekOrigin.Begin);

                Assert.AreEqual(rawContext.Length, msOut.Length);

                for (int i = 0; i < rawContext.Length; i++)
                {
                    var expected = rawContext.ReadByte();
                    var actual = msOut.ReadByte();
                    Assert.AreEqual(expected, actual, "Mismatch at offset 0x{0:X2}, expected 0x{1:X2}, actual 0x{2:X2}", i, expected, actual);
                }
            }
        }
    }
}
