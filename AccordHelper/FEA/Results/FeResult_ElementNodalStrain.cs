using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_ElementNodalStrain
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
        /// Section shear strains - Z
        /// </summary>
        public double SEz { get; set; }

        /// <summary>
        /// Section shear strains - Y
        /// </summary>
        public double SEy { get; set; }
    }
}
