extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Optimization;
using BaseWPFLibrary.Forms;
using r3dm::Rhino;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace AccordHelper.Opt.ParamDefinitions
{
    public class Point_Input_ParamDef : Input_ParamDefBase
    {
        public Point_Input_ParamDef(string inName, PointValueRange inRange) : base(inName, inRange)
        {
        }

        public PointValueRange SearchRangeTyped
        {
            get => (PointValueRange)SearchRange;
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

                    case Point3d typedValue:
                        SetProperty(ref _start, typedValue);
                        break;

                    default:
                        throw new Exception($"Start value {value} is not valid for {GetType()}.");
                }
            }
        }

        public override int VarCount => 3;

        public override List<NonlinearConstraint> ConstraintDefinitions(ObjectiveFunctionBase inFunction)
        {
            return new List<NonlinearConstraint>()
                {
                new NonlinearConstraint(inFunction, PointXValueConstraintFunction, ConstraintType.GreaterThanOrEqualTo, SearchRangeTyped.RangeX.Min, PointXValueConstraintGradient),
                new NonlinearConstraint(inFunction, PointYValueConstraintFunction, ConstraintType.GreaterThanOrEqualTo, SearchRangeTyped.RangeY.Min, PointYValueConstraintGradient),
                new NonlinearConstraint(inFunction, PointZValueConstraintFunction, ConstraintType.GreaterThanOrEqualTo, SearchRangeTyped.RangeZ.Min, PointZValueConstraintGradient),

                new NonlinearConstraint(inFunction, PointXValueConstraintFunction, ConstraintType.LesserThanOrEqualTo, SearchRangeTyped.RangeX.Max, PointXValueConstraintGradient),
                new NonlinearConstraint(inFunction, PointYValueConstraintFunction, ConstraintType.LesserThanOrEqualTo, SearchRangeTyped.RangeY.Max, PointYValueConstraintGradient),
                new NonlinearConstraint(inFunction, PointZValueConstraintFunction, ConstraintType.LesserThanOrEqualTo, SearchRangeTyped.RangeZ.Max, PointZValueConstraintGradient)
                };
        }
        public double PointXValueConstraintFunction(double[] inInputs)
        {
            // Assumes the input variable to be an integer
            return inInputs[IndexInDoubleArray + 0];
        }
        public double PointYValueConstraintFunction(double[] inInputs)
        {
            // Assumes the input variable to be an integer
            return inInputs[IndexInDoubleArray + 1];
        }
        public double PointZValueConstraintFunction(double[] inInputs)
        {
            // Assumes the input variable to be an integer
            return inInputs[IndexInDoubleArray + 2];
        }
        public double[] PointXValueConstraintGradient(double[] inInputs)
        {
            double[] retArray = Enumerable.Repeat(0d, inInputs.Length).ToArray();
            retArray[IndexInDoubleArray + 0] = 1d;
            return retArray;
        }
        public double[] PointYValueConstraintGradient(double[] inInputs)
        {
            double[] retArray = Enumerable.Repeat(0d, inInputs.Length).ToArray();
            retArray[IndexInDoubleArray + 1] = 1d;
            return retArray;
        }
        public double[] PointZValueConstraintGradient(double[] inInputs)
        {
            double[] retArray = Enumerable.Repeat(0d, inInputs.Length).ToArray();
            retArray[IndexInDoubleArray + 2] = 1d;
            return retArray;
        }

        public override string TypeName => "Point";


        #region UI Helpers
        public override void UpdateInputParameter(string inStartString, string inMinBoundString, string inMaxBoundString)
        {
            try
            {
                Point3d? startVal = null, minBoundVal = null, maxBoundVal = null;

                if (!string.IsNullOrWhiteSpace(inStartString))
                {
                    if (RhinoStaticMethods.TryParsePoint3d(inStartString, out Point3d val)) startVal = val;
                    else throw new InvalidCastException($"The input string {inStartString} could not be converted a value valid for {GetType()}'s start value.");
                }

                if (!string.IsNullOrWhiteSpace(inMinBoundString))
                {
                    if (RhinoStaticMethods.TryParsePoint3d(inMinBoundString, out Point3d val)) minBoundVal = val;
                    else throw new InvalidCastException($"The input string {inMinBoundString} could not be converted a value valid for {GetType()}'s minimum boundary value.");
                }

                if (!string.IsNullOrWhiteSpace(inMaxBoundString))
                {
                    if (RhinoStaticMethods.TryParsePoint3d(inMaxBoundString, out Point3d val)) maxBoundVal = val;
                    else throw new InvalidCastException($"The input string {inMaxBoundString} could not be converted a value valid for {GetType()}'s maximum boundary value.");
                }

                // Validates the bounds XOR
                if (!(minBoundVal.HasValue && maxBoundVal.HasValue))
                    throw new InvalidOperationException($"All input values must have the boundaries set.");

                if (startVal.HasValue) Start = startVal.Value;
                else Start = null;

                SearchRange = new PointValueRange(minBoundVal.Value, maxBoundVal.Value);
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
