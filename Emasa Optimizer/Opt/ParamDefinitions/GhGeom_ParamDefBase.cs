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
        
        protected ValueRangeBase _scaleRange;
        public virtual ValueRangeBase ScaleRange
        {
            get => _scaleRange;
            set => _scaleRange = value;
        }

        protected ValueRangeBase _allowableRange;
        public virtual ValueRangeBase AllowableRange
        {
            get => _allowableRange;
            set => _allowableRange = value;
        }

        protected object _targetValue;
        public virtual object TargetValue
        {
            get => throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
            set => throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
        }
        public string TargetValueStr => TargetValue != null ? $"{TargetValue}" : "";

        #region Finite Element Assignments
        public string FeGroupNameHelper => $"{Name}_{TypeName}";
        #endregion


        #region UI Helpers
        #endregion
    }
}
