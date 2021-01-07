using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ProbQuantity;
using NLoptNet;

namespace Emasa_Optimizer.WpfResources
{
    /// <summary>
    /// List Descriptions Static Holder
    /// </summary>
    public sealed class ListDescSH
    {
        #region Singleton Management
        private static ListDescSH _mainInstance;
        private static readonly object _lockThis = new object();

        private ListDescSH() { }

        /// <summary>
        /// The singleton instance
        /// </summary>
        public static ListDescSH I
        {
            get
            {
                lock (_lockThis)
                {
                    // If singleton hasn't been initialized
                    if (_mainInstance == null) _mainInstance = new ListDescSH();
                }

                return _mainInstance;
            }
        }
        #endregion

        /// <summary>
        /// First is short, second is full name
        /// </summary>
        public Dictionary<FeAnalysisShapeEnum, Tuple<string, string>> FeAnalysisShapeEnumDescriptions = new Dictionary<FeAnalysisShapeEnum, Tuple<string, string>>()
            {
                {FeAnalysisShapeEnum.PerfectShape, new Tuple<string, string>("Perfect", "Perfect")},
                {FeAnalysisShapeEnum.ImperfectShape_FullStiffness, new Tuple<string, string>("Imp. Stiff", "Imperfect/Full Stiffness")},
                {FeAnalysisShapeEnum.ImperfectShape_Softened, new Tuple<string, string>("Imp. Soft", "Imperfect/Softened")},
            };
        /// <summary>
        /// First is short, second is full name
        /// </summary>
        public Dictionary<FeResultFamilyEnum, Tuple<string, string>> FeResultFamilyEnumDescriptions = new Dictionary<FeResultFamilyEnum, Tuple<string, string>>()
            {
                {FeResultFamilyEnum.Nodal_Reaction, new Tuple<string, string>("Reaction", "Nodal Reaction")},
                {FeResultFamilyEnum.Nodal_Displacement, new Tuple<string, string>("Displacement", "Nodal Displacement")},

                {FeResultFamilyEnum.SectionNode_Stress, new Tuple<string, string>("Section Stress", "Section Nodal Stress (Tensor)")},
                {FeResultFamilyEnum.SectionNode_Strain, new Tuple<string, string>("Section Strain", "Section Nodal Strain (Tensor)")},

                {FeResultFamilyEnum.ElementNodal_Force, new Tuple<string, string>("Force", "Element Nodal Force (Stress Resultant)")},
                {FeResultFamilyEnum.ElementNodal_Strain, new Tuple<string, string>("Strain", "Element Nodal Strain (Strain Resultant)")},

                {FeResultFamilyEnum.ElementNodal_Stress, new Tuple<string, string>("Linearized  Stress", "Element Nodal Linearized Stress")},
                {FeResultFamilyEnum.ElementNodal_BendingStrain, new Tuple<string, string>("Linearized Strain", "Element Nodal Linearized Strain")},
                
                {FeResultFamilyEnum.Others, new Tuple<string, string>("Others", "Others")},
            };
        /// <summary>
        /// First is short, second is full name
        /// </summary>
        public Dictionary<FeResultTypeEnum, Tuple<string, string>> FeResultTypeEnumDescriptions = new Dictionary<FeResultTypeEnum, Tuple<string, string>>()
            {
                {FeResultTypeEnum.Nodal_Reaction_Fx, new Tuple<string, string>("RFx", "Reaction - Force X")},
                {FeResultTypeEnum.Nodal_Reaction_Fy, new Tuple<string, string>("RFy", "Reaction - Force Y")},
                {FeResultTypeEnum.Nodal_Reaction_Fz, new Tuple<string, string>("RFz", "Reaction - Force Z")},
                {FeResultTypeEnum.Nodal_Reaction_Mx, new Tuple<string, string>("RMx", "Reaction - Moment X")},
                {FeResultTypeEnum.Nodal_Reaction_My, new Tuple<string, string>("RMy", "Reaction - Moment Y")},
                {FeResultTypeEnum.Nodal_Reaction_Mz, new Tuple<string, string>("RMz", "Reaction - Moment Z")},

                {FeResultTypeEnum.Nodal_Displacement_Ux, new Tuple<string, string>("Ux", "Displacement - Δ X")},
                {FeResultTypeEnum.Nodal_Displacement_Uy, new Tuple<string, string>("Uy", "Displacement - Δ Y")},
                {FeResultTypeEnum.Nodal_Displacement_Uz, new Tuple<string, string>("Uz", "Displacement - Δ Z")},
                {FeResultTypeEnum.Nodal_Displacement_Rx, new Tuple<string, string>("Rx", "Displacement - Rot X")},
                {FeResultTypeEnum.Nodal_Displacement_Ry, new Tuple<string, string>("Ry", "Displacement - Rot Y")},
                {FeResultTypeEnum.Nodal_Displacement_Rz, new Tuple<string, string>("Rz", "Displacement - Rot Z")},
                {FeResultTypeEnum.Nodal_Displacement_UTotal, new Tuple<string, string>("UΔ", "Displacement - Δ Abs")},

                {FeResultTypeEnum.SectionNode_Stress_S1, new Tuple<string, string>("σ 1", "Stress - Principal 1")},
                {FeResultTypeEnum.SectionNode_Stress_S2, new Tuple<string, string>("σ 2", "Stress Principal 2")},
                {FeResultTypeEnum.SectionNode_Stress_S3, new Tuple<string, string>("σ 3", "Stress Principal 3")},
                {FeResultTypeEnum.SectionNode_Stress_SInt, new Tuple<string, string>("σ Int", "Stress Intensity")},
                {FeResultTypeEnum.SectionNode_Stress_SEqv, new Tuple<string, string>("σ Eqv", "Stress Von-Mises")},

                {FeResultTypeEnum.SectionNode_Strain_EPTT1, new Tuple<string, string>("εT 1", "Total Mechanical Strain (Elastic + Plastic) - Principal 1")},
                {FeResultTypeEnum.SectionNode_Strain_EPTT2, new Tuple<string, string>("εT 2", "Total Mechanical Strain (Elastic + Plastic) - Principal 2")},
                {FeResultTypeEnum.SectionNode_Strain_EPTT3, new Tuple<string, string>("εT 3", "Total Mechanical Strain (Elastic + Plastic) - Principal 3")},
                {FeResultTypeEnum.SectionNode_Strain_EPTTInt, new Tuple<string, string>("εT Int", "Total Mechanical Strain (Elastic + Plastic) - Intensity")},
                {FeResultTypeEnum.SectionNode_Strain_EPTTEqv, new Tuple<string, string>("εT Eqv", "Total Mechanical Strain (Elastic + Plastic) - Von Mises")},

                {FeResultTypeEnum.ElementNodal_Force_Fx, new Tuple<string, string>("Fx", "Axial force - X")},
                {FeResultTypeEnum.ElementNodal_Force_SFy, new Tuple<string, string>("Fy", "Shear force - Y")},
                {FeResultTypeEnum.ElementNodal_Force_SFz, new Tuple<string, string>("Fz", "Shear force - Z")},
                {FeResultTypeEnum.ElementNodal_Force_Tq, new Tuple<string, string>("Mx", "Torsional moment - X")},
                {FeResultTypeEnum.ElementNodal_Force_My, new Tuple<string, string>("My", "Bending moment - Y")},
                {FeResultTypeEnum.ElementNodal_Force_Mz, new Tuple<string, string>("Mz", "Bending moment - Z")},
                
                {FeResultTypeEnum.ElementNodal_Strain_Ex, new Tuple<string, string>("εx", "Axial strain - X")},
                {FeResultTypeEnum.ElementNodal_Strain_SEy, new Tuple<string, string>("εy", "Shear strain - Y")},
                {FeResultTypeEnum.ElementNodal_Strain_SEz, new Tuple<string, string>("εz", "Shear strain - Z")},
                {FeResultTypeEnum.ElementNodal_Strain_Te, new Tuple<string, string>("Tε", "Torsional strain - X")},
                {FeResultTypeEnum.ElementNodal_Strain_Ky, new Tuple<string, string>("Ky", "Curvature - Y")},
                {FeResultTypeEnum.ElementNodal_Strain_Kz, new Tuple<string, string>("Kz", "Curvature - Z")},

                {FeResultTypeEnum.ElementNodal_Stress_SDir, new Tuple<string, string>("σ Dir", "Axial direct stress [Fx/A]")},
                {FeResultTypeEnum.ElementNodal_Stress_SByT, new Tuple<string, string>("σ Ben+Y", "Bending stress on the element +Y side of the beam [-Mz * ymax / Izz]")},
                {FeResultTypeEnum.ElementNodal_Stress_SByB, new Tuple<string, string>("σ Ben-Y", "Bending stress on the element -Y side of the beam [-Mz * ymin / Izz]")},
                {FeResultTypeEnum.ElementNodal_Stress_SBzT, new Tuple<string, string>("σ Ben+Z", "Bending stress on the element +Z side of the beam [My * zmax / Iyy]")},
                {FeResultTypeEnum.ElementNodal_Stress_SBzB, new Tuple<string, string>("σ Ben-Z", "Bending stress on the element -Z side of the beam [My * zmin / Iyy]")},

                {FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, new Tuple<string, string>("ε Dir", "Axial strain at the end [Ex]")},
                {FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, new Tuple<string, string>("ε Ben+Y", "Bending strain on the element +Y side of the beam [-Kz * ymax]")},
                {FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, new Tuple<string, string>("ε Ben-Y", "Bending strain on the element -Y side of the beam [-Kz * ymin]")},
                {FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, new Tuple<string, string>("ε Ben+Z", "Bending strain on the element +Z side of the beam [Ky * zmax]")},
                {FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, new Tuple<string, string>("ε Ben-Z", "Bending strain on the element -Z side of the beam [Ky * zmin]")},
                
                {FeResultTypeEnum.ElementNodal_CodeCheck, new Tuple<string, string>("Code Check", "Code Check")},
                {FeResultTypeEnum.Element_StrainEnergy, new Tuple<string, string>("Strain Energy", "Strain Energy")},
                {FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, new Tuple<string, string>("EVB M1", "Eigenvalue Buckling Factor - Mode 1")},
                {FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, new Tuple<string, string>("EVB M2", "Eigenvalue Buckling Factor - Mode 2")},
                {FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, new Tuple<string, string>("EVB M3", "Eigenvalue Buckling Factor - Mode 3")},
            };
        /// <summary>
        /// First is short, second is full name
        /// </summary>
        public Dictionary<FeResultLocationEnum, Tuple<string,string>> FeResultLocationEnumDescriptions = new Dictionary<FeResultLocationEnum, Tuple<string, string>>()
            {
                {FeResultLocationEnum.Node, new Tuple<string, string>("N", "Node")},
                {FeResultLocationEnum.Element, new Tuple<string, string>("E", "Element")},
                {FeResultLocationEnum.ElementNode, new Tuple<string, string>("EN", "Element Nodal")},
                {FeResultLocationEnum.SectionNode, new Tuple<string, string>("SN", "Section Nodal")},
                {FeResultLocationEnum.Model, new Tuple<string, string>("M", "Model")},
            };


