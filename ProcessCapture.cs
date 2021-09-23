using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM
{
    public class ProcessCapture
    {
        /// <summary>
        /// The name of the process to hook into if running.
        /// </summary>
        public string ProcessName { get; }
        /// <summary>
        /// A reference to the currently hooked process, if any, or null.
        /// </summary>
        public Process HookedProcess { get { return _process; } }
        /// <summary>
        /// Whether or not the memory interface is currently hooked. Will not trigger a hook attempt.
        /// </summary>
        public bool IsHooked { get { return CheckHook(); } }
        /// <summary>
        /// Number of seconds in between hooking attempts
        /// </summary>
        public int HookAttemptDelay { get; set; } = 10;

        /// <summary>
        /// Event raised whenever a new process has been hooked.
        /// </summary>
        public event EventHandler ProcessHooked;
        /// <summary>
        /// Event raised whenever a previously hooked process has been detected as lost (exited).
        /// </summary>
        public event EventHandler ProcessLost;

        private Process _process = null;
        private bool _isHooked = false;
        private DateTime _lastHookAttempt;

        public ProcessCapture(string processName)
        {
            ProcessName = processName;
        }

        public bool CheckHook()
        {
            // Check if process has exited since last check
            if (_process != null)
            {
                if (_process.HasExited)
                {
                    Unhook();
                }
                else
                {
                    _isHooked = true;
                }
            }
            return _isHooked;
        }

        public bool EnsureProcessHook()
        {
            CheckHook();

            // If not currently hook, check if hook attempt should be made
            if (!_isHooked && _lastHookAttempt.AddSeconds(HookAttemptDelay) <= DateTime.Now)
            {
                _lastHookAttempt = DateTime.Now;

                Process[] processes = Process.GetProcessesByName(ProcessName);
                if (processes != null && processes.Length > 0)
                {
                    _process = processes[0];
                    _isHooked = true;
                    OnProcessHooked(new EventArgs());
                }
                else
                {
                    _process = null;
                    _isHooked = false;
                }
            }

            return _isHooked;
        }

        public void Unhook()
        {
            OnProcessLost(new EventArgs());
            _isHooked = false;
            _process = null;
        }

        public void Dispose()
        {
            if (_process != null)
            {
                _process.Dispose();
                _process = null;
            }
        }


        protected virtual void OnProcessHooked(EventArgs e)
        {
            ProcessHooked?.Invoke(this, e);
        }

        protected virtual void OnProcessLost(EventArgs e)
        {
            ProcessLost?.Invoke(this, e);
        }
    }
}
