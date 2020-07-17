using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Accord;
using Accord.Math;
using BaseWPFLibrary.Forms;
using Prism.Mvvm;

namespace AccordHelper.Opt.ParamDefinitions
{
    /// <summary>
    /// Defines an output parameter of type Double.
    /// You can specify a range to scale the values so that they will have a matching scale on the Objective Function.
    /// This will greatly assist when problems have, for instance, lengths together with radian angles.
    /// </summary>
    public class Double_Output_ParamDef : Output_ParamDefBase
    {
        public override string TypeName => "Double";

        public double AllowableRangePenalty
        {
            get => 100d;
        }

        public Double_Output_ParamDef(string inName,
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

        public override void UpdateOutputParameter(string inTargetString, string inMinScaleString, string inMaxScaleString, string inMinAllowableString, string inMaxAllowableString)
        {
            try
            {
                double? targetVal = null, minScaleVal = null, maxScaleVal = null, minAllowableVal = null, maxAllowableVal = null;

                if (!string.IsNullOrWhiteSpace(inTargetString))
                {
                    if (double.TryParse(inTargetString, out double val)) targetVal = val;
                    else throw new InvalidCastException($"The input string {inTargetString} could not be converted a value valid for {GetType()}'s start value.");
                }

                if (!string.IsNullOrWhiteSpace(inMinScaleString))
                {
                    if (double.TryParse(inMinScaleString, out double val)) minScaleVal = val;
                    else throw new InvalidCastException($"The input string {inMinScaleString} could not be converted a value valid for {GetType()}'s minimum boundary value.");
                }

                if (!string.IsNullOrWhiteSpace(inMaxScaleString))
                {
                    if (double.TryParse(inMaxScaleString, out double val)) maxScaleVal = val;
                    else throw new InvalidCastException($"The input string {inMaxScaleString} could not be converted a value valid for {GetType()}'s maximum boundary value.");
                }

                if (!string.IsNullOrWhiteSpace(inMinAllowableString))
                {
                    if (double.TryParse(inMinAllowableString, out double val)) minAllowableVal = val;
                    else throw new InvalidCastException($"The input string {inMinAllowableString} could not be converted a value valid for {GetType()}'s minimum boundary value.");
                }

                if (!string.IsNullOrWhiteSpace(inMaxAllowableString))
                {
                    if (double.TryParse(inMaxAllowableString, out double val)) maxAllowableVal = val;
                    else throw new InvalidCastException($"The input string {inMaxAllowableString} could not be converted a value valid for {GetType()}'s maximum boundary value.");
                }

                // Validates the bounds XOR
                if (minScaleVal.HasValue ^ maxScaleVal.HasValue) throw new InvalidOperationException($"Please set no scale range values, or both.");
                if (minAllowableVal.HasValue ^ maxAllowableVal.HasValue) throw new InvalidOperationException($"Please set no allowable range values, or both.");

                if (targetVal.HasValue) TargetValue = targetVal.Value;
                else TargetValue = null;

                if (minScaleVal.HasValue && maxScaleVal.HasValue) ScaleRange = new DoubleValueRange(minScaleVal.Value, maxScaleVal.Value);
                if (minAllowableVal.HasValue && maxAllowableVal.HasValue) AllowableRange = new DoubleValueRange(minAllowableVal.Value, maxAllowableVal.Value);
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, $"Could not update the data for the {Name} input parameter.");
            }
            finally
            {
                UpdateBindingValues();
            }
        }

        #endregion
    }
}
