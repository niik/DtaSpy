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
using System.Diagnostics;

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
