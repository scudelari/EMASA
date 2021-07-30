using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAP2000v1;

namespace Sap2000PluginTest
{
    /// <summary>
    /// Sap2000 Entry Point
    /// </summary>
    public class cPlugin
    {
        private cSapModel _sapModel;
        private cPluginCallback _iSapPlugin;
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            // saves the references for future use
            _sapModel = SapModel;
            _iSapPlugin = ISapPlugin;

            MainWindow w = new MainWindow();
            w.Closed += (sender, args) => {
                // No error
                _iSapPlugin.Finish(0);
            };

            w.Show();
            

        }
        //Public Function Info(ByRef Text As String) As Integer
        public int Info(ref string Text)
        {
            Text = "This is a test plugin made by Emasa.";
            return 0;
        }

        // SAP2000v1.cPluginCallback.Finish(ByVal iVal As Integer)
        



    }
}
