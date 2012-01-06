using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace DtaSpy
{
    public class BizTalkTrackedMessagePart : IDisposable
    {
        private BizTalkTrackingDb db;
        private int spoolId;

        public BizTalkTrackedMessagePart(BizTalkTrackingDb db, int spoolId)
        {
            this.db = db;
            this.spoolId = spoolId;
        }

        public string PartName { get; set; }

        public Guid PartId { get; set; }

        public int FragmentCount { get; set; }

        public byte[] ImagePart { get; set; }
        public byte[] ImagePropBag { get; set; }

        public Guid OldPartId { get; set; }

        private List<BizTalkTrackedMessagePartFragment> _fragments;

        public List<BizTalkTrackedMessagePartFragment> Fragments
        {
            get
            {
                if (_fragments == null)
                {
                    var fragments = new List<BizTalkTrackedMessagePartFragment>();

                    for (int i = 1; i <= FragmentCount; i++)
                    {
                        var fragment = this.db.LoadTrackedPartFragment(this.PartId, i, this.spoolId);

                        if (fragment == null)
                            break;

                        fragments.Add(fragment);
                    }

                    _fragments = fragments;
                }

                return _fragments;
            }
        }

        public int SaveTo(Stream s)
        {
            int total = 0;

            // TODO: If fragmentcount == 1 then we can probably skip the iteration and go 
            // straight to our own ImagePart.
            foreach (var fragment in this.Fragments)
            {
                using (var ds = new MemoryStream(fragment.ImagePart))
                using (var bz = new BizTalkFragmentStream(ds, CompressionMode.Decompress))
                {
                    byte[] buf = new byte[8192];
                    int c;

                    while ((c = bz.Read(buf, 0, buf.Length)) > 0)
                    {
                        s.Write(buf, 0, c);
                        total += c;
                    }
                }
            }

            return total;
        }

        public void Dispose()
        {
            if (this._fragments != null)
            {
                foreach (var fragment in this._fragments)
                    fragment.Dispose();

                this._fragments = null;
            }
        }
    }
}
