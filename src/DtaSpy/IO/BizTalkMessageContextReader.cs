using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DtaSpy
{
    /// <summary>
    /// Experimental context reader. Still havent reverse-engineered the entire format. 
    /// A bit shaky and full of Asserts. Use with caution.
    /// </summary>
    public class BizTalkMessageContextReader : IDisposable
    {
        private Stream stream;
        private BinaryReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkMessageContextReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public BizTalkMessageContextReader(Stream stream)
        {
            this.stream = stream;
            this.reader = new BinaryReader(stream);
        }

        public IEnumerable<BizTalkTrackedMessageContextProperty> ReadContext()
        {
            var clsid = new Guid(reader.ReadBytes(16));

            // Markus: Can somebody shine some light on what this GUID represents? I haven't
            // got BizTalk myself so I can only resort to google and MSDN. I've found a couple
            // of nearby GUIDs that has to do with BizTalk by googling for different parts of
            // the GUID so I'm certain it is a GUID but I don't know what it represent or if
            // it's just an opaque ID.
            Debug.Assert(clsid == new Guid("6c90e0c4-4918-11d3-a242-00c04f60a533"));

            int namespaceCount = reader.ReadInt32();

            for (int i = 0; i < namespaceCount; i++)
            {
                string ns = ReadLengthPrefixedString();

                int propertyCount = reader.ReadInt32();

                if (propertyCount == 0)
                    break;

                for (int j = 0; j < propertyCount; j++)
                {
                    string propertyName = ReadLengthPrefixedString();

                    int unknown = reader.ReadInt32();
                    Debug.Assert(unknown == 1 || unknown == 2 || unknown == 16);

                    object value = ReadValue();

                    yield return new BizTalkTrackedMessageContextProperty(ns, propertyName, value);
                }
            }
        }

        private object ReadValue()
        {
            byte isArrayByte = reader.ReadByte();
            Debug.Assert(isArrayByte == 0 || isArrayByte == 1);
            bool isArray = isArrayByte == 1;

            byte valueType = reader.ReadByte();
            byte ignored = reader.ReadByte();

            Debug.Assert(ignored == 0 || (isArray && ignored == 0x20));

            if (isArray)
                return ReadArray(valueType);

            return ReadValue(valueType);
        }

        private object ReadArray(byte elementType)
        {
            // I'm guessing this is the number of dimensions but I'm really not sure.
            short dimensions = reader.ReadInt16();
            Debug.Assert(dimensions == 1);

            int length = reader.ReadInt32();

            int lowerBound = reader.ReadInt32(); // Lower bound is just a guess, I really don't know what else it could be though
            Debug.Assert(lowerBound == 0);

            Array valueArray = Array.CreateInstance(GetManagedType(elementType), length);

            for (int i = 0; i < length; i++)
                valueArray.SetValue(ReadValue(elementType), i);

            return valueArray;
        }

        private Type GetManagedType(byte valueType)
        {
            switch (valueType)
            {
                case 3:
                    return typeof(int);
                case 7:
                    return typeof(DateTime);
                case 8:
                    return typeof(string);
                case 11:
                    return typeof(bool);
                case 19:
                    return typeof(uint);
                default:
                    throw new NotSupportedException();
            }
        }

        private object ReadValue(byte valueType)
        {
            // I believe the valut types either map directly to variant types as specified in
            // http://en.wikipedia.org/wiki/Variant_type#Types or some preceeding standard.
            // It seems to coincide perfectly so far but we're still lacking support for floats
            // and some other types since I haven't seen any such types in message contexts yet.
            switch (valueType)
            {
                case 3:
                    return ReadInt32();
                case 7:
                    return ReadDateTime();
                case 8:
                    return ReadLengthPrefixedString();
                case 11:
                    return ReadBoolean();
                case 19:
                    return ReadUInt32();
                default:
                    throw new NotSupportedException();
            }
        }

        private bool ReadBoolean()
        {
            // http://msdn.microsoft.com/en-us/library/cc237864(v=prot.10).aspx
            // VARIANT_TRUE 0XFFFF
            // VARIANT_FALSE 0X000
            var b = reader.ReadInt16();

            if (b == -1)
                return true;
            else if (b == 0)
                return false;

            throw new FormatException();
        }

        private int ReadInt32()
        {
            return reader.ReadInt32();
        }

        private uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }

        private DateTime ReadDateTime()
        {
            // Epic! Fought like crazy to get this to work using my own 
            // reverse-engineered hackish solution and it turns out .net has
            // built-in support for OLE-dates :)
            return DateTime.FromOADate(reader.ReadDouble());
        }

        private string ReadLengthPrefixedString()
        {
            int lenght = reader.ReadInt32();
            byte[] buf = reader.ReadBytes(lenght);

            string s = Encoding.Unicode.GetString(buf, 0, buf.Length - 2);

            Debug.Assert(buf[buf.Length - 1] == 0 && buf[buf.Length - 2] == 0, "String was not null terminated as expected");

            return s;
        }

        public void Dispose()
        {
        }
    }
}