        public Dictionary<StartPositionTypeEnum, string> StartPositionTypeEnumStaticDescriptions { get; private set; } = new Dictionary<StartPositionTypeEnum, string>()
            {
                {StartPositionTypeEnum.Given, "Given Value"},
                {StartPositionTypeEnum.CenterOfRange, "Center of Range"},
                {StartPositionTypeEnum.Random, "Random Within Range"},
                {StartPositionTypeEnum.PercentRandomFromCenter, "% Rand Shift from Center"},
                {StartPositionTypeEnum.PercentRandomFromGiven, "% Rand Shift from Given"}
            };
        public Dictionary<MainAxisDirectionEnum, string> MainAxisDirectionEnumStaticDescriptions = new Dictionary<MainAxisDirectionEnum, string>()
            {
                {MainAxisDirectionEnum.xPos, "+X"},
                {MainAxisDirectionEnum.xNeg, "-X"},
                {MainAxisDirectionEnum.yPos, "+Y"},
                {MainAxisDirectionEnum.yNeg, "-Y"},
                {MainAxisDirectionEnum.zPos, "+Z"},
                {MainAxisDirectionEnum.zNeg, "-Z"},
            };
        public Dictionary<FeSolverTypeEnum, string> FeSolverTypeEnumStaticDescriptions = new Dictionary<FeSolverTypeEnum, string>()
            {
                {FeSolverTypeEnum.NotFeProblem, "Not FEA"},
                {FeSolverTypeEnum.Ansys, "Ansys"},
                {FeSolverTypeEnum.Sap2000, "Sap2000"},
            };
        public Dictionary<NLoptAlgorithm, string> NlOptAlgorithmEnumDescriptions = new Dictionary<NLoptAlgorithm, string>()
            {
                {NLoptAlgorithm.LN_COBYLA, "Cobyla [LN] - E/NE"},
                {NLoptAlgorithm.LD_SLSQP, "Sequential Least-Squares Quadratic Programming [GN] - E/NE"},
                {NLoptAlgorithm.GN_ISRES, "Improved Stochastic Ranking Evolution Strategy [GN] - E/NE"},

                {NLoptAlgorithm.GN_AGS, "AGS [GN] - NE"},
                {NLoptAlgorithm.LD_MMA, "Method of Moving Asymptotes [LD] - NE"},

                {NLoptAlgorithm.LD_LBFGS ,"Low-storage BFGS [LD]"},

                {NLoptAlgorithm.GN_DIRECT, "Dividing Rectangles [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L, "Dividing Rectangles - Locally Biased [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L_RAND, "Dividing Rectangles - Locally Biased With Some Randomization [GN]"},

                {NLoptAlgorithm.GN_CRS2_LM, "Controlled Random Search With Local Mutation [GN]"},

                {NLoptAlgorithm.GN_ESCH, "ESCH (evolutionary algorithm) [GN]"},
                {NLoptAlgorithm.GD_STOGO, "StoGo [GD]"},
                {NLoptAlgorithm.GD_STOGO_RAND, "StoGo - Randomized [GD]"},
                {NLoptAlgorithm.LN_BOBYQA, "Bobyqa [LN]"},
            };

        public Dictionary<ObjectiveFunctionSumTypeEnum, Tuple<string, string>> ObjectiveFunctionSumTypeEnumNameAndDescription = new Dictionary<ObjectiveFunctionSumTypeEnum, Tuple<string, string>>()
            {
                {ObjectiveFunctionSumTypeEnum.Squares, new Tuple<string, string>("Squares", "The value of each problem quantity composing the objective function will be squared prior to being summed together.")},
                {ObjectiveFunctionSumTypeEnum.Simple, new Tuple<string, string>("Simple", "The value of each problem quantity composing the objective function will be simply summed together - no transformation.")},
            };

        public Dictionary<Quantity_FunctionObjectiveEnum, Tuple<string, double, string>> Quantity_FunctionObjectiveEnumDescriptions = new Dictionary<Quantity_FunctionObjectiveEnum, Tuple<string, double, string>>()
            {
                {Quantity_FunctionObjectiveEnum.Maximize, new Tuple<string, double, string>("➜", -80d, "Maximize")},
                {Quantity_FunctionObjectiveEnum.Minimize, new Tuple<string, double, string>("➜", 80d, "Minimize")},
                {Quantity_FunctionObjectiveEnum.Target, new Tuple<string, double, string>("➜", 0d, "Towards given target")},
            };
        public Dictionary<Quantity_TreatmentTypeEnum, string> Quantity_TreatmentTypeEnumDescriptions = new Dictionary<Quantity_TreatmentTypeEnum, string>()
            {
                {Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize, "Minimize Objective Function"},
                {Quantity_TreatmentTypeEnum.Constraint, "Constraint"},
                {Quantity_TreatmentTypeEnum.OutputOnly, "Only Output"}
            };
        public Dictionary<Quantity_ConstraintObjectiveEnum, string> Quantity_ConstraintObjectiveEnumDescriptions = new Dictionary<Quantity_ConstraintObjectiveEnum, string>()
            {
                {Quantity_ConstraintObjectiveEnum.EqualTo, "="},
                {Quantity_ConstraintObjectiveEnum.HigherThanOrEqual, "≥"},
                {Quantity_ConstraintObjectiveEnum.LowerThanOrEqual, "≤"}
            };

        public Dictionary<Quantity_AggregateTypeEnum, string> Quantity_AggregateTypeEnumDescriptions = new Dictionary<Quantity_AggregateTypeEnum, string>()
            {
                {Quantity_AggregateTypeEnum.Max, "Max"},
                {Quantity_AggregateTypeEnum.Min, "Min"},
                {Quantity_AggregateTypeEnum.Mean, "Mean"},
                {Quantity_AggregateTypeEnum.StandardDeviation, "StDev"},
                {Quantity_AggregateTypeEnum.Sum, "Sum"},
                {Quantity_AggregateTypeEnum.Product, "Prod"},
            };

        public Dictionary<ObjectiveFunctionSumTypeEnum, string> ObjectiveFunctionSumTypeEnumDescriptions = new Dictionary<ObjectiveFunctionSumTypeEnum, string>()
            {
                {ObjectiveFunctionSumTypeEnum.Simple, "Simple"},
                {ObjectiveFunctionSumTypeEnum.Squares, "Squares"}
            };

        public Dictionary<StopCriteriaTypeEnum, string> StopCriteriaTypeEnumDescriptions = new Dictionary<StopCriteriaTypeEnum, string>()
            {
                {StopCriteriaTypeEnum.Iterations, "Max. Iterations"},
                {StopCriteriaTypeEnum.Time, "Max. Time"},
                {StopCriteriaTypeEnum.FunctionValue, "F Stop Value"},
                {StopCriteriaTypeEnum.FunctionAbsoluteChange, "F Absolute Change"},
                {StopCriteriaTypeEnum.FunctionRelativeChange, "F Relative Change"},
                {StopCriteriaTypeEnum.ParameterAbsoluteChange, "X Absolute Change"},
                {StopCriteriaTypeEnum.ParameterRelativeChange, "X Relative Change"}
            };

        public Dictionary<NlOpt_OptimizeTerminationCodeEnum, string> NlOpt_TerminationCodeEnumDescriptions = new Dictionary<NlOpt_OptimizeTerminationCodeEnum, string>()
            {
                {NlOpt_OptimizeTerminationCodeEnum.NotStarted, "Not Started"},
                {NlOpt_OptimizeTerminationCodeEnum.Optimizing, "Optimizing"},

                {NlOpt_OptimizeTerminationCodeEnum.Failed, "Failed"},
                {NlOpt_OptimizeTerminationCodeEnum.Forced_Stop, "Forced Stop"},

                {NlOpt_OptimizeTerminationCodeEnum.Success, "Success"},

                {NlOpt_OptimizeTerminationCodeEnum.Converged, "Converged"},
            };


        public SolidColorBrush[] ChartAvailableColors = new[]
            {
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.DarkBlue),
            new SolidColorBrush(Colors.DarkGreen),
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Violet),
            new SolidColorBrush(Colors.Indigo),
            new SolidColorBrush(Colors.Orange),

