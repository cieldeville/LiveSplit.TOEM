using System;

namespace LiveSplit.TOEM.Memory
{
    [Flags]
    public enum MemoryAccessFlags : int
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }
}
