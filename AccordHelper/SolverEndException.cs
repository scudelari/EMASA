using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.Opt;

namespace AccordHelper
{
    /// <summary>
    /// Exception to be thrown when the solver wants to signal that it finished
    /// </summary>
    public class SolverEndException : Exception
    {
        public double FinalFunctionEvalValue { get; set; }
        public SolverStatus FinishStatus { get; set; }

        public SolverEndException(SolverStatus inStatus , double inFinalFunctionEvalValue)
        {
            FinalFunctionEvalValue = inFinalFunctionEvalValue;
            FinishStatus = inStatus;
        }
    }
}
