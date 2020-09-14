using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class DoubleList_GhGeom_ParamDef : GhGeom_ParamDefBase
    {
        public DoubleList_GhGeom_ParamDef(string inName) : base(inName)
        {
        }

        public override string TypeName => "DoubleList";
    }
}
