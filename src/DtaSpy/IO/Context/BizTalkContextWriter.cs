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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DtaSpy
{
    /// <summary>
    /// Experimental context writer. Blind inverted implementation of the
    /// context reader, please refer to it for documentation and/or guidance.
    /// </summary>
    public class BizTalkContextWriter : IDisposable
    {
        private Stream stream;
        private BinaryWriter writer;

        private static readonly byte[] PropertyContextClassId = new Guid("6c90e0c4-4918-11d3-a242-00c04f60a533").ToByteArray();

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkContextWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public BizTalkContextWriter(Stream stream)
        {
            this.stream = stream;
            this.writer = new BinaryWriter(stream);
        }

        public virtual void WriteContext(BizTalkPropertyBag context)
        {
            this.WriteContext(context.Properties);
        }

        public virtual void WriteContext(IEnumerable<BizTalkContextProperty> properties)
        {
            // I'm guessing namespaces are always lower case (since they're uris) and even if they're not I'll treat them as such.
            var propertiesByNamespace = new Dictionary<string, List<BizTalkContextProperty>>(StringComparer.OrdinalIgnoreCase);
            var namespacesInOrderOfAppearance = new List<string>();

            foreach (var property in properties)
            {
                List<BizTalkContextProperty> pl;

                if (!propertiesByNamespace.TryGetValue(property.Namespace, out pl))
                {
                    pl = new List<BizTalkContextProperty>();
                    propertiesByNamespace.Add(property.Namespace, pl);
                    namespacesInOrderOfAppearance.Add(property.Namespace);
                }

                pl.Add(property);
            }

            this.writer.Write(PropertyContextClassId);

            this.writer.Write((int)propertiesByNamespace.Count);

            foreach (var ns in namespacesInOrderOfAppearance)
            {
                List<BizTalkContextProperty> props = propertiesByNamespace[ns];

                this.WriteLengthPrefixedString(ns);

                int propertyCount = props.Count;
                this.writer.Write(propertyCount);

                foreach (var property in props)
                    WriteProperty(property);
            }
        }

        protected virtual void WriteProperty(BizTalkContextProperty property)
        {
            this.WriteLengthPrefixedString(property.Name);
            this.writer.Write((int)property.PropertyType);

            WriteValue(property.Value);
        }

        protected virtual void WriteValue(object value)
        {
            if (value == null)
            {
                // I don't know if this is supported in BizTalk, haven't seen any null values in
                // the test databases I've received but I'm guessing it should be implemented with
                // VT_NULL if supported. Skipping for now though.
                throw new NotImplementedException("Null property values not supported");
            }

            var propertyType = value.GetType();

            bool isArray = propertyType.IsArray;

            if (isArray)
                propertyType = propertyType.GetElementType();

            this.writer.Write((byte)(isArray ? 1 : 0));

            VarEnum valueType = GetElementVariantType(propertyType);

            this.writer.Write((byte)valueType);
            this.writer.Write((byte)(isArray ? 0x20 : 0));


            if (isArray)
                WriteArray(valueType, (Array)value);
            else
                WriteValue(valueType, value);
        }

        protected virtual VarEnum GetElementVariantType(Type propertyType)
        {
            if (propertyType == typeof(short))
                return VarEnum.VT_I2;

            if (propertyType == typeof(int))
                return VarEnum.VT_I4;

            if (propertyType == typeof(float))
                return VarEnum.VT_R4;

            if (propertyType == typeof(double))
                return VarEnum.VT_R8;

            if (propertyType == typeof(DateTime))
                return VarEnum.VT_DATE;

            if (propertyType == typeof(string))
                return VarEnum.VT_BSTR;

            if (propertyType == typeof(bool))
                return VarEnum.VT_BOOL;

            if (propertyType == typeof(decimal))
                return VarEnum.VT_DECIMAL;

            if (propertyType == typeof(sbyte))
                return VarEnum.VT_I1;

            if (propertyType == typeof(byte))
                return VarEnum.VT_UI1;

            if (propertyType == typeof(ushort))
                return VarEnum.VT_UI2;

            if (propertyType == typeof(uint))
                return VarEnum.VT_UI4;

            throw new NotSupportedException();
        }

        protected virtual void WriteArray(VarEnum elementType, Array value)
        {
            if (value.Rank != 1)
                throw new NotSupportedException("Multi dimensional arrays are not supported");

            // I'm guessing this is the number of dimensions but I'm really not sure.
            this.writer.Write((short)value.Rank);

            int length = value.GetLength(0);
            this.writer.Write(length);

            int lb = (int)value.GetLowerBound(0);
            if (lb != 0)
                throw new NotSupportedException("Non-zero lowerbound not supported");

            this.writer.Write(lb);

            for (int i = 0; i < length; i++)
                WriteValue(elementType, value.GetValue(i));
        }

        protected virtual void WriteValue(VarEnum valueType, object value)
        {
            switch (valueType)
            {
                case VarEnum.VT_I2:
                    this.writer.Write((short)value);
                    break;
                case VarEnum.VT_I4:
                    this.writer.Write((int)value);
                    break;
                case VarEnum.VT_R4:
                    this.writer.Write((float)value);
                    break;
                case VarEnum.VT_R8:
                    this.writer.Write((double)value);
                    break;
                case VarEnum.VT_DATE:
                    this.WriteDateTime((DateTime)value);
                    break;
                case VarEnum.VT_BSTR:
                    this.WriteLengthPrefixedString((string)value);
                    break;
                case VarEnum.VT_BOOL:
                    this.WriteBoolean((bool)value);
                    break;
                case VarEnum.VT_DECIMAL:
                    this.WriteDecimal((decimal)value);
                    break;
                case VarEnum.VT_I1:
                    this.writer.Write((sbyte)value);
                    break;
                case VarEnum.VT_UI1:
                    this.writer.Write((byte)value);
                    break;
                case VarEnum.VT_UI2:
                    this.writer.Write((ushort)value);
                    break;
                case VarEnum.VT_UI4:
                    this.writer.Write((uint)value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteDecimal(decimal value)
        {
            this.WriteLengthPrefixedString(value.ToString(CultureInfo.InvariantCulture));
        }

        private void WriteBoolean(bool value)
        {
            this.writer.Write((short)(value ? -1 : 0));
        }

        private void WriteDateTime(DateTime value)
        {
            this.writer.Write((double)value.ToOADate());
        }

        private void WriteLengthPrefixedString(string value)
        {
            byte[] buf = Encoding.Unicode.GetBytes(value);

            this.writer.Write((int)(buf.Length + 2));
            this.writer.Write(buf);

            // Null terminated
            this.writer.Write((byte)0);
            this.writer.Write((byte)0);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this.stream.Close();
        }
    }
}
