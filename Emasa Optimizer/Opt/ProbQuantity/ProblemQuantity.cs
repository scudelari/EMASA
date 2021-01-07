using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Windows;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics.Statistics;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class ProblemQuantity : BindableBase
    {
        public bool IsFeResult => (QuantitySource is FeResultClassification);
        public bool IsGhDoubleList => (QuantitySource is DoubleList_GhGeom_ParamDef);
        public IProblemQuantitySource QuantitySource { get; private set; }
        public FeResultClassification QuantitySource_AsFeResult => QuantitySource as FeResultClassification;

        public ProblemQuantity(IProblemQuantitySource inProblemQuantitySource, Quantity_TreatmentTypeEnum inTreatmentType)
        {
            // Saves the index of this quantity for faster reference
            InternalId = AppSS.I.ProbQuantMgn.ProblemQuantityMaxIndex++;

            // Sets the quantity type
            QuantitySource = inProblemQuantitySource;

            // Sets the treatment type
            TreatmentType = inTreatmentType;

            // Initializes the Aggregator Options
            QuantityAggregatorOptions = new ProblemQuantityAggregator();
            if (IsFeResult && IsObjectiveFunctionMinimize)
            {
                QuantityAggregatorOptions.HasScale = false;
                QuantityAggregatorOptions.SetDefaultScale(QuantitySource_AsFeResult.ResultType);
            }
            else
            { 
                QuantityAggregatorOptions.HasScale = false;
            }

        }

        private int _internalId;
        public int InternalId
        {
            get => _internalId;
            set => SetProperty(ref _internalId, value);
        }

        #region Variables related to the type of treatment of this quantity
        public bool IsOutputOnly => TreatmentType == Quantity_TreatmentTypeEnum.OutputOnly;
        public bool IsObjectiveFunctionMinimize => TreatmentType == Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize;
        public bool IsConstraint => TreatmentType == Quantity_TreatmentTypeEnum.Constraint;

        private Quantity_TreatmentTypeEnum _treatmentType;
        public Quantity_TreatmentTypeEnum TreatmentType
        {
            get => _treatmentType;
            set
            {
                SetProperty(ref _treatmentType, value);

                RaisePropertyChanged("IsOutputOnly");
                RaisePropertyChanged("IsObjectiveFunctionMinimize");
                RaisePropertyChanged("IsConstraint");
            }
        }
        #endregion

        #region Variables related to the Objective Function
        public Dictionary<Quantity_FunctionObjectiveEnum, Tuple<string, double, string>> Quantity_FunctionObjectiveEnumDescriptions => ListDescSH.I.Quantity_FunctionObjectiveEnumDescriptions;
        private Quantity_FunctionObjectiveEnum _functionObjective = Quantity_FunctionObjectiveEnum.Minimize;
        public Quantity_FunctionObjectiveEnum FunctionObjective
        {
            get => _functionObjective;
            set
            {
                SetProperty(ref _functionObjective, value);
                RaisePropertyChanged("FunctionObjective_TargetValue_TextBoxVisibility");
            }
        }
        
        public Visibility FunctionObjective_TargetValue_TextBoxVisibility => FunctionObjective == Quantity_FunctionObjectiveEnum.Target ? Visibility.Visible : Visibility.Hidden;
        private double _functionObjectiveTargetValue = 0d;
        public double FunctionObjective_TargetValue
        {
            get => _functionObjectiveTargetValue;
            set => SetProperty(ref _functionObjectiveTargetValue, value);
        }
        #endregion

        #region Variables Related to Constraint SETUP
        public Dictionary<Quantity_ConstraintObjectiveEnum, string> Quantity_ConstraintObjectiveEnumDescriptions => ListDescSH.I.Quantity_ConstraintObjectiveEnumDescriptions;
        private Quantity_ConstraintObjectiveEnum _constraintObjective;
        public Quantity_ConstraintObjectiveEnum ConstraintObjective
        {
            get => _constraintObjective;
            set => SetProperty(ref _constraintObjective, value);
        }

        private double _constraintTolerance = 0.001d;
        public double ConstraintTolerance
        {
            get => _constraintTolerance;
            set => SetProperty(ref _constraintTolerance, value);
        }

        private double _constraintObjective_CompareValue;
        public double ConstraintObjective_CompareValue
        {
            get => _constraintObjective_CompareValue;
            set => SetProperty(ref _constraintObjective_CompareValue, value);
        }
        #endregion

        public Visibility FilterEntity_GridVisibility => (IsFeResult) ? Visibility.Visible : Visibility.Collapsed;


        #region Wpf
        public void DeleteProblemQuantity()
        {
            AppSS.I.ProbQuantMgn.DeleteProblemQuantity(this);
        }

        public override string ToString()
        {
            return $"[{ListDescSH.I.Quantity_TreatmentTypeEnumDescriptions[TreatmentType]}] {QuantitySource} :: {ListDescSH.I.Quantity_AggregateTypeEnumDescriptions[QuantityAggregatorOptions.AggregateType]}";
        }

        public string WpfSummaryFunctionObjective_ConstraintObjective_DisplayOnly => Quantity_ConstraintObjectiveEnumDescriptions[ConstraintObjective];
        public Tuple<string, double, string> WpfSummaryFunctionObjective_DisplayOnly => Quantity_FunctionObjectiveEnumDescriptions[FunctionObjective];
        public Visibility WpfSummaryFunctionObjective_FunctionObjective_TargetValue_TextBlockVisibility => FunctionObjective == Quantity_FunctionObjectiveEnum.Target ? Visibility.Visible : Visibility.Collapsed;
        #endregion
        
        #region Selected Aggregator Configuration
        public ProblemQuantityAggregator QuantityAggregatorOptions { get; private set; }
        #endregion


        #region Constraint Calculations
        // Must be set here in the Problem Quantity because they exists BEFORE the iterations start and thus may be used to hold the constraint functions
        public double NlOptEntryPoint_ConstraintFunction(double[] inValues, double[] inGradient)
        {
            // The inValues are ignored because it is better to go directly to the CurrentCalc_NlOptPoint of the problem
            NlOpt_Point solPoint = AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint;

            // Calculates this quantity at the solution point
            solPoint.CalculateConstraintResult(this);

            // Do we require gradient?
            if (inGradient != null)
            {
                for (int i = 0; i < solPoint.GradientSolutionPoints.Length; i++)
                {
                    // TODO: It is possible that we need here also to calculate the eval as it would be considering the constraint objective

                    // Calculates this quantity at the gradient solution point
                    solPoint.GradientSolutionPoints[i].CalculateConstraintResult(this);

                    double a, b, xa, xb;

                    // Calculates the gradient
                    a = solPoint.InputValuesAsDoubleArray[i];
                    b = solPoint.GradientSolutionPoints[i].InputValuesAsDoubleArray[i];

                    switch (ConstraintObjective)
                    {
                        case Quantity_ConstraintObjectiveEnum.EqualTo: // fc(x) - Tolerance = 0
                        case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual: // fc(x) <= Tolerance
                            xa = solPoint.ConstraintData[this].EvalValue - ConstraintObjective_CompareValue;
                            xb = solPoint.GradientSolutionPoints[i].ConstraintData[this].EvalValue - ConstraintObjective_CompareValue;
                            break;

                        case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual: // // fc(x) <= Tolerance
                            xa = -(solPoint.ConstraintData[this].EvalValue - ConstraintObjective_CompareValue);
                            xb = -(solPoint.GradientSolutionPoints[i].ConstraintData[this].EvalValue - ConstraintObjective_CompareValue);
                            break;

                        default:
                            return double.NaN;
                    }

                    inGradient[i] = (xb - xa) / (b - a);

                }
                // Adds a copy to the solution point's records
                solPoint.ConstraintData[this].Gradients = inGradient.Clone() as double[];
            }

            // The quantity itself depends on the constraint objective and value
            switch (ConstraintObjective)
            {
                //case Quantity_ConstraintObjectiveEnum.EqualTo: // |fc(x)| <= Tolerance
                //    return Math.Abs(solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue);

                case Quantity_ConstraintObjectiveEnum.EqualTo: // fc(x) - Tolerance = 0
                case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual: // fc(x) <= Tolerance
                    return solPoint.ConstraintData[this].EvalValue - ConstraintObjective_CompareValue;

                case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual: // // fc(x) <= Tolerance
                    return -(solPoint.ConstraintData[this].EvalValue - ConstraintObjective_CompareValue);

                default:
                    return double.NaN;
            }
        }
        #endregion


        public void Wpf_ClickedOver_SetDisplayAggregatorFilters()
        {
            // Copies the settings of this problem quantity's aggregator to the display options
            AppSS.I.NlOptDetails_DisplayAggregator.CopySettingsFrom(QuantityAggregatorOptions);

            // Tells the interface that the display aggregator changed
            AppSS.I.NlOptDetails_DisplayAggregator_Changed();
        }
    }

    public enum Quantity_TreatmentTypeEnum
    {
        OutputOnly,
        ObjectiveFunctionMinimize,
        Constraint
    }

    public enum Quantity_FunctionObjectiveEnum
    {
        Minimize,
        Maximize,
        Target,
    }

    public enum Quantity_ConstraintObjectiveEnum
    {
        EqualTo,
        LowerThanOrEqual,
        HigherThanOrEqual
    }
}
