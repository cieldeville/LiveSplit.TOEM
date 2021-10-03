using LiveSplit.TOEM.Game;
using LiveSplit.TOEM.Memory;
using System;
using System.Diagnostics;

namespace LiveSplit.TOEM.Game
{
    public class GameState
    {

        public enum Region
        {
            None = 0,
            Generic = 1,
            Home = 2,
            Forest = 3,
            Harbor = 4,
            City = 5,
            Mountain = 6,
            MountainTop = 7,
            Unknown
        }


        public enum EndScreenState
        {
            FadeIn = 0,
            Wait = 1,
            Input = 2,
            Done = 3,
            Unknown
        }

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

        private PointerPath _menuManagerTypeInfo;
        private PointerPath _menuManagerStaticFields;
        private PointerPath _menuManagerInstancePath;
        private PointerPath _theEndScreenInstancePath;
        private PointerPath _theEndScreenStatePath;


        //
        // Meta
        //
        private readonly MemoryInterface _memInterface;

        //
        // State Variables
        //
        private VariableWatcher<bool> _atTitleScreen;
        private VariableWatcher<int> _currentRegion;
        private VariableWatcher<bool> _isLoadingScene;
        private VariableWatcher<int> _endScreenState;

        public bool AtTitleScreen { get { return _atTitleScreen.CurrentValue; } }
        public Region CurrentRegion { get { return Enum.IsDefined(typeof(Region), _currentRegion.CurrentValue) ? (Region)_currentRegion.CurrentValue : Region.Unknown; } }
        public Region PreviousRegion { get { return Enum.IsDefined(typeof(Region), _currentRegion.OldValue) ? (Region)_currentRegion.OldValue : Region.Unknown; } }
        public bool IsLoadingScene { get { return _isLoadingScene.CurrentValue; } }
        public EndScreenState EndScreen { get { return Enum.IsDefined(typeof(EndScreenState), _endScreenState.CurrentValue) ? (EndScreenState)_endScreenState.CurrentValue : EndScreenState.Unknown; } }

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

            _menuManagerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1E41780UL).Deref().Build();
            _menuManagerStaticFields = _menuManagerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            _menuManagerInstancePath = _menuManagerStaticFields.Extend().Deref().Build();
            _theEndScreenInstancePath = _menuManagerInstancePath.Extend().Offset(0xD8UL).Deref().Build();
            _theEndScreenStatePath = _theEndScreenInstancePath.Extend().Offset(0x40UL).Build();
        }

        private void Initialize()
        {
            try
            {
                _atTitleScreenPath.Flush(true);
                _currentRegionPath.Flush(true);
                _isLoadingScenePath.Flush(true);
                _theEndScreenStatePath.Flush(true);

                _atTitleScreen = _memInterface.WatchMemory<bool>(_atTitleScreenPath, false);
                _currentRegion = _memInterface.WatchMemory<int>(_currentRegionPath, -1);
                _isLoadingScene = _memInterface.WatchMemory<bool>(_isLoadingScenePath, false);
                _endScreenState = _memInterface.WatchMemory<int>(_theEndScreenStatePath, -1);
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
            _theEndScreenStatePath.Flush();

            _atTitleScreen.Update();
            _currentRegion.Update();
            _isLoadingScene.Update();
            _endScreenState.Update();

            //Console.WriteLine("------------------------------------------------------");
            //Console.WriteLine("AtTitleScreen \t\t= " + AtTitleScreen);
            //Console.WriteLine("CurrentRegion \t\t= " + CurrentRegion);
            //Console.WriteLine("IsLoadingScene \t\t= " + IsLoadingScene);
        }
    }
}
