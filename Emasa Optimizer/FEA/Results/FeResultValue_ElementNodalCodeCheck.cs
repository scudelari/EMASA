using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_ElementNodalCodeCheck : FeResultValue
    {
        public double Pr { get; set; } = 0;
        public double MrMajor { get; set; } = 0;
        public double MrMinor { get; set; } = 0;
        public double VrMajor { get; set; } = 0;
        public double VrMinor { get; set; } = 0;
        public double Tr { get; set; } = 0;

        public double PRatio { get; set; } = 0;
        public double MMajRatio { get; set; } = 0;
        public double MMinRatio { get; set; } = 0;
        public double VMajRatio { get; set; } = 0;
        public double VMinRatio { get; set; } = 0;
        public double TorRatio { get; set; } = 0;

        public double PcComp { get; set; } = 0;
        public double PcTension { get; set; } = 0;
        public double MrMajorDsgn { get; set; } = 0;
        public double McMajor { get; set; } = 0;
        public double MrMinorDsgn { get; set; } = 0;
        public double McMinor { get; set; } = 0;

        public double TotalRatio { get; set; } = 0;
    }
}
