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
using System.Diagnostics;

namespace DtaSpy
{
    [DebuggerDisplay("{Name}: {Value} ({Namespace})")]
    public class BizTalkContextProperty
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the property.
        /// This is some internal BizTalk thingy which we shouldn't care about (http://msdn.microsoft.com/en-us/library/microsoft.biztalk.message.interop.contextpropertytype(v=bts.10).aspx)
        /// But we persist it to support the scenario persisting a complete property definition between reading and writing.
        /// </summary>
        public PropertyType PropertyType { get; set; }

        public BizTalkContextProperty()
        {
            this.PropertyType = PropertyType.PropWritten; // Seems to be the default.
        }

        public BizTalkContextProperty(string ns, string name, object value)
            : this(ns, name, value, PropertyType.PropWritten)
        {
        }

        public BizTalkContextProperty(string ns, string name, object value, PropertyType propertyType)
        {
            this.Namespace = ns;
            this.Name = name;
            this.Value = value;
            this.PropertyType = propertyType;
        }

        public override string ToString()
        {
            return Convert.ToString(Value);
        }
    }
}
