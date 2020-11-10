using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLoptNet;

namespace NlOptNewVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            // Attempts to create
            NLoptSolver s = new NLoptSolver(NLoptAlgorithm.LN_COBYLA, 2, false, false, false);
        }
    }
}
