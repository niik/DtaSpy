using System;

namespace DtaSpy
{
    public class BizTalkFragment : IDisposable
    {
        public BizTalkFragment()
        {
        }

        public byte[] ImagePart { get; set; }
        public int FragmentNumber { get; set; }

        public void Dispose()
        {
            this.ImagePart = null;
        }

    }
}
