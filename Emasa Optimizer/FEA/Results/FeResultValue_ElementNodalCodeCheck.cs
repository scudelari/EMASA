using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_ElementNodalCodeCheck : FeResultValue
    {
        /// <summary>
        /// P_A = Fx / c_secArea
        /// </summary>
        public double P_A { get; set; }

        /// <summary>
        /// M2_Z2 = My / c_secPlMod2
        /// </summary>
        public double M2_Z2 { get; set; }


        /// <summary>
        /// M3_Z3 = Mz / c_secPlMod3
        /// </summary>
        public double M3_Z3 { get; set; }

        /// <summary>
        /// SUM = P_A + M2_Z2 + M3_Z3
        /// </summary>
        public double SUM { get; set; }

        /// <summary>
        /// G_MAT_FY = Fy * gamma_mat
        /// </summary>
        public double G_MAT_FY { get; set; }

        /// <summary>
        /// RATIO = SUM / G_MAT_FY
        /// </summary>
        public double RATIO { get; set; }
    }
}
