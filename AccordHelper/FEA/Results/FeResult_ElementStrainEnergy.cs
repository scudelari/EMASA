using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_ElementStrainEnergy
    {
        public FeResult_ElementStrainEnergy(double inStrainEnergy)
        {
            StrainEnergy = inStrainEnergy;
        }

        public double StrainEnergy { get; set; }
    }
}
