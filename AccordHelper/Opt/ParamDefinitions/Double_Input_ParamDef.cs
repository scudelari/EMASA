using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math.Optimization;
using BaseWPFLibrary.Forms;

namespace AccordHelper.Opt.ParamDefinitions
{
    public class Double_Input_ParamDef : Input_ParamDefBase
    {
        public Double_Input_ParamDef(string inName, DoubleValueRange inRange) : base(inName, inRange)
        {
        }

        public DoubleValueRange SearchRangeTyped
        {
            get => (DoubleValueRange)SearchRange;
        }

        public override object Start
        {
            get => _start;
            set
            {
                switch (value)
                {
                    case null:
                        Start = null;
                        break;

                    case double typedValue:
                        SetProperty(ref _start, typedValue);
                        break;

                    default:
                        throw new Exception($"Start value {value} is not valid for {GetType()}.");
                }
            }
        }

        public override int VarCount => 1;

        //public override List<NonlinearConstraint> ConstraintDefinitions(ObjectiveFunctionBase inFunction)
        //{
        //    return new List<NonlinearConstraint>()
        //        {
        //        new NonlinearConstraint(inFunction, SingleValueConstraintFunction, ConstraintType.GreaterThanOrEqualTo, SearchRangeTyped.Range.Min, SingleValueConstraintGradient),
        //        new NonlinearConstraint(inFunction, SingleValueConstraintFunction, ConstraintType.LesserThanOrEqualTo, SearchRangeTyped.Range.Max, SingleValueConstraintGradient)
        //        };
        //}
        //public double SingleValueConstraintFunction(double[] inInputs)
        //{
        //    // Assumes the input variable to be an integer
        //    return inInputs[IndexInDoubleArray];
        //}
        //public double[] SingleValueConstraintGradient(double[] inInputs)
        //{
        //    double[] retArray = Enumerable.Repeat(0d, inInputs.Length).ToArray();
        //    retArray[IndexInDoubleArray] = 1d;
        //    return retArray;
        //}

        public override string TypeName => "Double";


        #region UI Helpers

        public override void UpdateInputParameter(string inStartString, string inMinBoundString, string inMaxBoundString)
        {
            try
            {
                double? startVal = null, minBoundVal = null, maxBoundVal = null;

                if (!string.IsNullOrWhiteSpace(inStartString))
                {
                    if (double.TryParse(inStartString, out double val)) startVal = val;
                    else throw new InvalidCastException($"The input string {inStartString} could not be converted a value valid for {GetType()}'s start value.");
                }

                if (!string.IsNullOrWhiteSpace(inMinBoundString))
                {
                    if (double.TryParse(inMinBoundString, out double val)) minBoundVal = val;
                    else throw new InvalidCastException($"The input string {inMinBoundString} could not be converted a value valid for {GetType()}'s minimum boundary value.");
                }

                if (!string.IsNullOrWhiteSpace(inMaxBoundString))
                {
                    if (double.TryParse(inMaxBoundString, out double val)) maxBoundVal = val;
                    else throw new InvalidCastException($"The input string {inMaxBoundString} could not be converted a value valid for {GetType()}'s maximum boundary value.");
                }

                // Validates the bounds XOR
                if (!(minBoundVal.HasValue && maxBoundVal.HasValue))
                    throw new InvalidOperationException($"All input values must have the boundaries set.");

                if (startVal.HasValue) Start = startVal.Value;
                else Start = null;

                SearchRange = new DoubleValueRange(minBoundVal.Value, maxBoundVal.Value);
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
