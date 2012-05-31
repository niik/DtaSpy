using System;
using System.Linq;
using DtaSpy.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtaSpy.Tests.IO
{
    [TestClass]
    public class BizTalkMessageContextReaderTest
    {
        /// <summary>
        /// The message context for message 9c38fa72-868f-46aa-8a73-8d00da713700 is said to be a good sample of all available
        /// property types. This is a reference as much as a data source for further reverse-engineering.
        /// </summary>
        [TestMethod]
        public void ReadAllPropertyTypes()
        {
            var rawContext = ResourceHelper.LoadTestResource("Context/IM.BizTalk.Schemas.All.AllPropertiesSet/1.1/9c38fa72-868f-46aa-8a73-8d00da713700.bin");

            using (rawContext)
            using (var reader = new BizTalkMessageContextReader(rawContext))
            {
                var properties = reader.ReadContext().ToList();

                Assert.AreEqual(60, properties.Count);

                //<anyURIValue>http://www.example.com</anyURIValue>
                Assert.AreEqual("http://www.example.com", properties.First(p => p.Name.StartsWith("anyURI")).Value);

                //<booleanValue>true</booleanValue>
                Assert.AreEqual(true, properties.First(p => p.Name.StartsWith("boolean")).Value);

                //<byteValue>125</byteValue>
                Assert.AreEqual((sbyte)125, properties.First(p => p.Name.StartsWith("byte")).Value);

                //<dateValue>1999-05-31</dateValue>
                Assert.AreEqual(new DateTime(1999, 5, 31), properties.First(p => p.Name == "dateProperty").Value);

                //<dateTimeValue>1999-05-31T13:20:00.000-05:00</dateTimeValue>
                // AFAICT OLE dates doesn't support timezones so I guess the test data above has been generated on a computer with
                // a timezone setting of -5hours. No guarantees though. The date support is shaky at best.
                Assert.AreEqual(new DateTime(1999, 5, 31, 18, 20, 00), properties.First(p => p.Name.StartsWith("dateTime")).Value);

                //<decimalValue>10.4</decimalValue>
                Assert.AreEqual(10.4M, properties.First(p => p.Name.StartsWith("decimal")).Value);

                //<doubleValue>10</doubleValue>
                Assert.AreEqual(10D, properties.First(p => p.Name.StartsWith("double")).Value);

                //<ENTITYValue>ENTITYValue0</ENTITYValue>
                Assert.AreEqual("ENTITYValue0", properties.First(p => p.Name.StartsWith("ENTITY")).Value);

                //<floatValue>10</floatValue>
                Assert.AreEqual(10f, properties.First(p => p.Name.StartsWith("float")).Value);

                // Incomplete dates aren't supported yet.
                //<gDayValue>---31</gDayValue>
                //<gMonthValue>--05--</gMonthValue>
                //<gMonthDayValue>--05-31</gMonthDayValue>
                //<gYearValue>1999</gYearValue>
                //<gYearMonthValue>1999-02</gYearMonthValue>

                //<IDValue>IDValue0</IDValue>
                Assert.AreEqual("IDValue0", properties.First(p => p.Name.StartsWith("ID")).Value);

                //<IDREFValue>IDREFValue0</IDREFValue>
                Assert.AreEqual("IDREFValue0", properties.First(p => p.Name.StartsWith("IDREF")).Value);

                //<intValue>10</intValue>
                Assert.AreEqual(10, properties.First(p => p.Name.StartsWith("intProperty")).Value);

                //<integerValue>100</integerValue>
                Assert.AreEqual(100M, properties.First(p => p.Name.StartsWith("integerProperty")).Value);

                //<languageValue>en-US</languageValue>
                Assert.AreEqual("en-US", properties.First(p => p.Name.StartsWith("language")).Value);

                //<NameValue>NameValue_0</NameValue>
                Assert.AreEqual("NameValue_0", properties.First(p => p.Name.StartsWith("Name")).Value);

                //<NCNameValue>NCNameValue_0</NCNameValue>
                Assert.AreEqual("NCNameValue_0", properties.First(p => p.Name.StartsWith("NCName")).Value);

                //<negativeIntegerValue>-10</negativeIntegerValue>
                Assert.AreEqual(-10M, properties.First(p => p.Name.StartsWith("negativeInteger")).Value);

                //<NMTOKENValue>NMTOKENValue0</NMTOKENValue>
                Assert.AreEqual("NMTOKENValue0", properties.First(p => p.Name.StartsWith("NMTOKEN")).Value);

                //<nonNegativeIntegerValue>10</nonNegativeIntegerValue>
                Assert.AreEqual(10M, properties.First(p => p.Name.StartsWith("nonNegativeInteger")).Value);

                //<nonPositiveIntegerValue>-10</nonPositiveIntegerValue>
                Assert.AreEqual(-10M, properties.First(p => p.Name.StartsWith("nonPositiveInteger")).Value);

                //<normalizedStringValue>normalizedStringValue_0</normalizedStringValue>
                Assert.AreEqual("normalizedStringValue_0", properties.First(p => p.Name.StartsWith("normalizedString")).Value);

                //<NOTATIONValue>jpeg</NOTATIONValue>
                Assert.AreEqual("jpeg", properties.First(p => p.Name.StartsWith("NOTATION")).Value);

                //<positiveIntegerValue>100</positiveIntegerValue>
                Assert.AreEqual(100M, properties.First(p => p.Name.StartsWith("positiveInteger")).Value);

                //<QNameValue>QNameValue</QNameValue>
                Assert.AreEqual("QNameValue", properties.First(p => p.Name.StartsWith("QName")).Value);

                //<shortValue>10</shortValue>
                Assert.AreEqual((short)10, properties.First(p => p.Name.StartsWith("short")).Value);

                //<stringValue>stringValue_0</stringValue>
                Assert.AreEqual("stringValue_0", properties.First(p => p.Name.StartsWith("string")).Value);

                // Not supported
                //<timeValue>13:20:00.000-05:00</timeValue>

                //<tokenValue>tokenValue_0</tokenValue>
                Assert.AreEqual("tokenValue_0", properties.First(p => p.Name.StartsWith("token")).Value);

                //<unsignedByteValue>125</unsignedByteValue>
                Assert.AreEqual((byte)125, properties.First(p => p.Name.StartsWith("unsignedByte")).Value);

                //<unsignedIntValue>10</unsignedIntValue>
                Assert.AreEqual((uint)10, properties.First(p => p.Name.StartsWith("unsignedInt")).Value);

                //<unsignedShortValue>10</unsignedShortValue>
                Assert.AreEqual((ushort)10, properties.First(p => p.Name.StartsWith("unsignedShort")).Value);
            }
        }
    }
}
