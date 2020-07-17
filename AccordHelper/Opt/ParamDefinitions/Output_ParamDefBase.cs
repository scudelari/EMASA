using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.Opt.ParamDefinitions
{
    public abstract class Output_ParamDefBase : ParamDefBase
    {
        public Output_ParamDefBase(string inName) : base(inName)
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

        #region UI Helpers

        public virtual void UpdateOutputParameter(string inTargetString, string inMinScaleString, string inMaxScaleString, string inMinAllowableString, string inMaxAllowableString)
        {
            throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
        }
        public override void UpdateBindingValues()
        {
            RaisePropertyChanged("TargetValueStr");
            RaisePropertyChanged("ScaleRange");
            RaisePropertyChanged("AllowableRange");
            IsDirty = false;
        }
        #endregion
    }
}
