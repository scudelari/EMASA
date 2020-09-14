extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using r3dm::Rhino;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public abstract class ValueRangeBase : BindableBase
    {
        public abstract string WpfMinString { get; set; }
        public abstract string WpfMaxString { get; set; }

        public virtual void DisplayVariablesChanged()
        {
            RaisePropertyChanged("WpfMinString");
            RaisePropertyChanged("WpfMaxString");
        }

        public override string ToString()
        {
            return $"From: {WpfMinString} To: {WpfMaxString}";
        }
    }
}
