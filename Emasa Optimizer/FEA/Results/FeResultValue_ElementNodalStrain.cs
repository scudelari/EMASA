using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_ElementNodalStrain : FeResultValue
    {
        /// <summary>
        /// Axial strain
        /// </summary>
        public double Ex { get; set; }

        /// <summary>
        /// Curvature - Y
        /// </summary>
        public double Ky { get; set; }

        /// <summary>
        /// Curvature - Z
        /// </summary>
        public double Kz { get; set; }

        /// <summary>
        /// OptimizationSection shear strains - Z
        /// </summary>
        public double SEz { get; set; }

        /// <summary>
        /// OptimizationSection shear strains - Y
        /// </summary>
        public double SEy { get; set; }
    }
}
