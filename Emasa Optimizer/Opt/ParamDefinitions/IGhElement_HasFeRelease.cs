using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public interface IGhElement_HasFeRelease
    {
        string Name { get; }
        string TypeName { get; }
        FeRelease Release { get; }
    }
}
