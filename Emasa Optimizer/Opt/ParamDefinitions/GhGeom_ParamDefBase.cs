using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public abstract class GhGeom_ParamDefBase : ParamDefBase
    {
        public GhGeom_ParamDefBase(string inName) : base(inName)
        {
        }

        #region Finite Element Assignments
        public string FeGroupNameHelper => $"{Name}_{TypeName}";
        #endregion

        #region UI Helpers
        #endregion
    }
}
