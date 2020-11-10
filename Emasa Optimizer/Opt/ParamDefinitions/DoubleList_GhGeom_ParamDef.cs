using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Opt.ProbQuantity;

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
        public string ResultTypeExplanation => $"Grasshopper double list named {TargetShapeDescription}";

        public string Wpf_ProblemQuantityName => Name;
        public string Wpf_ProblemQuantityGroup => "GH Double List";
        public string Wpf_Explanation => $"Grasshopper double list named {TargetShapeDescription}";

        public string ScreenShotFileName => "DoubleList_RhinoScreenshot";
        public string DataTableName => $"{Wpf_ProblemQuantityGroup} - {Wpf_ProblemQuantityName}";

        public void AddProblemQuantity_FunctionObjective()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize));
        }
        public void AddProblemQuantity_ConstraintObjective()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.Constraint));
        }
        public void AddProblemQuantity_OutputOnly()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.OutputOnly));
        }
        #endregion
    }
}
