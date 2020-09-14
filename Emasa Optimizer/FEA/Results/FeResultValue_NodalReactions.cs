using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_NodalReactions : FeResultValue
    {
        public FeResultValue_NodalReactions()
        {
        }

        public FeResultValue_NodalReactions(double? inFx, double? inFy, double? inFz, double? inMx, double? inMy, double? inMz)
        {
            FX = inFx;
            FY = inFy;
            FZ = inFz;
            MX = inMx;
            MY = inMy;
            MZ = inMz;
        }

        public double? FX { get; set; }
        public double? FY { get; set; }
        public double? FZ { get; set; }
        public double? MX { get; set; }
        public double? MY { get; set; }
        public double? MZ { get; set; }


        public bool ContainsAnyValue
        {
            get
            {
                return FX.HasValue || FY.HasValue || FZ.HasValue || MX.HasValue || MY.HasValue || MZ.HasValue;
            }
        }
    }
}
