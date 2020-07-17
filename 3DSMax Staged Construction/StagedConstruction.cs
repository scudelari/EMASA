using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Max;
using Autodesk.Max.Plugins;

namespace _3DSMax_Staged_Construction
{
    public class StagedConstruction : UtilityObj
    {
        PluginDescriptor _pluginDescriptor;
        public PluginDescriptor PluginDesc { get => _pluginDescriptor; }

        public StagedConstruction(PluginDescriptor inPluginDesc) : base()
        {
            this._pluginDescriptor = inPluginDesc;
        }

        Staged3DSMaxWindow pluginWindow;
        public override void BeginEditParams(IInterface ip, IIUtil iu)
        {
            // Opens the main form of the plugin
            this.pluginWindow = new Staged3DSMaxWindow();
            this.pluginWindow.Closed += Wnd_Closed;

            // Locks the function!
            this.pluginWindow.ShowDialog();
        }

        private void Wnd_Closed(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(pluginWindow.ResidualMessage))
                MaxHelper.MaxInterface.PushPrompt(pluginWindow.ResidualMessage);
        }

        public override void EndEditParams(IInterface ip, IIUtil iu)
        {
            ip.PopPrompt();
        }
    }
}
