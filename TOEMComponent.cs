using LiveSplit.Model;
using LiveSplit.TOEM.Game;
using LiveSplit.TOEM.Memory;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.TOEM
{
    public class TOEMComponent : IComponent
    {
        public string ComponentName { get { return "TOEM Autosplitter"; } }
        public float HorizontalWidth { get { return 0; } }
        public float MinimumHeight { get { return 0; } }
        public float VerticalHeight { get { return 0; } }
        public float MinimumWidth { get { return 0; } }
        public float PaddingTop { get { return 0; } }
        public float PaddingBottom { get { return 0; } }
        public float PaddingLeft { get { return 0; } }
        public float PaddingRight { get { return 0; } }
        public IDictionary<string, Action> ContextMenuControls { get { return null; } }

        private Thread _updateLoop;
        private bool _updateLoopRunning;

        private ProcessCapture _processCapture;
        private MemoryInterface _memoryInterface;
        private Speedrun _speedrun;

        public TOEMComponent(LiveSplitState state, bool shown = false)
        {
            _processCapture = new ProcessCapture("TOEM");
            _processCapture.ProcessHooked += InitializeHook;
            _processCapture.ProcessLost += DisposeHook;

            Debug.WriteLine("LiveSplitState = " + state);
            _speedrun = new Speedrun(state);

            _updateLoopRunning = true;
            _updateLoop = new Thread(UpdateLoopMain);
            _updateLoop.IsBackground = true;
            _updateLoop.Start();
            
        }

        //
        // Component Logic
        //
        private void UpdateLoopMain()
        {
            while (_updateLoopRunning)
            {
                try
                {
                    PerformUpdate();
                }
                catch (Exception ex)
                {
                    // TODO: Perform logging
                }
                Thread.Sleep(5);
            }
        }

        private void PerformUpdate()
        {
            // Attempt to hook game, if not already hooked
            if (!_processCapture.EnsureProcessHook()) return;
            UpdateHook();
        }

        private void InitializeHook(object sender, EventArgs args)
        {
            try
            {
                _memoryInterface = new MemoryInterface(_processCapture.HookedProcess);
                _speedrun.ReInitialize(_memoryInterface);
            }
            catch (Exception)
            {
                // TODO: Log exception
                _processCapture.Unhook();
            }
        }

        private void UpdateHook()
        {
            try
            {
                _speedrun.Update();
            }
            catch (Exception ex)
            {
                // TODO: Log exception
                Console.WriteLine(ex);
            }
        }

        private void DisposeHook(object sender, EventArgs args)
        {
            _speedrun.UnInitialize();
            _memoryInterface = null;
        }

        //
        // IComponent implementation
        //
        public void Dispose()
        {
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return document.CreateElement("Settings");
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            return null;
        }

        public void SetSettings(XmlNode settings)
        {
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
        }





    }
}
