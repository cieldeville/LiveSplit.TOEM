using LiveSplit.TOEM.Game;
using LiveSplit.TOEM.Memory;
using System;
using System.Diagnostics;

namespace LiveSplit.TOEM.Gane
{
    public class GameState
    {
        //
        // Addresses
        //
        private static readonly PointerPath _atTitleScreenPath = PointerPath.Signature(Signature.From("41 FF D3 48 8B C8 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 40 88 08 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 0F B6 00 85 C0"), 8, true).Build();
        private static readonly PointerPath _currentRegionAddr = PointerPath.Signature(Signature.From("41 FF D3 48 8B CE 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 89 08 48 B8"), 8, true).Build();

        //
        // Meta
        //
        private readonly MemoryInterface _memInterface;

        //
        // State Variables
        //
        public VariableWatcher<bool> AtTitleScreen;
        public VariableWatcher<int> CurrentRegion;
        public PlayerController PlayerController;

        public GameState(MemoryInterface memInterface)
        {
            _memInterface = memInterface;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                AtTitleScreen = _memInterface.WatchMemory<bool>(_atTitleScreenPath, false);
                PlayerController = new PlayerController();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Update()
        {
            AtTitleScreen.Update();
            if (!AtTitleScreen.CurrentValue && !PlayerController.Ready)
            {
                PlayerController.Initialize(_memInterface);
            }
            PlayerController.Update();
        }
    }
}
