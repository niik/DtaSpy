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
using System.IO;

namespace DtaSpy
{
    public class BizTalkTrackedMessagePart
    {
        private BizTalkTrackingDb db;
        private int spoolId;

        public string PartName { get; set; }
        public Guid PartId { get; set; }
        public int FragmentCount { get; set; }
        public Guid OldPartId { get; set; }

        private List<BizTalkFragment> _fragments;

        public List<BizTalkFragment> Fragments
        {
            get
            {
                if (_fragments == null && this.db != null)
                {
                    _fragments = new List<BizTalkFragment>();

                    foreach (var fragment in LoadFragments())
                        _fragments.Add(fragment);
                }

                return _fragments;
            }
        }

        public BizTalkPropertyBag Properties { get; set; }


        public BizTalkTrackedMessagePart()
        {
            this._fragments = new List<BizTalkFragment>();
            this.Properties = new BizTalkPropertyBag();
        }

        public BizTalkTrackedMessagePart(BizTalkTrackingDb db, int spoolId)
        {
            this.db = db;
            this.spoolId = spoolId;

            this._fragments = null;
        }

        internal BizTalkTrackedMessagePart(BizTalkTrackingDb db, int spoolId, BizTalkFragment initialFragment): this(db, spoolId)
        {
            this.initialFragment = initialFragment;
        }

        private IEnumerable<BizTalkFragment> LoadFragments()
        {
            int startFragment = 1;

            if (initialFragment != null)
            {
                // We already have the first fragment
                yield return initialFragment;

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
                using (var bz = new BizTalkMessagePartStream(ds, StreamMode.Read))
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

        public BizTalkFragment initialFragment { get; set; }
    }
}
