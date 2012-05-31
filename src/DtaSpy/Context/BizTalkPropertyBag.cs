using System;
using System.Collections.Generic;

namespace DtaSpy
{
    public class BizTalkPropertyBag: ICollection<BizTalkContextProperty>
    {
        public List<BizTalkContextProperty> Properties { get; set; }

        public BizTalkPropertyBag()
        {
            this.Properties = new List<BizTalkContextProperty>();
        }

        public BizTalkPropertyBag(IEnumerable<BizTalkContextProperty> properties)
        {
            this.Properties = new List<BizTalkContextProperty>(properties);
        }

        public bool TryGetProperty(string ns, string name, out BizTalkContextProperty property)
        {
            foreach (var p in this.Properties)
            {
                if (p.Namespace == ns && p.Name == name)
                {
                    property = p;
                    return true;
                }
            }

            property = null;
            return false;
        }

        public void Add(string ns, string name, object value, PropertyType type)
        {
            this.Add(new BizTalkContextProperty
            {
                Namespace = ns,
                Name = name,
                Value = value,
                PropertyType = type
            });
        }

        public void Add(BizTalkContextProperty property)
        {
            this.Properties.Add(property);
        }

        public void Clear()
        {
            this.Properties.Clear();
        }

        public IEnumerator<BizTalkContextProperty> GetEnumerator()
        {
            return this.Properties.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)(this.Properties)).GetEnumerator();
        }

        public bool Contains(BizTalkContextProperty property)
        {
            return this.Properties.Contains(property);
        }

        public void CopyTo(BizTalkContextProperty[] array, int arrayIndex)
        {
            this.Properties.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.Properties.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(BizTalkContextProperty property)
        {
            return this.Properties.Remove(property);
        }
    }
}
