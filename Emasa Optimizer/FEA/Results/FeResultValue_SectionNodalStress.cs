using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_SectionNodalStress : FeResultValue
    {
        /// <summary>
        /// Principal stresses - 1
        /// </summary>
        public double S1 { get; set; }

        /// <summary>
        /// Principal stresses - 2
        /// </summary>
        public double S2 { get; set; }

        /// <summary>
        /// Principal stresses - 3
        /// </summary>
        public double S3 { get; set; }

        /// <summary>
        /// Principal stresses Intensity
        /// </summary>
        public double SINT { get; set; }

        /// <summary>
        /// Principal stresses Equivalent
        /// </summary>
        public double SEQV { get; set; }
    }
}
