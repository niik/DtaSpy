using System;
using System.Collections.Generic;

namespace DtaSpy
{
    public class BizTalkTrackedMessage : IDisposable
    {
        private BizTalkTrackingDb db;
        
        public int SpoolId { get; set; }
        public Guid MessageId { get; set; }
        public Guid BodyPartId { get; set; }
        public int PartCount { get; set; }

        private BizTalkTrackedMessagePart _bodyPart;
        public BizTalkTrackedMessagePart BodyPart
        {
            get
            {
                if (_bodyPart == null)
                {
                    if (cachedParts != null)
                    {
                        foreach (var part in cachedParts)
                        {
                            if (part.PartId == this.BodyPartId)
                            {
                                _bodyPart = part;
                                return part;
                            }
                        }
                    }

                    _bodyPart = this.db.LoadTrackedPart(this.MessageId, this.BodyPartId, this.SpoolId);
                }

                return _bodyPart;
            }
        }

        private List<BizTalkTrackedMessagePart> cachedParts;

        public IEnumerable<BizTalkTrackedMessagePart> Parts
        {
            get
            {
                if (cachedParts == null)
                    cachedParts = new List<BizTalkTrackedMessagePart>(this.db.LoadTrackedParts(this.MessageId, this.SpoolId));

                return cachedParts;
            }
        }

        public BizTalkTrackedMessage(BizTalkTrackingDb db, int spoolId)
        {
            if (db == null)
                throw new ArgumentNullException("db");

            this.db = db;
            this.SpoolId = spoolId;
        }

        public void Dispose()
        {
            if (this._bodyPart != null)
            {
                this._bodyPart.Dispose();
                this._bodyPart = null;
            }
        }
    }
}
