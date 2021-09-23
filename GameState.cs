using LiveSplit.TOEM.Memory;

namespace LiveSplit.TOEM
{
    public class GameState
    {
        //
        // Addresses
        //
        private static readonly IResolvableAddress _currentRegionAddr = new SignatureResolvableAddress(Signature.From("41 FF D3 48 8B CE 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 89 08 48 B8"), 8, true);

        //
        // Meta
        //
        private readonly MemoryInterface _memInterface;

        //
        // State Variables
        //
        public VariableWatcher<int> CurrentRegion;

        public GameState(MemoryInterface memInterface)
        {
            _memInterface = memInterface;
            Initialize();
        }

        private void Initialize()
        {
            CurrentRegion = _memInterface.WatchMemory<int>(_currentRegionAddr, -1);
        }

        public void Update()
        {
            CurrentRegion.Update();
        }
    }
}
