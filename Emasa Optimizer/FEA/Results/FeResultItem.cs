using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultItem
    {
        public FeResultItem([NotNull] FeResultClassification inResultClass, [NotNull] FeResultLocation inFeLocation, [NotNull] FeResultValue inResultValue)
        {
            _feLocation = inFeLocation ?? throw new ArgumentNullException(nameof(inFeLocation));
            _resultValue = inResultValue ?? throw new ArgumentNullException(nameof(inResultValue));
            _resultClass = inResultClass ?? throw new ArgumentNullException(nameof(inResultClass));
        }

        private FeResultLocation _feLocation;
        public FeResultLocation FeLocation
        {
            get => _feLocation;
            set => _feLocation = value;
        }

        /// <summary>
        /// Mutable Data Class that contains the values of the result
        /// </summary>
        private FeResultValue _resultValue;
        public FeResultValue ResultValue
        {
            get => _resultValue;
            set => _resultValue = value;
        }

        private FeResultClassification _resultClass;
        public FeResultClassification ResultClass
        {
            get => _resultClass;
            set => _resultClass = value;
        }
    }
}
