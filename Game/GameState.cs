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
            // script.json @ GameManager_TypeInfo [Address]
            _gameManagerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1BBA520UL).Deref().Build();
            // IL2CPP constant
            _gameManagerStaticFields = _gameManagerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            // GameManager.cs @<AtTitleScreen>k__BackingField [FieldOffset]
            _atTitleScreenPath = _gameManagerStaticFields.Extend().Offset(0x38UL).Build();
            // GameManager.cs @<CurrentRegion>k__BackingField [FieldOffset]
            _currentRegionPath = _gameManagerStaticFields.Extend().Offset(0x48UL).Build();


            // script.json @ SceneTransitionController_TypeInfo [Address]
            _sceneTransitionControllerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1BB22C0UL).Deref().Build();
            // IL2CPP constant
            _sceneTransitionControllerStaticFields = _sceneTransitionControllerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            // SceneTransitionController.cs @ <IsLoadingScene>k__BackingField
            _isLoadingScenePath = _sceneTransitionControllerStaticFields.Extend().Offset(0x29UL).Build();


            // script.json @ MenuManager_TypeInfo [Address]
            _menuManagerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1BBBC60UL).Deref().Build();
            // IL2CPP constant
            _menuManagerStaticFields = _menuManagerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            // MenuManager.cs @<Instance>k__BackingField [FieldOffset]
            _menuManagerInstancePath = _menuManagerStaticFields.Extend().Offset(0x0UL).Deref().Build();
            // MenuManager.cs @menu_TheEnd [FieldOffset]
            _theEndScreenInstancePath = _menuManagerInstancePath.Extend().Offset(0xD8UL).Deref().Build();

            // TheEndScreen.cs @myState [FieldOffset]
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
