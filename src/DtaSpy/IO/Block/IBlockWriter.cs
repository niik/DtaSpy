using System;

namespace DtaSpy
{
    public interface IBlockWriter: IDisposable
    {
        void WriteBlock(FragmentBlock block);
        void WriteBlock(byte[] buffer, int offset, int count, bool compressed, int uncompressedLength);
        void Flush();
        void Close();
    }
}
