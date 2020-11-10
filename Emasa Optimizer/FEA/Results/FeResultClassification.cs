using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultClassification : BindableBase, IProblemQuantitySource
    {
        public FeResultClassification(FeResultTypeEnum inResultType, bool inOutputDataIsSelected, FeAnalysisShapeEnum inShape)
        {
            ResultType = inResultType; // Will set also the family and result location
            _outputDataIsSelected = inOutputDataIsSelected;
            _targetShape = inShape;
        }

        private FeResultFamilyEnum _resultFamily;
        public FeResultFamilyEnum ResultFamily
        {
            get => _resultFamily;
            set => SetProperty(ref _resultFamily, value);
        }
        
        private FeResultTypeEnum _resultType;
        public FeResultTypeEnum ResultType
        {
            get => _resultType;
            set
            {
                SetProperty(ref _resultType, value);

                switch (value)
                {
                    case FeResultTypeEnum.Nodal_Reaction_Fx:
                    case FeResultTypeEnum.Nodal_Reaction_My:
                    case FeResultTypeEnum.Nodal_Reaction_Mz:
                    case FeResultTypeEnum.Nodal_Reaction_Mx:
                    case FeResultTypeEnum.Nodal_Reaction_Fz:
                    case FeResultTypeEnum.Nodal_Reaction_Fy:
                        ResultFamily = FeResultFamilyEnum.Nodal_Reaction;
                        ResultLocation = FeResultLocationEnum.Node;
                        break;

                    case FeResultTypeEnum.Nodal_Displacement_Ux:
                    case FeResultTypeEnum.Nodal_Displacement_Uy:
                    case FeResultTypeEnum.Nodal_Displacement_Uz:
                    case FeResultTypeEnum.Nodal_Displacement_Rx:
                    case FeResultTypeEnum.Nodal_Displacement_Ry:
                    case FeResultTypeEnum.Nodal_Displacement_Rz:
                    case FeResultTypeEnum.Nodal_Displacement_UTotal:
                        ResultFamily = FeResultFamilyEnum.Nodal_Displacement;
                        ResultLocation = FeResultLocationEnum.Node;
                        break;

                    case FeResultTypeEnum.SectionNode_Stress_S1:
                    case FeResultTypeEnum.SectionNode_Stress_S2:
                    case FeResultTypeEnum.SectionNode_Stress_S3:
                    case FeResultTypeEnum.SectionNode_Stress_SInt:
                    case FeResultTypeEnum.SectionNode_Stress_SEqv:
                        ResultFamily = FeResultFamilyEnum.SectionNode_Stress;
                        ResultLocation = FeResultLocationEnum.SectionNode;
                        break;

                    case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                        ResultFamily = FeResultFamilyEnum.SectionNode_Strain;
                        ResultLocation = FeResultLocationEnum.SectionNode;
                        break;

                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB:
                        ResultFamily = FeResultFamilyEnum.ElementNodal_BendingStrain;
                        ResultLocation = FeResultLocationEnum.ElementNode;
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_Fx:
                    case FeResultTypeEnum.ElementNodal_Force_My:
                    case FeResultTypeEnum.ElementNodal_Force_Mz:
                    case FeResultTypeEnum.ElementNodal_Force_Tq:
                    case FeResultTypeEnum.ElementNodal_Force_SFz:
                    case FeResultTypeEnum.ElementNodal_Force_SFy:
                        ResultFamily = FeResultFamilyEnum.ElementNodal_Force;
                        ResultLocation = FeResultLocationEnum.ElementNode;
                        break;

                    case FeResultTypeEnum.ElementNodal_Strain_Ex:
                    case FeResultTypeEnum.ElementNodal_Strain_Ky:
                    case FeResultTypeEnum.ElementNodal_Strain_Kz:
                    case FeResultTypeEnum.ElementNodal_Strain_Te:
                    case FeResultTypeEnum.ElementNodal_Strain_SEz:
                    case FeResultTypeEnum.ElementNodal_Strain_SEy:
                        ResultFamily = FeResultFamilyEnum.ElementNodal_Strain;
                        ResultLocation = FeResultLocationEnum.ElementNode;
                        break;

                    case FeResultTypeEnum.ElementNodal_Stress_SDir:
                    case FeResultTypeEnum.ElementNodal_Stress_SByT:
                    case FeResultTypeEnum.ElementNodal_Stress_SByB:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzB:
                        ResultFamily = FeResultFamilyEnum.ElementNodal_Stress;
                        ResultLocation = FeResultLocationEnum.ElementNode;
                        break;

                    case FeResultTypeEnum.ElementNodal_CodeCheck:
                        ResultFamily = FeResultFamilyEnum.Others;
                        ResultLocation = FeResultLocationEnum.ElementNode;
                        break;

                    case FeResultTypeEnum.Element_StrainEnergy:
                        ResultFamily = FeResultFamilyEnum.Others;
                        ResultLocation = FeResultLocationEnum.Element;
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                        ResultFamily = FeResultFamilyEnum.Others;
                        ResultLocation = FeResultLocationEnum.Model;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
        
        private FeResultLocationEnum _resultLocation;
        public FeResultLocationEnum ResultLocation
        {
            get => _resultLocation;
            set => SetProperty(ref _resultLocation, value);
        }

        private bool _outputDataIsSelected;
        public bool OutputData_IsSelected
        {
            get => _outputDataIsSelected;
            set => SetProperty(ref _outputDataIsSelected, value);
        }

        private FeAnalysisShapeEnum _targetShape;
        public FeAnalysisShapeEnum TargetShape
        {
            get => _targetShape;
            set => SetProperty(ref _targetShape, value);
        }

        public bool IsEigenValueBuckling => ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor ||
                                            ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor ||
                                            ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor;

        public bool IsSupportedByCurrentSolver
        {
            get
            {
                // TODO: Will need to be improved if different FeSolvers are added.
                if (AppSS.I.FeOpt != null) return AppSS.I.FeOpt.FeSolverType_Selected == FeSolverTypeEnum.Ansys;
                else return true; // The default is Ansys
            }
        }

        // Strings that define this element for various contexts
        public string ResultFileName
        {
            get
            {
                switch (ResultFamily)
                {
                    case FeResultFamilyEnum.Nodal_Reaction:
                    case FeResultFamilyEnum.Nodal_Displacement:
                    case FeResultFamilyEnum.SectionNode_Stress:
                    case FeResultFamilyEnum.SectionNode_Strain:
                    case FeResultFamilyEnum.ElementNodal_BendingStrain:
                    case FeResultFamilyEnum.ElementNodal_Force:
                    case FeResultFamilyEnum.ElementNodal_Strain:
                    case FeResultFamilyEnum.ElementNodal_Stress:
                        return $"ems_output_{TargetShape}_{ResultFamily}";

                    case FeResultFamilyEnum.Others:
                        return $"ems_output_{TargetShape}_{ResultFamily}_{ResultType}";

                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }
        }
        public string ScreenShotFileName => $"ems_image_{TargetShape}_{ResultType}";

        public string DataTableName
        {
            get
            {
                switch (ResultType)
                {
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                        return $"{ListDescSH.I.FeAnalysisShapeEnumDescriptions[TargetShape].Item1} - {ListDescSH.I.FeResultFamilyEnumDescriptions[ResultFamily].Item1} - EV Blk Modes";

                    default:
                        return ToString();
                }
            }
        }

        public string Wpf_ProblemQuantityName => $"{ListDescSH.I.FeResultLocationEnumDescriptions[ResultLocation].Item1} - {ListDescSH.I.FeResultTypeEnumDescriptions[ResultType].Item1}";
        public string Wpf_ProblemQuantityGroup => $"{ListDescSH.I.FeAnalysisShapeEnumDescriptions[TargetShape].Item1} - {ListDescSH.I.FeResultFamilyEnumDescriptions[ResultFamily].Item1}";
        public string Wpf_Explanation => $@"Target Shape: {ListDescSH.I.FeAnalysisShapeEnumDescriptions[TargetShape].Item2}
Family: {ListDescSH.I.FeResultFamilyEnumDescriptions[ResultFamily].Item2}
Result:  {ListDescSH.I.FeResultTypeEnumDescriptions[ResultType].Item2}
Location: {ListDescSH.I.FeResultLocationEnumDescriptions[ResultLocation].Item2}";


        #region IProblemQuantitySource
        public bool IsGhGeometryDoubleListData => false;
        public bool IsFiniteElementData => true;

        public void AddProblemQuantity_FunctionObjective()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize));
        }
        public void AddProblemQuantity_ConstraintObjective()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.Constraint));
        }
        public void AddProblemQuantity_OutputOnly()
        {
            AppSS.I.ProbQuantMgn.AddProblemQuantity(new ProblemQuantity(this, Quantity_TreatmentTypeEnum.OutputOnly));
        }
        #endregion

        public override string ToString()
        {
            return $"{Wpf_ProblemQuantityGroup} - {Wpf_ProblemQuantityName}";
        }
    }

    public enum FeAnalysisShapeEnum
    {
        PerfectShape,
        ImperfectShape_FullStiffness,
        ImperfectShape_Softened
    }
    public enum FeResultFamilyEnum
    {
        Nodal_Reaction,
        Nodal_Displacement,

        SectionNode_Stress,
        SectionNode_Strain,

        ElementNodal_BendingStrain,
        ElementNodal_Force,
        ElementNodal_Strain,
        ElementNodal_Stress,

        Others,
    }
    public enum FeResultTypeEnum
    {
        // Family: Nodal Reaction
        Nodal_Reaction_Fx,
        Nodal_Reaction_Fy,
        Nodal_Reaction_Fz,
        Nodal_Reaction_Mx,
        Nodal_Reaction_My,
        Nodal_Reaction_Mz,

        // Family: Nodal_Displacement
        Nodal_Displacement_Ux,
        Nodal_Displacement_Uy,
        Nodal_Displacement_Uz,
        Nodal_Displacement_Rx,
        Nodal_Displacement_Ry,
        Nodal_Displacement_Rz,
        Nodal_Displacement_UTotal,

        // Family: SectionNode_Stress
        SectionNode_Stress_S1,
        SectionNode_Stress_S2,
        SectionNode_Stress_S3,
        SectionNode_Stress_SInt,
        SectionNode_Stress_SEqv,

        // Family: SectionNode_Strain
        SectionNode_Strain_EPTT1,
        SectionNode_Strain_EPTT2,
        SectionNode_Strain_EPTT3,
        SectionNode_Strain_EPTTInt,
        SectionNode_Strain_EPTTEqv,

        // Family: ElementNodal_BendingStrain
        ElementNodal_BendingStrain_EPELDIR,
        ElementNodal_BendingStrain_EPELByT,
        ElementNodal_BendingStrain_EPELByB,
        ElementNodal_BendingStrain_EPELBzT,
        ElementNodal_BendingStrain_EPELBzB,

        // Family: ElementNodal_Force
        ElementNodal_Force_Fx,
        ElementNodal_Force_SFy,
        ElementNodal_Force_SFz,
        ElementNodal_Force_Tq,
        ElementNodal_Force_My,
        ElementNodal_Force_Mz,
        
        // Family: ElementNodal_Strain
        ElementNodal_Strain_Ex,
        ElementNodal_Strain_Ky,
        ElementNodal_Strain_Kz,
        ElementNodal_Strain_SEz,
        ElementNodal_Strain_SEy,
        ElementNodal_Strain_Te,

        // Family: ElementNodal_Stress
        ElementNodal_Stress_SDir,
        ElementNodal_Stress_SByT,
        ElementNodal_Stress_SByB,
        ElementNodal_Stress_SBzT,
        ElementNodal_Stress_SBzB,

        // Family: Other
        ElementNodal_CodeCheck,
        Element_StrainEnergy,
        Model_EigenvalueBuckling_Mode1Factor,
        Model_EigenvalueBuckling_Mode2Factor,
        Model_EigenvalueBuckling_Mode3Factor,
    }

    public enum FeResultLocationEnum
    {
        Model,
        Node,
        ElementNode,
        SectionNode,
        Element,
    }
}
