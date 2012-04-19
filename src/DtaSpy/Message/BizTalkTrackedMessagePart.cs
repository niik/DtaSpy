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

        private List<BizTalkFragment> _fragments;

        public List<BizTalkFragment> Fragments
        {
            get
            {
                if (_fragments == null)
                {
                    _fragments = new List<BizTalkFragment>();

                    foreach (var fragment in LoadFragments())
                        _fragments.Add(fragment);
                }

                return _fragments;
            }
        }

        private IEnumerable<BizTalkFragment> LoadFragments()
        {
            int startFragment = 1;

            if (ImagePart != null)
            {
                // We already have the first fragment
                yield return new BizTalkFragment { ImagePart = this.ImagePart };

                // Don't load fragment #1 from db, we already have it.
                startFragment = 2;
            }

            for (int i = startFragment; i <= FragmentCount; i++)
            {
                var fragment = this.db.LoadTrackedPartFragment(this.PartId, i, this.spoolId);

                if (fragment == null)
                {
                    var io = new InvalidCastException("Could not find fragment " + i + " of " + FragmentCount + " for message part");
                    io.Data["partId"] = this.PartId;

                    throw io;
                }

                yield return fragment;
            }
        }

        /// <summary>
        /// Writes the decoded contents of all fragments to the given stream while caching
        /// loaded fragments for future access. Not caching improves memory usage and may improve
        /// performance so if you're never going to access the fragments after this call, please use
        /// the overload which allows you to specify whether to cache or not.
        /// </summary>
        public int WriteTo(Stream s)
        {
            return WriteTo(s, true);
        }

        /// <summary>
        /// Writes the decoded contents of all fragments to the given stream, optionally caching
        /// loaded fragments for future access. Not caching improves memory usage and may improve
        /// performance. Don't cache if you're never going to access the fragments after. Caching
        /// has no effect if fragments already have been loaded for this message part.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="cacheFragments">
        /// if set to <c>true</c> cache fragments for future access 
        /// via the Fragments property.
        /// </param>
        public int WriteTo(Stream s, bool cacheFragments)
        {
            int total = 0;

            IEnumerable<BizTalkFragment> fragments;

            if (!cacheFragments || this._fragments != null)
                fragments = this.Fragments;
            else
                fragments = LoadFragments();

            byte[] buf = new byte[8192];
            int c;

            foreach (var fragment in fragments)
            {
                using (var ds = new MemoryStream(fragment.ImagePart))
                using (var bz = new BizTalkFragmentStream(ds, CompressionMode.Decompress))
                {
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
