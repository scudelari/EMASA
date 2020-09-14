using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Forms;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class Double_Input_ParamDef : Input_ParamDefBase
    {
        public override int VarCount => 1;
        public override string TypeName => "Double";

        public Double_Input_ParamDef(string inName, DoubleValueRange inRange) : base(inName)
        {
            _searchRange = inRange;
            Start = inRange.Mid;
        }

        private DoubleValueRange _searchRange;
        public DoubleValueRange SearchRange
        {
            get => _searchRange;
            set => SetProperty(ref _searchRange, value);
        }

        private double _start = double.NaN;
        public double Start
        {
            get => _start;
            set
            {
                if (!SearchRange.IsInside(value)) throw new Exception("Given start value must be within the search range.");
                SetProperty(ref _start, value);
            }
        }
    }
}