            new SolidColorBrush(Colors.LightCoral),
            new SolidColorBrush(Colors.CornflowerBlue),
            new SolidColorBrush(Colors.LimeGreen),
            new SolidColorBrush(Colors.DimGray),
            new SolidColorBrush(Colors.DarkRed),
            new SolidColorBrush(Colors.MediumVioletRed),
            new SolidColorBrush(Colors.OrangeRed),
            };


        public Dictionary<NlOpt_Point_PhaseEnum, string> NlOpt_Point_PhaseEnumDescriptions = new Dictionary<NlOpt_Point_PhaseEnum, string>()
            {
                {NlOpt_Point_PhaseEnum.Initializing, "Initializing"},
                {NlOpt_Point_PhaseEnum.Grasshopper_Updating, "Updating Grasshopper"},
                {NlOpt_Point_PhaseEnum.FiniteElement_Running, "Running Fe Model"},
                {NlOpt_Point_PhaseEnum.Outputs_Initializing, "Initializing Outputs"},
                {NlOpt_Point_PhaseEnum.ObjectiveFunctionResult_Calculating, "Calculating Objective Function"},
                {NlOpt_Point_PhaseEnum.Gradients_Running, "Calculating Gradients"},
                {NlOpt_Point_PhaseEnum.Ended, "Ended Successfully"},
            };
    }
}

