using LiveSplit.Model;
using LiveSplit.TOEM.Memory;
using System;
using System.Diagnostics;

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
            /// The speedrun is waiting for the player to be sitting on his bed
            /// </summary>
            WaitingForBed,
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
        private int _currentSplit;

        private MemoryInterface _memoryInterface;
        private GameState _gameState;
        private PlayerController _playerController;

        //
        // LiveSplit integration
        //
        private TimerModel _timer;

        public Speedrun(LiveSplitState state)
        {
            _timer = new TimerModel() { CurrentState = state };
            _timer.InitializeGameTime();
            _timer.OnReset += (sender, args) => OnTimerReset();
            _timer.OnPause += (sender, args) => OnPause();
            _timer.OnResume += (sender, args) => OnResume();
            _timer.OnStart += (sender, args) => OnStart();
            _timer.OnSplit += (sender, args) => OnSplit();
            _timer.OnUndoSplit += (sender, args) => OnUndoSplit();
            _timer.OnSkipSplit += (sender, args) => OnSkipSplit();

            UnInitialize();
        }

        public void ReInitialize(MemoryInterface memInterface)
        {
            _memoryInterface = memInterface;
            _gameState = new GameState(memInterface);
            _playerController = new PlayerController(memInterface);
            OnTimerReset();
        }

        public void UnInitialize()
        {
            Reset();
            _gameState = null;
            _playerController = null;
        }

        public void Reset()
        {
            _currentState = State.WaitingForTitleScreen;
            _currentSplit = 0;
            _timer.Reset();
        }

        public void OnTimerReset()
        {
            Debug.WriteLine("OnTimerReset() invoked");
        }

        private void OnPause()
        {
            Debug.WriteLine("OnPause() invoked");
        }

        private void OnResume()
        {
            Debug.WriteLine("OnResume() invoked");
        }

        private void OnStart()
        {
            Debug.WriteLine("OnStart() invoked");
        }

        private void OnSplit()
        {
            Debug.WriteLine("OnSplit() invoked");
        }

        private void OnUndoSplit()
        {
            Debug.WriteLine("OnUndoSplit() invoked");
        }

        private void OnSkipSplit()
        {
            Debug.WriteLine("OnSkipSplit() invoked");
        }

        public void Update()
        {
            // Do not update if paused
            // if (_paused) return;

            _gameState.Update();
            _playerController.Update();

            if (_currentState == State.WaitingForTitleScreen)
            {
                _timer.CurrentState.IsGameTimePaused = true;
                if (_gameState.AtTitleScreen)
                {
                    // Player has found his way into the title screen menu
                    // -> advance to next state
                    Console.WriteLine("Detected player in main menu!");
                    _currentState = State.WaitingForBed;
                }
            }
            else if (_currentState == State.WaitingForBed)
            {
                _timer.CurrentState.IsGameTimePaused = true;
                if (_playerController.CurrentState == PlayerController.PlayerState.Sitting)
                {
                    Console.WriteLine("Detected player on bed!");
                    _currentState = State.ReadyForLaunch;
                }
            }
            else if (_currentState == State.ReadyForLaunch)
            {
                _timer.CurrentState.IsGameTimePaused = true;
                if (!_gameState.AtTitleScreen && _playerController.CurrentState == PlayerController.PlayerState.Roaming)
                {
                    Console.WriteLine("Detected Game Start!");
                    _currentState = State.Playing;
                    _timer.Start();
                }
            }
            else if (_currentState == State.Playing)
            {
                // Handle loading times
                _timer.CurrentState.IsGameTimePaused = _gameState.IsLoadingScene;

                if (_gameState.CurrentRegion > _gameState.PreviousRegion)
                {
                    Console.WriteLine("Detected player advancing to a new region!");
                    _timer.Split();
                }

                if (_gameState.EndScreen == GameState.EndScreenState.Input)
                {
                    Console.WriteLine("Detected end of game!");
                    _timer.Split();
                    _timer.CurrentState.IsGameTimePaused = true;
                }
            }

            if (_currentState >= State.ReadyForLaunch && _gameState.AtTitleScreen)
            {
                Reset();
            }
        }
    }
}
