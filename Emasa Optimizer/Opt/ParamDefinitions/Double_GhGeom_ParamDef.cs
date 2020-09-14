using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary.Forms;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    /// <summary>
    /// Defines an output parameter of type Double.
    /// You can specify a range to scale the values so that they will have a matching scale on the Objective Function.
    /// This will greatly assist when problems have, for instance, lengths together with radian angles.
    /// </summary>
    public class Double_GhGeom_ParamDef : GhGeom_ParamDefBase
    {
        public override string TypeName => "Double";

        public double AllowableRangePenalty
        {
            get => 100d;
        }

        public Double_GhGeom_ParamDef(string inName,
            double? inTargetValue = null,
            DoubleValueRange inExpectedScale = null,
            DoubleValueRange inAllowableRange = null) : base(inName)
        {
            // Checks if the target value is allowed
            if (inTargetValue.HasValue && inAllowableRange != null)
            {
                if (!inAllowableRange.IsInside(inTargetValue.Value)) throw new Exception($"The target value {inTargetValue.Value} is outside the allowable range {inAllowableRange}.");
            }

            _targetValue = inTargetValue;

            ScaleRange = inExpectedScale;
            AllowableRange = inAllowableRange;
        }

        public DoubleValueRange ScaleRangeTyped
        {
            get => (DoubleValueRange) ScaleRange;
        }
        public DoubleValueRange AllowableRangeTyped
        {
            get => (DoubleValueRange)AllowableRange;
        }

        public static DoubleValueRange ZeroToHundred = new DoubleValueRange(0d,100d); 

        public double? TargetValueTyped => (double?) TargetValue;

        public override object TargetValue
        {
            get => _targetValue;
            set
            {
                switch (value)
                {
                    case null:
                        _targetValue = null;
                        break;

                    case double d:
                        SetProperty(ref _targetValue, d);
                        break;

                    default:
                        throw new Exception($"Target value {value} is not valid for {GetType()}.");
                }
            }
        }
        
        public double GetValueForSquareSum(double inCalculatedValue)
        {
            // We have an allowable range and we are outside of it
            if (AllowableRange != null && !AllowableRangeTyped.IsInside(inCalculatedValue))
            {
                double distanceFromAllowable = AllowableRangeTyped.DistanceFrom(inCalculatedValue);
                
                // Do we have a scale?
                if (ScaleRange != null) distanceFromAllowable = ScaleRangeTyped.Scale(distanceFromAllowable, ZeroToHundred);

                // else, returns the error with the penalty
                return AllowableRangePenalty * distanceFromAllowable;
            }

            // We don't have an allowable OR we are within it
            double calcVal =
                ScaleRange == null ? inCalculatedValue : ScaleRangeTyped.Scale(inCalculatedValue, ZeroToHundred);

            double targetVal = 0d;
            if (TargetValueTyped.HasValue)
            {
                targetVal =
                    ScaleRange == null ? TargetValueTyped.Value : ScaleRangeTyped.Scale(TargetValueTyped.Value, ZeroToHundred);
            }

            return calcVal - targetVal;
        }

        #region UI Helpers
        #endregion
    }
}
