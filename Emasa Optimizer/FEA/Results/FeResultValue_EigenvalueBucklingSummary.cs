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

        private Dictionary<int, double> _eigenvalueBucklingMultipliers_NonNegative = null;
        public Dictionary<int, double> EigenvalueBucklingMultipliers_NonNegative
        {
            get
            {
                if (_eigenvalueBucklingMultipliers_NonNegative != null) return _eigenvalueBucklingMultipliers_NonNegative;

                _eigenvalueBucklingMultipliers_NonNegative = new Dictionary<int, double>();
                foreach (KeyValuePair<int, double> keyValuePair in EigenvalueBucklingMultipliers.Where(a => a.Value > 0d))
                {
                    _eigenvalueBucklingMultipliers_NonNegative.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return _eigenvalueBucklingMultipliers_NonNegative;
            }
        }

        private List<double> _nonNegative => (from a in EigenvalueBucklingMultipliers.Values where a > 0d select a).ToList();
        public double FirstNonNegative => _nonNegative[0];
        public double SecondNonNegative => _nonNegative[1];
        public double ThirdNonNegative => _nonNegative[2];
    }
}
