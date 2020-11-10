using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class PointList_GhGeom_ParamDef : GhGeom_ParamDefBase, IGhElement_HasFeRestraint
    {
        public override string TypeName => "PointList";

        public PointList_GhGeom_ParamDef(string inName, FeRestraint inDefaultRestraint = null) : base(inName)
        {
            if (inDefaultRestraint == null) Restraint = new FeRestraint(); // All False
            else Restraint = inDefaultRestraint;
        }

        #region Finite Element Assignments
        public FeRestraint Restraint { get; set; }
        #endregion
    }
}
