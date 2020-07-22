using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_ElementNodalBendingStrain
    {
        /// <summary>
        /// Axial strain at the end
        /// </summary>
        public double EPELDIR { get; set; }

        /// <summary>
        /// Bending strain on the element +Y side of the beam.
        /// </summary>
        public double EPELByT { get; set; }

        /// <summary>
        /// Bending strain on the element -Y side of the beam.
        /// </summary>
        public double EPELByB { get; set; }

        /// <summary>
        /// Bending strain on the element +Z side of the beam.
        /// </summary>
        public double EPELBzT { get; set; }

        /// <summary>
        /// Bending strain on the element -Z side of the beam.
        /// </summary>
        public double EPELBzB { get; set; }
    }
}
