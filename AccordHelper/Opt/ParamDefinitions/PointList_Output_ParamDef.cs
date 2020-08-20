using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Items;

namespace AccordHelper.Opt.ParamDefinitions
{
    public class PointList_Output_ParamDef : Output_ParamDefBase
    {
        public PointList_Output_ParamDef(string inName, FeRestraint inDefaultRestraint = null) : base(inName)
        {
            if (inDefaultRestraint == null) Restraint = new FeRestraint(); // All False
            else Restraint = inDefaultRestraint;
        }

        public override string TypeName => "PointList";

        public FeRestraint Restraint { get; private set; }
    }
}
