using LiveSplit.Model;
using LiveSplit.TOEM.Memory;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private Thread updateLoop;
        private bool updateLoopRunning;

        private ProcessCapture processCapture;
        private MemoryInterface memoryInterface;

        // Capture game variables
        private MemoryWatcher currentRegion = null;

        public TOEMComponent(LiveSplitState state, bool shown = false)
        {
            processCapture = new ProcessCapture("TOEM");
            processCapture.ProcessHooked += InitializeHook;
            processCapture.ProcessLost += DisposeHook;

            updateLoopRunning = true;
            updateLoop = new Thread(UpdateLoopMain);
            updateLoop.IsBackground = true;
            updateLoop.Start();
        }

        //
        // Component Logic
        //
        private void UpdateLoopMain()
        {
            while (updateLoopRunning)
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
            if (!processCapture.EnsureProcessHook()) return;
            UpdateHook();
        }

        private void InitializeHook(object sender, EventArgs args)
        {
            memoryInterface = new MemoryInterface(processCapture.HookedProcess);

            // Initialize game variables
            MemoryScanner scanner = new MemoryScanner(memoryInterface);
            List<MemoryScanner.Match> matches = scanner.Find(
                Signature.From("41 FF D3 48 8B CE 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 89 08 48 B8"),
                m => (m.Protect & WinAPI.PAGE_EXECUTE_READWRITE) != 0 && (m.Protect & WinAPI.PAGE_GUARD) == 0,
                true
            );

            if (matches.Count > 0)
            {
                MemoryScanner.Match match = matches.First();
                currentRegion = new MemoryWatcher(memoryInterface, match.GetUIntPtr(8), 4);
            }
            else
            {
                processCapture.Unhook();
            }
        }

        private void UpdateHook()
        {
            try
            {
                currentRegion.Update();
                Console.WriteLine("Current Region: " + currentRegion.Get<int>().ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to update game variables");
            }
        }

        private void DisposeHook(object sender, EventArgs args)
        {
            currentRegion = null;
            memoryInterface = null;
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
