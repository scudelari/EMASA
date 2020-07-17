using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper
{
    public class SolverSuccessException : Exception
    {
        public double FinalValue { get; set; }

        public SolverSuccessException(double inFinalValue)
        {
            FinalValue = inFinalValue;
        }
    }
}
