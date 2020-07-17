using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Forms;

using TSDatatype = Tekla.Structures.Datatype;
using TSModel = Tekla.Structures.Model;
using TSPlugins = Tekla.Structures.Plugins;

namespace TeklaPluginInOutWPF
{
    /// <summary>
    /// Main Class of the Plugin
    /// </summary>
    [TSPlugins.Plugin("EMSXMLInOut")]
    [TSPlugins.PluginUserInterface("EMSXMLInOut.MainForm")]
    public class MainPlugin : TSPlugins.PluginBase
    {
        // Enable inserting of objects in a model
        private readonly TSModel.Model _model;
        public TSModel.Model Model
        {
            get { return _model; }
        }

        // Enable retrieving of input values
        private readonly StructuresData _data;

        public MainPlugin(StructuresData data)
        {
            // Link to model.         
            //_model = new TSModel.Model(true);
            _model = new TSModel.Model();

            // Link to input values.         
            _data = data;

        }


        public override List<InputDefinition> DefineInput()
        {
            //throw new NotImplementedException();
            return new List<InputDefinition>();
        }

        public override bool Run(List<InputDefinition> Input)
        {
            try
            {
                // Shows the form
                MainWindow ioForm = new MainWindow(Model);
                ioForm.Closed += IoForm_Closed;
                ioForm.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return true;
        }

        private void IoForm_Closed(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class that has the data from the attributes on the dialog (???)
    /// </summary>
    public class StructuresData
    {
        //[TSPlugins.StructuresField("RafaAttribute")]
        //public double RafaVar;
    }
}
