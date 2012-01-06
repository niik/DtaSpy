using System;
using System.Collections.Generic;

namespace DtaSpy
{
    public class BizTalkTrackedMessageContext : IDisposable
    {
        private BizTalkTrackingDb db;
        public int SpoolId { get; private set; }

        public List<BizTalkTrackedMessageContextProperty> Properties { get; set; }

        public BizTalkTrackedMessageContext(BizTalkTrackingDb db, int spoolId, List<BizTalkTrackedMessageContextProperty> properties)
        {
            this.db = db;
            this.SpoolId = spoolId;
            this.Properties = properties;
        }

        public BizTalkTrackedMessageContext(BizTalkTrackingDb db, int spoolId)
            : this(db, spoolId, new List<BizTalkTrackedMessageContextProperty>())
        {
        }

        public bool TryGetProperty(string ns, string name, out object value)
        {
            foreach (var property in this.Properties)
            {
                if (property.Namespace == ns && property.Name == name)
                {
                    value = property.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
