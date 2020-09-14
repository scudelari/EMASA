using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_NodalDisplacements : FeResultValue
    {
        public double UX { get; set; }
        public double UY { get; set; }
        public double UZ { get; set; }
        public double RX { get; set; }
        public double RY { get; set; }
        public double RZ { get; set; }
    }
}
