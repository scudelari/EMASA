using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Helpers.Accord;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.WpfResources;
using MathNet.Numerics.Statistics;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public class ProblemQuantity : BindableBase
    {
        [NotNull] private readonly SolveManager _owner;
        public bool IsFeResult => (QuantitySource is FeResultClassification);
        public bool IsGhDoubleList => (QuantitySource is DoubleList_GhGeom_ParamDef);
        public IProblemQuantitySource QuantitySource { get; private set; }
        public FeResultClassification QuantitySource_AsFeResult => QuantitySource as FeResultClassification;

        public ProblemQuantity(IProblemQuantitySource inProblemQuantitySource, Quantity_TreatmentTypeEnum inTreatmentType, [NotNull] SolveManager inOwner)
        {
            // Saves the owner
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));
            
            // Saves the index of this quantity for faster reference
            InternalId = _owner.ProblemQuantityMaxIndex++;

            // Sets the quantity type
            QuantitySource = inProblemQuantitySource;

            // Sets the treatment type
            TreatmentType = inTreatmentType;

            // Initializes the Aggregator Options
            QuantityAggregatorOptions = new ProblemQuantityAggregator();
            if (IsFeResult)
            {
                QuantityAggregatorOptions.HasScale = true;
                QuantityAggregatorOptions.SetDefaultScale(QuantitySource_AsFeResult.ResultType);
            }
            else QuantityAggregatorOptions.HasScale = false;

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

        public string ResultFamilyGroupName => QuantitySource.ResultFamilyGroupName;
        public string ResultTypeDescription => QuantitySource.ResultTypeDescription;
        public string TargetShapeDescription => QuantitySource.TargetShapeDescription;

        #region Variables related to the Objective Function
        public Dictionary<Quantity_FunctionObjectiveEnum, Tuple<string, double, string>> Quantity_FunctionObjectiveEnumDescriptions => ListDescriptionStaticHolder.ListDescSingleton.Quantity_FunctionObjectiveEnumDescriptiosn;
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
        public Dictionary<Quantity_ConstraintObjectiveEnum, string> Quantity_ConstraintObjectiveEnumDescriptions => ListDescriptionStaticHolder.ListDescSingleton.Quantity_ConstraintObjectiveEnumDescriptions;
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
            _owner.DeleteProblemQuantity(this);
        }

        public override string ToString()
        {
            return $"[{ListDescriptionStaticHolder.ListDescSingleton.Quantity_TreatmentTypeEnumDescriptions[TreatmentType]}] {TargetShapeDescription} - {ResultFamilyGroupName} - {ResultTypeDescription} : {QuantityAggregatorOptions.Quantity_AggregateTypeEnumDescriptions[QuantityAggregatorOptions.AggregateType]}";
        }


        public string WpfSummaryFunctionObjective_ConstraintObjective_DisplayOnly => Quantity_ConstraintObjectiveEnumDescriptions[ConstraintObjective];
        public Tuple<string, double, string> WpfSummaryFunctionObjective_DisplayOnly => Quantity_FunctionObjectiveEnumDescriptions[FunctionObjective];
        public Visibility WpfSummaryFunctionObjective_FunctionObjective_TargetValue_TextBlockVisibility => FunctionObjective == Quantity_FunctionObjectiveEnum.Target ? Visibility.Visible : Visibility.Collapsed;

        #endregion


        #region Selected Aggregator Configuration
        public ProblemQuantityAggregator QuantityAggregatorOptions { get; private set; }
        #endregion

        public string ConcernedResult_ColumnName
        {
            get
            {
                string outputColumnName = string.Empty;

                if (IsFeResult)
                {
                    switch (QuantitySource_AsFeResult.ResultType)
                    {
                        case FeResultTypeEnum.Nodal_Reaction_Fx:
                            outputColumnName = "FX (N)";
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_Fy:
                            outputColumnName = "FY (N)";
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_Fz:
                            outputColumnName = "FZ (N)";
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_Mx:
                            outputColumnName = "MX (Nm)";
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_My:
                            outputColumnName = "MY (Nm)";
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_Mz:
                            outputColumnName = "MZ (Nm)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Ux:
                            outputColumnName = "Ux (m)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Uy:
                            outputColumnName = "Uy (m)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Uz:
                            outputColumnName = "Uz (m)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Rx:
                            outputColumnName = "Rx (rad)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Ry:
                            outputColumnName = "Ry (rad)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Rz:
                            outputColumnName = "Rz (rad)";
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_UTotal:
                            outputColumnName = "U Tot (m)";
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_S1:
                            outputColumnName = "Stress - Principal 1 (Pa)";
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_S2:
                            outputColumnName = "Stress - Principal 2 (Pa)";
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_S3:
                            outputColumnName = "Stress - Principal 3 (Pa)";
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_SInt:
                            outputColumnName = "Stress - Intensity (Pa)";
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_SEqv:
                            outputColumnName = "Stress - Von-Mises (Pa)";
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                            outputColumnName = "Strain - Principal 1";
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                            outputColumnName = "Strain - Principal 2";
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                            outputColumnName = "Strain - Principal 3";
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                            outputColumnName = "Strain - Intensity";
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                            outputColumnName = "Strain - Von-Mises";
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR:
                            outputColumnName = "Axial Strain at End";
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT:
                            outputColumnName = "Bending Strain +Y";
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB:
                            outputColumnName = "Bending Strain -Y";
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT:
                            outputColumnName = "Bending Strain +Z";
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB:
                            outputColumnName = "Bending Strain -Z";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_Fx:
                            outputColumnName = "Axial - Fx (N)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_SFy:
                            outputColumnName = "Shear - SFy (N)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_SFz:
                            outputColumnName = "Shear - SFz (N)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_Tq:
                            outputColumnName = "Torque - Mx (Nm)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_My:
                            outputColumnName = "Moment - My (Nm)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_Mz:
                            outputColumnName = "Moment - Mz (Nm)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Ex:
                            outputColumnName = "Axial - Ex";
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Ky:
                            outputColumnName = "Curvature - y";
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Kz:
                            outputColumnName = "Curvature - z";
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_SEz:
                            outputColumnName = "Shear - SEy";
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_SEy:
                            outputColumnName = "Shear - SEz";
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SDir:
                            outputColumnName = "Axial Direct Stress (Pa)";
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SByT:
                            outputColumnName = "Bending Stress +Y";
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SByB:
                            outputColumnName = "Bending Stress -Y";
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                            outputColumnName = "Bending Stress +Z";
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SBzB:
                            outputColumnName = "Bending Stress -Z";
                            break;

                        case FeResultTypeEnum.ElementNodal_CodeCheck:
                            outputColumnName = "Ratio";
                            break;

                        case FeResultTypeEnum.Element_StrainEnergy:
                            outputColumnName = "Strain Energy";
                            break;

                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            outputColumnName = "Multiplier";
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // It is a Grasshopper DoubleList 
                else if (IsGhDoubleList) outputColumnName = "Double Value";

                else outputColumnName = null;

                return outputColumnName;
            }
        }


        #region Constraint Calculations
        // Must be set here in the Problem Quantity because they exists BEFORE the iterations start and thus may be used to hold the constraint functions
        public double NlOptEntryPoint_ConstraintFunction(double[] inValues, double[] inGradient)
        {
            // The inValues are ignored because it is better to go directly to the CurrentCalc_SolutionPoint of the problem
            SolutionPoint solPoint = _owner.WpfSelectedProblem.CurrentCalc_SolutionPoint;

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
                        //case Quantity_ConstraintObjectiveEnum.EqualTo: // |fc(x)| <= Tolerance
                        //    xa = Math.Abs(solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue);
                        //    xb = Math.Abs(solPoint.GradientSolutionPoints[i].ConstraintEvals[this] - ConstraintObjective_CompareValue);
                        //    break;
                        case Quantity_ConstraintObjectiveEnum.EqualTo: // fc(x) - Tolerance = 0
                        case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual: // fc(x) <= Tolerance
                            xa = solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue;
                            xb = solPoint.GradientSolutionPoints[i].ConstraintEvals[this] - ConstraintObjective_CompareValue;
                            break;

                        case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual: // // fc(x) <= Tolerance
                            xa = -(solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue);
                            xb = -(solPoint.GradientSolutionPoints[i].ConstraintEvals[this] - ConstraintObjective_CompareValue);
                            break;

                        default:
                            return double.NaN;
                    }

                    inGradient[i] = (xb - xa) / (b - a);

                }
                // Adds a copy to the solution point's records
                solPoint.ConstraintGradients.Add(this, inGradient.Clone() as double[]);
            }

            // The quantity itself depends on the constraint objective and value
            switch (ConstraintObjective)
            {
                //case Quantity_ConstraintObjectiveEnum.EqualTo: // |fc(x)| <= Tolerance
                //    return Math.Abs(solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue);

                case Quantity_ConstraintObjectiveEnum.EqualTo: // fc(x) - Tolerance = 0
                case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual: // fc(x) <= Tolerance
                    return solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue;

                case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual: // // fc(x) <= Tolerance
                    return -(solPoint.ConstraintEvals[this] - ConstraintObjective_CompareValue);

                default:
                    return double.NaN;
            }
        }
        #endregion
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
