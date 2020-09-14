using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultClassification : BindableBase
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
        public string ResultFamilyGroupName => GetFriendlyEnumName(ResultFamily);

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
                    case FeResultTypeEnum.Nodal_Reaction_Tq:
                    case FeResultTypeEnum.Nodal_Reaction_SFz:
                    case FeResultTypeEnum.Nodal_Reaction_SFy:
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
        public string ResultTypeDescription => GetFriendlyEnumName(ResultType);

        public string ResultTypeExplanation
        {
            get
            {
                // TODO: Write the correct explanation of the results
                return ResultTypeDescription;
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
        public string TargetShapeDescription => GetFriendlyEnumName(TargetShape);

        public bool IsEigenValueBuckling => ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor ||
                                            ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor ||
                                            ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor;

        public bool IsSupportedBySolver(FeSolverTypeEnum inSolver)
        {
            return inSolver == FeSolverTypeEnum.Ansys;
        }

        public string WpfFriendlyName => $"{GetFriendlyEnumName(TargetShape)} - {GetFriendlyEnumName(ResultFamily)} - {GetFriendlyEnumName(ResultType)}";

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

        #region Friendly String Helpers
        public static string GetFriendlyEnumName(FeAnalysisShapeEnum inFeAnalysisShape)
        {
            switch (inFeAnalysisShape)
            {
                case FeAnalysisShapeEnum.PerfectShape:
                    return "Perfect";

                case FeAnalysisShapeEnum.ImperfectShape_FullStiffness:
                    return "Imperfect - Full Stiffness";

                case FeAnalysisShapeEnum.ImperfectShape_Softened:
                    return "Imperfect - Softened";

                default:
                    throw new ArgumentOutOfRangeException(nameof(inFeAnalysisShape), inFeAnalysisShape, null);
            }
        }
        public static string GetFriendlyEnumName(FeResultFamilyEnum inFeResultFamily)
        {
            switch (inFeResultFamily)
            {
                case FeResultFamilyEnum.Nodal_Reaction:
                    return "Nodal Reaction";

                case FeResultFamilyEnum.Nodal_Displacement:
                    return "Nodal Displacement";

                case FeResultFamilyEnum.SectionNode_Stress:
                    return "Section Nodal Stress";

                case FeResultFamilyEnum.SectionNode_Strain:
                    return "Section Nodal Strain";

                case FeResultFamilyEnum.ElementNodal_BendingStrain:
                    return "Element Nodal Bending Strain";

                case FeResultFamilyEnum.ElementNodal_Force:
                    return "Element Nodal Force";

                case FeResultFamilyEnum.ElementNodal_Strain:
                    return "Element Nodal Strain";

                case FeResultFamilyEnum.ElementNodal_Stress:
                    return "Element Nodal Stress";

                case FeResultFamilyEnum.Others:
                    return "Others";

                default:
                    throw new ArgumentOutOfRangeException(nameof(inFeResultFamily), inFeResultFamily, null);
            }
        }
        public static string GetFriendlyEnumName(FeResultTypeEnum inFeResultType)
        {
            //return inFeResultType.ToString();
            switch (inFeResultType)
            {
                case FeResultTypeEnum.Nodal_Reaction_Fx:
                    return "Force X";
                    break;

                case FeResultTypeEnum.Nodal_Reaction_My:
                    return "Moment Y";
                    break;

                case FeResultTypeEnum.Nodal_Reaction_Mz:
                    return "Moment Z";
                    break;

                case FeResultTypeEnum.Nodal_Reaction_Tq:
                    return "Moment X";
                    break;

                case FeResultTypeEnum.Nodal_Reaction_SFz:
                    return "Force Z";
                    break;

                case FeResultTypeEnum.Nodal_Reaction_SFy:
                    return "Force Y";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Ux:
                    return "Δ X";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Uy:
                    return "Δ Y";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Uz:
                    return "Δ Z";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Rx:
                    return "Rot X";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Ry:
                    return "Rot Y";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_Rz:
                    return "Rot Z";
                    break;

                case FeResultTypeEnum.Nodal_Displacement_UTotal:
                    return "Δ Abs";
                    break;

                case FeResultTypeEnum.SectionNode_Stress_S1:
                    return "Principal 1";
                    break;

                case FeResultTypeEnum.SectionNode_Stress_S2:
                    return "Principal 2";
                    break;

                case FeResultTypeEnum.SectionNode_Stress_S3:
                    return "Principal 3";
                    break;

                case FeResultTypeEnum.SectionNode_Stress_SInt:
                    return "Intensity";
                    break;

                case FeResultTypeEnum.SectionNode_Stress_SEqv:
                    return "Von-Mises";
                    break;

                case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                    return "Principal 1";
                    break;

                case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                    return "Principal 2";
                    break;

                case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                    return "Principal 3";
                    break;

                case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                    return "Intensity";
                    break;

                case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                    return "Von-Mises";
                    break;

                case FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR:
                    return "EPELDIR";
                    break;

                case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT:
                    return "EPELByT";
                    break;

                case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB:
                    return "EPELByB";
                    break;

                case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT:
                    return "EPELBzT";
                    break;

                case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB:
                    return "EPELBzB";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_Fx:
                    return "Fx - Axial";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_My:
                    return "Moment Y";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_Mz:
                    return "Moment Z";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_Tq:
                    return "Moment X";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_SFz:
                    return "Fz - Shear";
                    break;

                case FeResultTypeEnum.ElementNodal_Force_SFy:
                    return "Fy - Shear";
                    break;

                case FeResultTypeEnum.ElementNodal_Strain_Ex:
                    return "Ex - Axial";
                    break;

                case FeResultTypeEnum.ElementNodal_Strain_Ky:
                    return "Curvature Y";
                    break;

                case FeResultTypeEnum.ElementNodal_Strain_Kz:
                    return "Curvature Z";
                    break;

                case FeResultTypeEnum.ElementNodal_Strain_SEz:
                    return "Ez - Shear";
                    break;

                case FeResultTypeEnum.ElementNodal_Strain_SEy:
                    return "Ey - Shear";
                    break;

                case FeResultTypeEnum.ElementNodal_Stress_SDir:
                    return "Axial";
                    break;

                case FeResultTypeEnum.ElementNodal_Stress_SByT:
                    return "Bending +Y";
                    break;

                case FeResultTypeEnum.ElementNodal_Stress_SByB:
                    return "Bending -Y";
                    break;

                case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                    return "Bending +Z";
                    break;

                case FeResultTypeEnum.ElementNodal_Stress_SBzB:
                    return "Bending -Z";
                    break;

                case FeResultTypeEnum.ElementNodal_CodeCheck:
                    return "Code Check";
                    break;

                case FeResultTypeEnum.Element_StrainEnergy:
                    return "Strain Energy";
                    break;

                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                    return "EV Blk M1";
                    break;

                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                    return "EV Blk M2";
                    break;

                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                    return "EV Blk M2";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inFeResultType), inFeResultType, null);
            }
        }
        #endregion
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
    public class WpfEnumFeOptionsFriendlyStringValueConverter : IValueConverter
    {
        private ImageCaptureViewDirectionEnum target;

        public WpfEnumFeOptionsFriendlyStringValueConverter()
        {
        }

        // From value to WPF
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case FeResultFamilyEnum feResultFamily:
                    return FeResultClassification.GetFriendlyEnumName(feResultFamily);
                case FeResultTypeEnum feResultType:
                    return FeResultClassification.GetFriendlyEnumName(feResultType);
                case FeAnalysisShapeEnum feAnalysisShape:
                    return FeResultClassification.GetFriendlyEnumName(feAnalysisShape);
                case ImageCaptureViewDirectionEnum imageCaptureViewDirection:
                    return FeScreenShotOptions.GetFriendlyEnumName(imageCaptureViewDirection);
                default:
                    return "Error in the WpfEnumFeOptionsFriendlyStringValueConverter";
            }
        }

        // From WPF to value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string s)) throw new InvalidCastException("ResultValue is not string.");

            if (Enum.TryParse(s, out FeResultTypeEnum feResultType)) return feResultType;
            if (Enum.TryParse(s, out FeAnalysisShapeEnum feAnalysisShape)) return feAnalysisShape;
            if (Enum.TryParse(s, out FeResultFamilyEnum feResultFamily)) return feResultFamily;
            if (Enum.TryParse(s, out ImageCaptureViewDirectionEnum imageCaptureViewDirection)) return imageCaptureViewDirection;

            throw new InvalidCastException("ResultValue is not an expected enum.");

        }
    }

    public enum FeResultTypeEnum
    {
        // Family: Nodal Reaction
        Nodal_Reaction_Fx,
        Nodal_Reaction_SFy,
        Nodal_Reaction_SFz,
        Nodal_Reaction_Tq,
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
