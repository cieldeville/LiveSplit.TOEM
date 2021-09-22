using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM
{
    public class TOEMFactory : IComponentFactory
    {
        public string ComponentName => "TOEM Autosplitter";
        public string Description => "Autosplitter for TOEM game";
        public IComponent Create(LiveSplitState state) => new TOEMComponent(state);
        public string UpdateName => ComponentName;
        public string UpdateURL => "https://raw.githubusercontent.com/CielDeVille/LiveSplit.TOEM/master/";
        public string XMLURL => this.UpdateURL + "Components/LiveSplit.TOEM.Updates.xml";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public ComponentCategory Category => ComponentCategory.Control;
    }
}
