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
    }
}
