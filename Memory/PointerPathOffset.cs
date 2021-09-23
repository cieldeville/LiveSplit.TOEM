using System;

namespace LiveSplit.TOEM.Memory
{
    public class PointerPathOffset : IPointerPathElement
    {
        private ulong _offset;

        public PointerPathOffset(ulong offset)
        {
            _offset = offset;
        }

        public ulong Follow(MemoryInterface memInterface, ulong address)
        {
            return address + _offset;
        }
    }
}
