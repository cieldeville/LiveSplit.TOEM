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
        private PointerPath _gameManagerTypeInfo;
        private PointerPath _gameManagerStaticFields;
        private PointerPath _atTitleScreenPath;
        private PointerPath _currentRegionPath;

        private PointerPath _sceneTransitionControllerTypeInfo;
        private PointerPath _sceneTransitionControllerStaticFields;
        private PointerPath _isLoadingScenePath;

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
            BuildPaths();
            Initialize();
        }

        private void BuildPaths()
        {
            _gameManagerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1E3FE88UL).Deref().Build();
            _gameManagerStaticFields = _gameManagerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            _atTitleScreenPath = _gameManagerStaticFields.Extend().Offset(0x30UL).Build();
            _currentRegionPath = _gameManagerStaticFields.Extend().Offset(0x40UL).Build();

            _sceneTransitionControllerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1E32D28UL).Deref().Build();
            _sceneTransitionControllerStaticFields = _sceneTransitionControllerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            _isLoadingScenePath = _sceneTransitionControllerStaticFields.Extend().Offset(0x29UL).Build();
        }

        private void Initialize()
        {
            try
            {
                _atTitleScreenPath.Flush(true);
                _currentRegionPath.Flush(true);
                _isLoadingScenePath.Flush(true);

                AtTitleScreen = _memInterface.WatchMemory<bool>(_atTitleScreenPath, false);
                CurrentRegion = _memInterface.WatchMemory<int>(_currentRegionPath, -1);
                IsLoadingScene = _memInterface.WatchMemory<bool>(_isLoadingScenePath, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        public void Update()
        {
            _atTitleScreenPath.Flush();
            _currentRegionPath.Flush();
            _isLoadingScenePath.Flush();

            AtTitleScreen.Update();
            CurrentRegion.Update();
            IsLoadingScene.Update();

            //Console.WriteLine("------------------------------------------------------");
            //Console.WriteLine("AtTitleScreen \t\t= " + AtTitleScreen);
            //Console.WriteLine("CurrentRegion \t\t= " + CurrentRegion);
            //Console.WriteLine("IsLoadingScene \t\t= " + IsLoadingScene);
        }
    }
}
