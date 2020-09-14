using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Loads
{
    public abstract class FeLoad : BindableBase
    {
        protected FeLoad(double inMultiplier = 1d)
        {
            Multiplier = inMultiplier;
        }

        private double _multiplier = 1d;
        public double Multiplier
        {
            get => _multiplier;
            set => SetProperty(ref _multiplier, value);
        }
    }
}
