using LiveSplit.TOEM.Memory;
using System;

namespace LiveSplit.TOEM.Game
{
    public class PlayerController
    {
        public enum PlayerState
        {
            Roaming,
            Sitting,
            PlayAnimation,
            FaceBoard,
            Climbing,
            Unknown
        }

        private PointerPath _playerControllerTypeInfo;
        private PointerPath _playerControllerStaticFields;
        private PointerPath _playerControllerInstancePath;

        // Fields start at 16 bytes (= 0x10UL) offset in PlayerController_o structure
        //
        // struct PlayerController_o
        // {
        //     PlayerController_c* klass;
        //     void* monitor;
        //     PlayerController_Fields fields;
        // };
        //
        // HOWEVER: These 0x10 bytes are already included in the offsets produced by IL2CPPDumper (!)
        private PointerPath _currentStatePath;
        private PointerPath _nextStatePath;
        private PointerPath _roamStatePath;
        private PointerPath _sitStatePath;
        private PointerPath _playAnimationStatePath;
        private PointerPath _faceBoardStatePath;
        private PointerPath _climbingStatePath;


        public bool Ready { get { return _playerController != UIntPtr.Zero; } }
        public PlayerState CurrentState
        {
            get
            {
                if (!Ready) return PlayerState.Unknown;

                if (_currentStateRef.CurrentValue == _roamStateRef.CurrentValue) return PlayerState.Roaming;
                else if (_currentStateRef.CurrentValue == _sitStateRef.CurrentValue) return PlayerState.Sitting;
                else if (_currentStateRef.CurrentValue == _playAnimationStateRef.CurrentValue) return PlayerState.PlayAnimation;
                else if (_currentStateRef.CurrentValue == _faceBoardStateRef.CurrentValue) return PlayerState.FaceBoard;
                else if (_currentStateRef.CurrentValue == _climbingStateRef.CurrentValue) return PlayerState.Climbing;
                return PlayerState.Unknown;
            }
        }

        private UIntPtr _playerController;
        private VariableWatcher<UIntPtr> _currentStateRef;
        private VariableWatcher<UIntPtr> _nextStateRef;
        private VariableWatcher<UIntPtr> _sitStateRef;
        private VariableWatcher<UIntPtr> _roamStateRef;
        private VariableWatcher<UIntPtr> _playAnimationStateRef;
        private VariableWatcher<UIntPtr> _faceBoardStateRef;
        private VariableWatcher<UIntPtr> _climbingStateRef;

        private MemoryInterface _memInterface;

        public PlayerController(MemoryInterface memInterface)
        {
            _memInterface = memInterface;
            Cleanup();
            BuildPaths();
            Initialize();
        }

        private void BuildPaths()
        {
            // script.json @ PlayerController_TypeInfo [Address]
            _playerControllerTypeInfo = PointerPath.Module("GameAssembly.dll", 0x1B9D350UL).Deref().Build();
            // IL2CPP constant
            _playerControllerStaticFields = _playerControllerTypeInfo.Extend().Offset(0xB8UL).Deref().Build();
            // PlayerController.cs @<Instance>k__BackingField [FieldOffset]
            _playerControllerInstancePath = _playerControllerStaticFields.Extend().Offset(0x0UL).Deref().Build();

            // PlayerController.cs @currentState [FieldOffset]
            _currentStatePath = _playerControllerInstancePath.Extend().Offset(0x200UL).Build();
            // PlayerController.cs @nextState [FieldOffset]
            _nextStatePath = _playerControllerInstancePath.Extend().Offset(0x208UL).Build();
            // PlayerController.cs @roamState [FieldOffset]
            _roamStatePath = _playerControllerInstancePath.Extend().Offset(0x218UL).Build();
            // PlayerController.cs @sitState [ToFieldOffsetken]
            _sitStatePath = _playerControllerInstancePath.Extend().Offset(0x220UL).Build();
            // PlayerController.cs @animationState [FieldOffset]
            _playAnimationStatePath = _playerControllerInstancePath.Extend().Offset(0x228UL).Build();
            // PlayerController.cs @faceBoardState [FieldOffset]
            _faceBoardStatePath = _playerControllerInstancePath.Extend().Offset(0x230UL).Build();
            // PlayerController.cs @climbingState [FieldOffset]
            _climbingStatePath = _playerControllerInstancePath.Extend().Offset(0x238UL).Build();
        }

        private void Initialize()
        {
            UIntPtr defaultStateValue = new UIntPtr(0xFFFFFFFFFFFFFFFFUL);

            _playerController = _playerControllerInstancePath.Follow(_memInterface);
            _currentStateRef = _memInterface.WatchMemory<UIntPtr>(_currentStatePath, UIntPtr.Zero);
            _nextStateRef = _memInterface.WatchMemory<UIntPtr>(_nextStatePath, UIntPtr.Zero);
            _sitStateRef = _memInterface.WatchMemory<UIntPtr>(_sitStatePath, defaultStateValue);
            _roamStateRef = _memInterface.WatchMemory<UIntPtr>(_roamStatePath, defaultStateValue);
            _playAnimationStateRef = _memInterface.WatchMemory<UIntPtr>(_playAnimationStatePath, defaultStateValue);
            _faceBoardStateRef = _memInterface.WatchMemory<UIntPtr>(_faceBoardStatePath, defaultStateValue);
            _climbingStateRef = _memInterface.WatchMemory<UIntPtr>(_climbingStatePath, defaultStateValue);
        }

        public void Update()
        {
            // For some unknown reason these states actually do sometimes get re-created
            // or perhaps just moved by some C# / IL2CPP runtime quirk.
            //
            // This means we have to continuously follow the entire pointer path which
            // unfortunately results in quite some calls to WinAPI.ReadMemory
            _currentStatePath.Flush();
            _nextStatePath.Flush();
            _sitStatePath.Flush();
            _roamStatePath.Flush();
            _playAnimationStatePath.Flush();
            _faceBoardStatePath.Flush();
            _climbingStatePath.Flush();

            _currentStateRef.Update();
            _nextStateRef.Update();
            _sitStateRef.Update();
            _roamStateRef.Update();
            _playAnimationStateRef.Update();
            _faceBoardStateRef.Update();
            _climbingStateRef.Update();
        }

        public void Cleanup()
        {
            _playerController = UIntPtr.Zero;
            _currentStateRef = null;
            _sitStateRef = null;
            _roamStateRef = null;
            _playAnimationStateRef = null;
            _faceBoardStateRef = null;
            _climbingStateRef = null;
        }
    }
}
