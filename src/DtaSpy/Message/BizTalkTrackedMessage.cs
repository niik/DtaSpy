using System;
using System.Collections.Generic;

namespace DtaSpy
{
    public class BizTalkTrackedMessage
    {
        private BizTalkTrackingDb db;

        public int SpoolId { get; set; }
        public Guid MessageId { get; set; }
        public Guid BodyPartId { get; set; }
        public int PartCount { get; set; }

        public BizTalkTrackedMessagePart BodyPart
        {
            get
            {
                if (this.Parts == null)
                    return null;

                foreach (var part in this.Parts)
                {
                    if (part.PartId == this.BodyPartId)
                        return part;
                }

                return null;
            }
            set
            {

                foreach (var part in this.Parts)
                {
                    if (part.PartId == value.PartId)
                    {
                        this.BodyPartId = value.PartId;
                        return;
                    }
                }

                this.Parts.Add(value);
                this.BodyPartId = value.PartId;
            }
        }

        private List<BizTalkTrackedMessagePart> _parts;

        public List<BizTalkTrackedMessagePart> Parts
        {
            get
            {
                if (_parts == null && this.db != null)
                    _parts = new List<BizTalkTrackedMessagePart>(this.db.LoadTrackedParts(this.MessageId, this.SpoolId));

                if (_parts == null)
                    _parts = new List<BizTalkTrackedMessagePart>();

                return _parts;
            }
        }

        public BizTalkTrackedMessage()
        {
            this._parts = new List<BizTalkTrackedMessagePart>();
        }

        public BizTalkTrackedMessage(BizTalkTrackingDb db, int spoolId)
        {
            if (db == null)
                throw new ArgumentNullException("db");

            this.db = db;
            this.SpoolId = spoolId;
            
            this._parts = null;
        }
    }
}
