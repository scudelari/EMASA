using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using BaseWPFLibrary.Forms;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class Integer_Input_ParamDef : Input_ParamDefBase
    {
        public override int VarCount => 1;
        public override string TypeName => "Integer";

        public Integer_Input_ParamDef(string inName, IntegerValueRange inRange) : base(inName)
        {
            SearchRange = inRange;
            Start = inRange.Mid;
        }

        private IntegerValueRange _searchRange;
        public IntegerValueRange SearchRange
        {
            get => _searchRange;
            set => SetProperty(ref _searchRange, value);
        }

        private int _start;
        public int Start
        {
            get => _start;
            set
            {
                if (!SearchRange.IsInside(value)) throw new Exception("Given start value must be within the search range.");
                SetProperty(ref _start, value);
            }
        }

        #region UI Helpers
        #endregion
    }
}
