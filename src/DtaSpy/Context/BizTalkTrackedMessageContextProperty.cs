using System;
using System.Diagnostics;

namespace DtaSpy
{
    [DebuggerDisplay("{Name}: {Value} ({Namespace})")]
    public class BizTalkTrackedMessageContextProperty
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the property.
        /// This is some internal BizTalk thingy which we shouldn't care about (http://msdn.microsoft.com/en-us/library/microsoft.biztalk.message.interop.contextpropertytype(v=bts.10).aspx)
        /// But we persist it to support the scenario persisting a complete property definition between reading and writing.
        /// </summary>
        public ContextPropertyType PropertyType { get; set; }

        public BizTalkTrackedMessageContextProperty()
        {
            this.PropertyType = ContextPropertyType.PropWritten; // Seems to be the default.
        }

        public BizTalkTrackedMessageContextProperty(string ns, string name, object value)
            : this(ns, name, value, ContextPropertyType.PropWritten)
        {
        }

        public BizTalkTrackedMessageContextProperty(string ns, string name, object value, ContextPropertyType propertyType)
        {
            this.Namespace = ns;
            this.Name = name;
            this.Value = value;
            this.PropertyType = propertyType;
        }
    }
}
