using System;
using System.Collections.Generic;
using System.Text;

namespace DtaSpy
{
    /// <summary>
    /// This is some internal BizTalk thingy which we shouldn't care about (http://msdn.microsoft.com/en-us/library/microsoft.biztalk.message.interop.contextpropertytype(v=bts.10).aspx)
    /// </summary>
    [Flags]
    public enum PropertyType
    {
        PropPredicate = 4,
        PropPromoted = 2,
        PropWasPromoted = 0x10,
        PropWritten = 1
    }
}
