using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_ElementNodalStress : FeResultValue
    {
        /// <summary>
        /// Axial direct stress
        /// </summary>
        public double SDIR { get; set; }

        /// <summary>
        /// Bending stress on the element +Y side of the beam
        /// </summary>
        public double SByT { get; set; }

        /// <summary>
        /// Bending stress on the element -Y side of the beam
        /// </summary>
        public double SByB { get; set; }

        /// <summary>
        /// Bending stress on the element +Z side of the beam
        /// </summary>
        public double SBzT { get; set; }

        /// <summary>
        /// Bending stress on the element -Z side of the beam
        /// </summary>
        public double SBzB { get; set; }

    }
}
