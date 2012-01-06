using System;

namespace DtaSpy
{
    public class BizTalkTrackedMessagePartFragment : IDisposable
    {
        private BizTalkTrackingDb bizTalkTrackingDb;
        private int spoolId;

        public BizTalkTrackedMessagePartFragment(BizTalkTrackingDb bizTalkTrackingDb, int spoolId)
        {
            // TODO: Complete member initialization
            this.bizTalkTrackingDb = bizTalkTrackingDb;
            this.spoolId = spoolId;
        }

        public byte[] ImagePart { get; set; }

        public void Dispose()
        {
            this.ImagePart = null;
        }
    }
}
