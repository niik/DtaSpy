using System;

namespace DtaSpy
{
    public class BizTalkTrackedMessageContextProperty
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public BizTalkTrackedMessageContextProperty()
        {
        }

        public BizTalkTrackedMessageContextProperty(string ns, string name, object value)
        {
            this.Namespace = ns;
            this.Name = name;
            this.Value = value;
        }
    }
}
