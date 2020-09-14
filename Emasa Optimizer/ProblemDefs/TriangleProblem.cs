using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Opt;

namespace Emasa_Optimizer.ProblemDefs
{
    public class TriangleProblem : GeneralProblem
    {
        public TriangleProblem([NotNull] SolveManager inOwner) : base(inOwner)
        {
        }

        public override string WpfFriendlyName => "Triangle";
        public override string WpfProblemDescription => "Searches for a triangle that is horizontal, equilateral and centralized origin.";
        public override bool IsFea => false;
        public override bool TargetsOpenGhAlgorithm
        {
            get
            {
                List<string> requiredInputs = new List<string> { "A", "B", "C" };
                List<string> ghInputs = _owner.Gh_Alg.InputDefs.Select(a => a.Name).ToList();

                // Sorts both lists
                requiredInputs.Sort();
                ghInputs.Sort();

                // The list of gh inputs is not the same
                if (!requiredInputs.SequenceEqual(ghInputs)) return false;

                List<string> requiredGeometry = new List<string> { "Inner Centroid", "Inner Lines", "Inner Points" };
                List<string> ghGeom = _owner.Gh_Alg.GeometryDefs.Select(a => a.Name).ToList();

                // Sorts both lists
                requiredGeometry.Sort();
                ghGeom.Sort();

                // The list of gh geometries is not the same
                if (!requiredGeometry.SequenceEqual(ghGeom)) return false;

                // The currently open GH algorithm has the expected input/output for this problem
                return true;
            }
        }
        public override bool OverridesGeneralProblem => true;
    }
}
