extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using r3dm::Rhino;
using r3dm::Rhino.Geometry;

namespace AccordHelper.Opt.ParamDefinitions
{
    public abstract class ValueRangeBase : BindableBase
    {
        public abstract string Min_DisplayString { get; set; }
        public abstract string Max_DisplayString { get; set; }

        public virtual void DisplayVariablesChanged()
        {
            RaisePropertyChanged("Min_DisplayString");
            RaisePropertyChanged("Max_DisplayString");
        }

        public override string ToString()
        {
            return $"From: {Min_DisplayString} To: {Max_DisplayString}";
        }
    }
}
