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
        public FeResultItem([NotNull] FeResultClassification inResultClass, [NotNull] IFeEntity inFeEntity, [NotNull] FeResultValue inResultValue)
        {
            _feEntity = inFeEntity ?? throw new ArgumentNullException(nameof(inFeEntity));
            _resultValue = inResultValue ?? throw new ArgumentNullException(nameof(inResultValue));
            _resultClass = inResultClass ?? throw new ArgumentNullException(nameof(inResultClass));
        }

        private IFeEntity _feEntity;
        public IFeEntity FeEntity
        {
            get => _feEntity;
            set => _feEntity = value;
        }

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
