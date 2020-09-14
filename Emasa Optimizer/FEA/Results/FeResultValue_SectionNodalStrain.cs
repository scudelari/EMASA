using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_SectionNodalStrain : FeResultValue
    {
        /// <summary>
        /// Principal total strain - 1
        /// </summary>
        public double EPTT1 { get; set; }

        /// <summary>
        /// Principal total strain - 2
        /// </summary>
        public double EPTT2 { get; set; }

        /// <summary>
        /// Principal total strain - 3
        /// </summary>
        public double EPTT3 { get; set; }

        /// <summary>
        /// Principal total strain Intensity
        /// </summary>
        public double EPTTINT { get; set; }

        /// <summary>
        /// Principal total strain Equivalent
        /// </summary>
        public double EPTTEQV { get; set; }

    }
}
