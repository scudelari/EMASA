using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.Opt.ParamDefinitions
{
    public class LineList_Output_ParamDef : Output_ParamDefBase
    {
        public LineList_Output_ParamDef(string inName) : base(inName)
        {
        }

        public override string TypeName => "LineList";
    }
}
