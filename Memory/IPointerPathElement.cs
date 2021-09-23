namespace LiveSplit.TOEM.Memory
{
    public interface IPointerPathElement
    {
        ulong Follow(MemoryInterface memInterface, ulong address);
    }
}
