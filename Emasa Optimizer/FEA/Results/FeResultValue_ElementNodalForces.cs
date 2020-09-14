using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_ElementNodalForces : FeResultValue
    {
        /// <summary>
        /// Axial force
        /// </summary>
        public double Fx { get; set; }

        /// <summary>
        /// Bending moment - Y
        /// </summary>
        public double My { get; set; }

        /// <summary>
        /// Bending moment - Z
        /// </summary>
        public double Mz { get; set; }


        /// <summary>
        /// Torsional moment
        /// </summary>
        public double Tq { get; set; }

        /// <summary>
        /// OptimizationSection shear forces - Z
        /// </summary>
        public double SFz { get; set; }

        /// <summary>
        /// OptimizationSection shear forces - Y
        /// </summary>
        public double SFy { get; set; }
    }
}
