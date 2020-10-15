using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultValue_EigenvalueBucklingSummary : FeResultValue
    {
        /// <summary>
        /// Dictionary where the key is the mode and the value is the multiplier.
        /// The key starts with 1
        /// </summary>
        public Dictionary<int, double> EigenvalueBucklingMultipliers { get; private set; } = new Dictionary<int, double>();

        private List<double> _nonNegative => (from a in EigenvalueBucklingMultipliers.Values where a > 0d select a).ToList();
        public double FirstNonNegative => _nonNegative[0];
        public double SecondNonNegative => _nonNegative[1];
        public double ThirdNonNegative => _nonNegative[2];
    }
}
