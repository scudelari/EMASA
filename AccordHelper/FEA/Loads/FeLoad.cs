using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace AccordHelper.FEA.Loads
{
    public abstract class FeLoad : BindableBase
    {
        private double _factor = 1d;
        public double Factor
        {
            get => _factor;
            set => SetProperty(ref _factor, value);
        }

        private bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        protected FeLoad()
        {
        }

        public abstract void LoadModel(FeModelBase inModel);
    }
}
