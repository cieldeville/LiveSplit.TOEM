using LiveSplit.TOEM.Game;
using LiveSplit.TOEM.Memory;
using System;
using System.Diagnostics;

namespace LiveSplit.TOEM.Game
{
    public class GameState
    {
        //
        // Addresses
        //
        private static readonly PointerPath _gameManagerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1E3FE88UL).Deref().Build();
        private static readonly PointerPath _gameManagerStaticFields = _gameManagerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
        private static readonly PointerPath _atTitleScreenPath = _gameManagerStaticFields.Extend().Offset(0x30UL).Build();
        private static readonly PointerPath _currentRegionPath = _gameManagerStaticFields.Extend().Offset(0x40UL).Build();

        private static readonly PointerPath _sceneTransitionControllerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1E32D28UL).Deref().Build();
        private static readonly PointerPath _sceneTransitionControllerStaticFields = _sceneTransitionControllerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
        private static readonly PointerPath _isLoadingScenePath = _sceneTransitionControllerStaticFields.Extend().Offset(0x29UL).Build();

        //
        // Meta
        //
        private readonly MemoryInterface _memInterface;

        //
        // State Variables
        //
        public VariableWatcher<bool> AtTitleScreen;
        public VariableWatcher<int> CurrentRegion;
        public VariableWatcher<bool> IsLoadingScene;

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
                CurrentRegion = _memInterface.WatchMemory<int>(_currentRegionPath, -1);
                IsLoadingScene = _memInterface.WatchMemory<bool>(_isLoadingScenePath, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Update()
        {
            AtTitleScreen?.Update();
            CurrentRegion?.Update();
            IsLoadingScene?.Update();

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("AtTitleScreen \t\t= " + AtTitleScreen);
            Console.WriteLine("CurrentRegion \t\t= " + CurrentRegion);
            Console.WriteLine("IsLoadingScene \t\t= " + IsLoadingScene);
        }
    }
}
