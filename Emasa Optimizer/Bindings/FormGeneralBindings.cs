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

        private SolveManager _solveMgr;
        public SolveManager SolveMgr
        {
            get => _solveMgr;
            set => SetProperty(ref _solveMgr, value);
        }
        
        public override void SetOrReset()
        {
            SolveMgr = new SolveManager();
        }
    }

}
