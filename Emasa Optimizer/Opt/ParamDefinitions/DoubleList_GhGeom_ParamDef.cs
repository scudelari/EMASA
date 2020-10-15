using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.ProblemDefs;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class DoubleList_GhGeom_ParamDef : GhGeom_ParamDefBase, IProblemQuantitySource
    {
        public DoubleList_GhGeom_ParamDef(string inName) : base(inName)
        {
        }
        public override string TypeName => "DoubleList";

        #region IProblemQuantitySource
        public bool IsGhGeometryDoubleListData => true;
        public bool IsFiniteElementData => false;
        public string ResultFamilyGroupName => "GH Double List";
        public string ResultTypeDescription => "";
        public string TargetShapeDescription => Name;

        public void AddProblemQuantity_FunctionObjective(object inSolveMan)
        {
            if (inSolveMan is SolveManager s) s.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize, s));
            else
                throw new InvalidOperationException($"{nameof(inSolveMan)} is not of expected type ({typeof(SolveManager)}).");
        }
        public void AddProblemQuantity_ConstraintObjective(object inSolveMan)
        {
            if (inSolveMan is SolveManager s) s.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.Constraint, s));
            else
                throw new InvalidOperationException($"{nameof(inSolveMan)} is not of expected type ({typeof(SolveManager)}).");
        }
        public void AddProblemQuantity_OutputOnly(object inSolveMan)
        {
            if (inSolveMan is SolveManager s) s.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.OutputOnly, s));
            else
                throw new InvalidOperationException($"{nameof(inSolveMan)} is not of expected type ({typeof(SolveManager)}).");
        }
        #endregion
    }
}
