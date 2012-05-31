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
