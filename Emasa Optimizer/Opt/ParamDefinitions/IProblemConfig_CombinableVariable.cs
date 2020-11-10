using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public interface IProblemConfig_CombinableVariable
    {
        /// <summary>
        /// This property has the responsibility of returning a lists with all the combinations of the given variable.
        /// For example, in the case of a LineList, it would give the combinations of the FeSection and the FeRestraint assigned to them.
        /// </summary>
        List<ProblemConfig_ElementCombinationValue> FlatCombinationList { get; }

        string Name { get; }
        string TypeName { get; }
    }
}
