using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_ElementNodalForces
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
        /// Section shear forces - Z
        /// </summary>
        public double SFz { get; set; }

        /// <summary>
        /// Section shear forces - Y
        /// </summary>
        public double SFy { get; set; }
    }
}
