using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
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

        public virtual IEnumerable<BizTalkTrackedMessageContextProperty> ReadContext()
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

                    ContextPropertyType propertyType = (ContextPropertyType)this.reader.ReadInt32();

                    object value = ReadValue();

                    yield return new BizTalkTrackedMessageContextProperty(ns, propertyName, value, propertyType);
                }
            }
        }

        private object ReadValue()
        {
            byte isArrayByte = reader.ReadByte();
            Debug.Assert(isArrayByte == 0 || isArrayByte == 1);
            bool isArray = isArrayByte == 1;

            VarEnum valueType = (VarEnum)reader.ReadByte();
            byte ignored = reader.ReadByte();

            // This 0x20 thing has something to do with VT_ARRAY (0x2000) and little endian
            // and something else but 0x2001 would suggest that VarEnum is a flag and if that's
            // the case then 0x2001 would be VT_ARRAY | VT_NULL which might make sense but I
            // just don't know enough yet.
            Debug.Assert(ignored == 0 || (isArray && ignored == 0x20));

            if (isArray)
                return ReadArray(valueType);

            return ReadValue(valueType);
        }

        private object ReadArray(VarEnum elementType)
        {
            // The layout seems to match that of a ARRAYDESC with its SAFEARRAYBOUND
            // http://msdn.microsoft.com/en-us/library/aa908862
            // http://msdn.microsoft.com/en-us/library/aa917627

            ushort dimensions = reader.ReadUInt16();
            Debug.Assert(dimensions == 1);

            int length = reader.ReadInt32();

            int lowerBound = reader.ReadInt32();
            Debug.Assert(lowerBound == 0);

            Array valueArray = Array.CreateInstance(GetManagedType(elementType), length);

            for (int i = 0; i < length; i++)
                valueArray.SetValue(ReadValue(elementType), i);

            return valueArray;
        }

        private Type GetManagedType(VarEnum valueType)
        {
            switch (valueType)
            {
                case VarEnum.VT_I2: return typeof(short);
                case VarEnum.VT_I4: return typeof(int);
                case VarEnum.VT_R4: return typeof(float);
                case VarEnum.VT_R8: return typeof(double);
                case VarEnum.VT_DATE: return typeof(DateTime);
                case VarEnum.VT_BSTR: return typeof(string);
                case VarEnum.VT_BOOL: return typeof(bool);
                case VarEnum.VT_DECIMAL: return typeof(string);
                case VarEnum.VT_I1: return typeof(sbyte);
                case VarEnum.VT_UI1: return typeof(byte);
                case VarEnum.VT_UI2: return typeof(ushort);
                case VarEnum.VT_UI4: return typeof(uint);
                default:
                    throw new NotSupportedException();
            }
        }

        private object ReadValue(VarEnum valueType)
        {
            // I believe the value types map roughly to the variant types as specified in
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380072(v=vs.85).aspx
            // It maps even more closely to the subset specified in
            // http://msdn.microsoft.com/en-us/library/ms897140.aspx (Windows CE 5.0 documentation)
            switch (valueType)
            {
                case VarEnum.VT_I2: return reader.ReadInt16();
                case VarEnum.VT_I4: return reader.ReadInt32();
                case VarEnum.VT_R4: return reader.ReadSingle();
                case VarEnum.VT_R8: return reader.ReadDouble();
                // case 6: VT_CY: 8-byte two's complement integer (scaled by 10,000). This type is commonly used for currency amounts.
                case VarEnum.VT_DATE: return ReadDateTime();
                case VarEnum.VT_BSTR: return ReadLengthPrefixedString();
                // case 9: VT_DISPATCH
                // case 10: VT_ERROR
                case VarEnum.VT_BOOL: return ReadBoolean();
                // case 12: VT_VARIANT 
                // case 13: VT_UNKNOWN
                case VarEnum.VT_DECIMAL: return ReadDecimal();
                // case 15: Unused
                case VarEnum.VT_I1: return reader.ReadSByte();
                case VarEnum.VT_UI1: return reader.ReadByte();
                case VarEnum.VT_UI2: return reader.ReadUInt16();
                case VarEnum.VT_UI4: return reader.ReadUInt32();
                default:
                    throw new NotSupportedException();
            }
        }

        private decimal ReadDecimal()
        {
            // Okay, this freaks me out. All the other types codes I've seen seems to map directly to the VarEnum
            // enum and thus to the corresponding VT_ types. By that logic we should be seeing a VT_DECIMAL struct
            // here (http://blogs.microsoft.co.il/blogs/arik/archive/2009/10/16/setting-decimal-value-on-propvariant.aspx)
            // but as far as I can tell it's a BSTR. I don't know if we should bubble it up the chain as a string
            // or not. We need more data on this. If you end up here it'd be swell if you could provide some sample
            // data with more complex decimals.
            
             return decimal.Parse(ReadLengthPrefixedString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
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

        private DateTime ReadDateTime()
        {
            // Epic! Fought like crazy to get this to work using my own 
            // reverse-engineered hackish solution and it turns out .net has
            // built-in support for OLE-dates :)
            return DateTime.FromOADate(this.reader.ReadDouble());
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
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.stream != null)
                {
                    this.stream.Close();
                    this.stream = null;
                }
            }
        }
    }
}
