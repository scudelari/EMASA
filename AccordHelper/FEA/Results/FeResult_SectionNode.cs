using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_SectionNode
    {
        public int SectionNodeId { get; set; }

        /// <summary>
        /// Principal total strain - 1
        /// </summary>
        public double? EPTT1 { get; set; }

        /// <summary>
        /// Principal total strain - 2
        /// </summary>
        public double? EPTT2 { get; set; }

        /// <summary>
        /// Principal total strain - 3
        /// </summary>
        public double? EPTT3 { get; set; }

        /// <summary>
        /// Principal total strain Intensity
        /// </summary>
        public double? EPTTINT { get; set; }

        /// <summary>
        /// Principal total strain Equivalent
        /// </summary>
        public double? EPTTEQV { get; set; }

        /// <summary>
        /// Principal stresses - 1
        /// </summary>
        public double? S1 { get; set; }

        /// <summary>
        /// Principal stresses - 2
        /// </summary>
        public double? S2 { get; set; }

        /// <summary>
        /// Principal stresses - 3
        /// </summary>
        public double? S3 { get; set; }

        /// <summary>
        /// Principal stresses Intensity
        /// </summary>
        public double? SINT { get; set; }

        /// <summary>
        /// Principal stresses Equivalent
        /// </summary>
        public double? SEQV { get; set; }
    }
}
