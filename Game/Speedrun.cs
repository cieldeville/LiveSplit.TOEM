using LiveSplit.Model;
using LiveSplit.TOEM.Memory;

namespace LiveSplit.TOEM.Game
{
    public class Speedrun
    {
        public enum State
        {
            /// <summary>
            /// The speedrun is not currently bound to the game
            /// </summary>
            Uninitialized,
            /// <summary>
            /// The speedrun is waiting for the player to visit the title screen to get ready for launch
            /// </summary>
            WaitingForTitleScreen,
            /// <summary>
            /// The speedrun was in the main menu and may now start
            /// </summary>
            ReadyForLaunch,
            /// <summary>
            /// The speedrun is currently ongoing
            /// </summary>
            Playing,
            /// <summary>
            /// The speedrun has finished
            /// </summary>
            Finished
        }


        private State _currentState;
        private bool _paused;
        private int _currentSplit;

        private MemoryInterface _memoryInterface;
        private GameState _gameState;
        private PlayerController _playerController;

        //
        // LiveSplit integration
        //
        private LiveSplitState _liveSplit;

        public Speedrun(LiveSplitState state)
        {
            UnInitialize();

            if (state != null)
            {
                _liveSplit.OnReset += (sender, args) => Reset();
                _liveSplit.OnPause += (sender, args) => Pause();
                _liveSplit.OnResume += (sender, args) => Resume();
                _liveSplit.OnStart += (sender, args) => Start();
                _liveSplit.OnSplit += (sender, args) => Split();
                _liveSplit.OnUndoSplit += (sender, args) => UndoSplit();
                _liveSplit.OnSkipSplit += (sender, args) => SkipSplit();
            }
        }

        public void ReInitialize(MemoryInterface memInterface)
        {
            _memoryInterface = memInterface;
            _gameState = new GameState(memInterface);
            Reset();
        }

        public void UnInitialize()
        {
            Reset();
            _gameState = null;
            _playerController = null;
            _currentState = State.Uninitialized;
        }

        public void Reset()
        {
            _currentState = State.WaitingForTitleScreen;
            _paused = true;
            _currentSplit = 0;
        }

        private void Pause()
        {
            _paused = true;
        }

        private void Resume()
        {
            _paused = false;
        }

        private void Start()
        {
            Reset();
            _paused = false;
        }

        private void Split()
        {
            ++_currentSplit;
        }

        private void UndoSplit()
        {
            --_currentSplit;
        }

        private void SkipSplit()
        {
            ++_currentSplit;
        }

        public void Update()
        {
            // Do not update if paused
            // if (_paused) return;

            _gameState.Update();


            if (_currentState == State.WaitingForTitleScreen)
            {
                if (_gameState.AtTitleScreen.CurrentValue)
                {
                    // Player has found his way into the title screen menu
                    // -> advance to next state
                    _currentState = State.ReadyForLaunch;
                }
            }
            else if (_currentState == State.ReadyForLaunch)
            {
                if (!_gameState.AtTitleScreen.CurrentValue)
                {
                    
                }
            }
        }

        private void Launch()
        {
            _currentState = State.Playing;
            _currentSplit = 0;

            // Launch LiveSplit Timer
            //_liveSplit.Start();
        }
    }
}
