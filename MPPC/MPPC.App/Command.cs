using System;
using System.Collections.Generic;
using System.Text;

namespace MPPC.App
{
    public enum Command : byte
    {
        Delay = 0x02,
        Start = 0x03,
        Finish = 0x04,
        Read = 0x05,
        Clear = 0x06
    }
}
