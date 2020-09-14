using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using Emasa_Optimizer.Opt;
using Prism.Commands;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Bindings
{
    public class FormGeneralBindings : BindableSingleton<FormGeneralBindings>
    {
        private FormGeneralBindings() { }

        private SolveManager _solveManager;
        public SolveManager SolveManager
        {
            get => _solveManager;
            set => SetProperty(ref _solveManager, value);
        }
        
        public override void SetOrReset()
        {
            SolveManager = new SolveManager();
        }
    }

}
