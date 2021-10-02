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

        private static readonly PointerPath _playerControllerInstancePath = PointerPath.Signature(Signature.From("F3 0F 10 01 F3 0F 5A C0 F2 0F 5A E8 F3 0F 11 A8 ?? ?? ?? ?? 48 B8 ?? ?? ?? ?? ?? ?? ?? ??"), 22, true).Deref().Build();
        private static readonly PointerPath _currentStatePath = _playerControllerInstancePath.Extend().Offset(0x1C0ul).Build();
        private static readonly PointerPath _roamStatePath = _playerControllerInstancePath.Extend().Offset(0x1D0ul).Build();
        private static readonly PointerPath _sitStatePath = _playerControllerInstancePath.Extend().Offset(0x1D8ul).Build();
        private static readonly PointerPath _playAnimationStatePath = _playerControllerInstancePath.Extend().Offset(0x1E0ul).Build();
        private static readonly PointerPath _faceBoardStatePath = _playerControllerInstancePath.Extend().Offset(0x1E8ul).Build();
        private static readonly PointerPath _climbingStatePath = _playerControllerInstancePath.Extend().Offset(0x1F0ul).Build();


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
        private VariableWatcher<UIntPtr> _sitStateRef;
        private VariableWatcher<UIntPtr> _roamStateRef;
        private VariableWatcher<UIntPtr> _playAnimationStateRef;
        private VariableWatcher<UIntPtr> _faceBoardStateRef;
        private VariableWatcher<UIntPtr> _climbingStateRef;

        private bool _wasRoamingBefore;

        public PlayerController()
        {
            Cleanup();
        }

        public bool Initialize(MemoryInterface memInterface)
        {
            try
            {
                UIntPtr defaultStateValue = new UIntPtr(0xFFFFFFFFFFFFFFFFUL);

                _playerController = _playerControllerInstancePath.Follow(memInterface);
                _currentStateRef = memInterface.WatchMemory<UIntPtr>(_currentStatePath, UIntPtr.Zero);
                _sitStateRef = memInterface.WatchMemory<UIntPtr>(_sitStatePath, defaultStateValue);
                _roamStateRef = memInterface.WatchMemory<UIntPtr>(_roamStatePath, defaultStateValue);
                _playAnimationStateRef = memInterface.WatchMemory<UIntPtr>(_playAnimationStatePath, defaultStateValue);
                _faceBoardStateRef = memInterface.WatchMemory<UIntPtr>(_faceBoardStatePath, defaultStateValue);
                _climbingStateRef = memInterface.WatchMemory<UIntPtr>(_climbingStatePath, defaultStateValue);

                _wasRoamingBefore = false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Update()
        {
            _currentStateRef?.Update();
            _sitStateRef?.Update();
            _roamStateRef?.Update();
            _playAnimationStateRef?.Update();
            _faceBoardStateRef?.Update();
            _climbingStateRef?.Update();

            if (!_wasRoamingBefore && CurrentState == PlayerState.Roaming)
            {
                // Player stood up for the first time
                Console.WriteLine("Starting Timer!");
                _wasRoamingBefore = true;
            }
        }

        public void Cleanup()
        {
            _playerController = UIntPtr.Zero;
            _currentStateRef = null;
            _sitStateRef = null;
            _roamStateRef = null;
        }
    }
}
