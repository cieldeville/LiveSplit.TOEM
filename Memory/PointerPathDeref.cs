using System;

namespace LiveSplit.TOEM.Memory
{
    public class PointerPathDeref : IPointerPathElement
    {
        public ulong Follow(MemoryInterface memInterface, ulong address)
        {
            try
            {
                byte[] buffer = new byte[UIntPtr.Size];
                memInterface.ReadMemory(new UIntPtr(address), buffer, (ulong) UIntPtr.Size, out ulong numberOfBytesRead);
                if (numberOfBytesRead != (ulong)UIntPtr.Size) throw new Exception("Failed to fully dereference pointer");

                UIntPtr deref;
                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        deref = *(UIntPtr*)p;
                    }
                }
                return deref.ToUInt64();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to dereference pointer", ex);
            }
        }
    }
}
