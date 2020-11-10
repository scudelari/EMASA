using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Emasa_Optimizer.WpfResources
{
    public class StringDoublePair : BindableBase
    {
        public StringDoublePair(string inStringValue = "", double inDoubleValue = double.NaN)
        {
            _doubleValue = inDoubleValue;
            _stringValue = inStringValue;
        }

        private double _doubleValue;
        public double DoubleValue
        {
            get => _doubleValue;
            set => SetProperty(ref _doubleValue, value);
        }

        private string _stringValue;
        public string StringValue
        {
            get => _stringValue;
            set => SetProperty(ref _stringValue, value);
        }

    }
}
