using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Properties;
using Emasa_Optimizer.WpfResources;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA
{
    public class FeOptions : BindableBase
    {
        [NotNull] private readonly SolveManager _owner;

        public FeOptions([NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            // Creates the ResultClass Selection List
            _resultOutputs = new FastObservableCollection<FeResultClassification>()
                {
                // Perfect
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Tq, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, true, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S1, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S2, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S3, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SInt, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SEqv, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT1, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT2, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT3, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTInt, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTEqv, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, true, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, true, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, true, FeAnalysisShapeEnum.PerfectShape),


                // Imperfect - Full Stiffness
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Tq, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, true, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S1, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S2, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S3, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SInt, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SEqv, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT1, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT2, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT3, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTInt, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTEqv, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),


                // Imperfect - Softened
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Tq, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_SFy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S1, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S2, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S3, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SInt, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SEqv, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT1, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT2, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT3, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTInt, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTEqv, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, true, FeAnalysisShapeEnum.ImperfectShape_Softened),
                };

            // Adds a listener to all the changes in any of the ResultsSelectStatuses
            foreach (FeResultClassification feResult in _resultOutputs)
            {
                feResult.PropertyChanged += FeResultOnPropertyChanged;
            }

            // Creates the view for the Perfect Results
            CollectionViewSource allResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            WpfResults = allResults_cvs.View;
            WpfResults.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedBySolver(FeSolverType_Selected));
            WpfResults.GroupDescriptions.Add(new PropertyGroupDescription("TargetShape"));
            WpfResults.GroupDescriptions.Add(new PropertyGroupDescription("ResultFamily"));
            WpfResults.SortDescriptions.Add(new SortDescription("TargetShape", ListSortDirection.Ascending));
            WpfResults.SortDescriptions.Add(new SortDescription("ResultFamily", ListSortDirection.Ascending));
            WpfResults.SortDescriptions.Add(new SortDescription("ResultType", ListSortDirection.Ascending));

            // Creates the view for the Perfect Results
            CollectionViewSource perfectResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            WpfPerfectShapeResults = perfectResults_cvs.View;
            WpfPerfectShapeResults.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedBySolver(FeSolverType_Selected) && item.TargetShape == FeAnalysisShapeEnum.PerfectShape);


            // Creates the view for the Imperfect Full Stiffness Results
            CollectionViewSource imperfectFullStiffnessResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            WpfImperfectShapeFullStiffnessResults = imperfectFullStiffnessResults_cvs.View;
            WpfImperfectShapeFullStiffnessResults.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedBySolver(FeSolverType_Selected) && item.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness);

            // Creates the view for the Imperfect Softened Results
            CollectionViewSource imperfectSoftenedResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            WpfImperfectShapeSoftenedResults = imperfectSoftenedResults_cvs.View;
            WpfImperfectShapeSoftenedResults.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedBySolver(FeSolverType_Selected) && item.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened);

            // Creates the view for the Selected Output Results
            CollectionViewSource selectedResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            WpfAllSelectedOutputResults = selectedResults_cvs.View;
            WpfAllSelectedOutputResults.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedBySolver(FeSolverType_Selected) && item.OutputData_IsSelected);
        }

        private readonly FeScreenShotOptions _screenShotOptions = new FeScreenShotOptions();
        public FeScreenShotOptions ScreenShotOptions => _screenShotOptions;
        
        #region Solver Type and Basic Options
        private FeSolverTypeEnum _feSolverType_Selected = (FeSolverTypeEnum)Enum.Parse(typeof(FeSolverTypeEnum), Settings.Default.Default_FeSolverType);
        public FeSolverTypeEnum FeSolverType_Selected
        {
            get => _feSolverType_Selected;
            set
            {
                SetProperty(ref _feSolverType_Selected, value);
            }
        }

        public Dictionary<FeSolverTypeEnum, string> WfpCaption_FeSolverTypeEnum => ListDescriptionStaticHolder.ListDescSingleton.FeSolverTypeEnumStaticDescriptions;

        private int _eigenvalueBuckling_ShapesToCapture = Settings.Default.Default_Model_EigenvalueBuckling_ShapesToCapture;
        public int EigenvalueBuckling_ShapesToCapture
        {
            get => _eigenvalueBuckling_ShapesToCapture;
            set => SetProperty(ref _eigenvalueBuckling_ShapesToCapture, value);
        }
        #endregion
        
        #region Gravity Loads
        private bool _gravity_IsLoad = Properties.Settings.Default.Default_AddGravityLoad;
        public bool Gravity_IsLoad
        {
            get => _gravity_IsLoad;
            set => SetProperty(ref _gravity_IsLoad, value);
        }

        private double _gravity_Multiplier = Properties.Settings.Default.Default_AddGravityLoad_Multiplier;
        public double Gravity_Multiplier
        {
            get => _gravity_Multiplier;
            set => SetProperty(ref _gravity_Multiplier, value);
        }

        private MainAxisDirectionEnum _gravityDirectionEnum_Selected = (MainAxisDirectionEnum)Enum.Parse(typeof(MainAxisDirectionEnum), Settings.Default.Default_AddGravityLoad_Direction);
        public MainAxisDirectionEnum Gravity_DirectionEnum_Selected
        {
            get => _gravityDirectionEnum_Selected;
            set => SetProperty(ref _gravityDirectionEnum_Selected, value);
        }

        public Dictionary<MainAxisDirectionEnum, string> WfpCaption_MainAxisDirectionEnum => ListDescriptionStaticHolder.ListDescSingleton.MainAxisDirectionEnumStaticDescriptions;
        #endregion

        #region Imperfect Shape - Mode and Multiplier
        private int _imperfect_EigenvalueBucklingMode = Settings.Default.Default_Imperfect_Model_EigenvalueBucklingMode;
        public int Imperfect_EigenvalueBucklingMode
        {
            get => _imperfect_EigenvalueBucklingMode;
            set => SetProperty(ref _imperfect_EigenvalueBucklingMode, value);
        }

        private bool _imperfect_MultiplierFromBoundingBox = Settings.Default.Default_Imperfect_Multiplier_FromJoints_BoundingBox;
        public bool Imperfect_MultiplierFromBoundingBox
        {
            get => _imperfect_MultiplierFromBoundingBox;
            set => SetProperty(ref _imperfect_MultiplierFromBoundingBox, value);
        }

        private double _imperfect_Multiplier = Settings.Default.Default_Imperfect_Multiplier;
        public double Imperfect_Multiplier
        {
            get => _imperfect_Multiplier;
            set => SetProperty(ref _imperfect_Multiplier, value);
        }
        #endregion

        #region Large Deflections
        private bool _largeDeflections_IsSet = Settings.Default.Default_LargeDeflections_IsSet;
        public bool LargeDeflections_IsSet
        {
            get => _largeDeflections_IsSet;
            set => SetProperty(ref _largeDeflections_IsSet, value);
        }
        private bool _largeDeflections_IsCheckBoxEnabled = Settings.Default.Default_LargeDeflections_IsCheckBoxEnabled;
        public bool LargeDeflections_IsCheckBoxEnabled
        {
            get => _largeDeflections_IsCheckBoxEnabled;
            set => SetProperty(ref _largeDeflections_IsCheckBoxEnabled, value);
        }
        #endregion

        #region FeMesh
        private int _mesh_ElementsPerFrame = Settings.Default.Default_Mesh_ElementsPerFrame;
        public int Mesh_ElementsPerFrame
        {
            get => _mesh_ElementsPerFrame;
            set => SetProperty(ref _mesh_ElementsPerFrame, value);
        }
        #endregion

        #region ResultClass Output
        private readonly FastObservableCollection<FeResultClassification> _resultOutputs;
        public ICollectionView WpfResults { get; private set; }
        public ICollectionView WpfPerfectShapeResults { get; private set; }
        public ICollectionView WpfImperfectShapeFullStiffnessResults { get; private set; }
        public ICollectionView WpfImperfectShapeSoftenedResults { get; private set; }
        public ICollectionView WpfAllSelectedOutputResults { get; private set; }
        public List<FeResultClassification> SelectedOutputResults => WpfAllSelectedOutputResults.OfType<FeResultClassification>().ToList();

        private void FeResultOnPropertyChanged(object inSender, PropertyChangedEventArgs inE)
        {
            // Updates the selected result display 
            WpfResults.Refresh();

            bool hasPerfectShape_EigenvalueBuckling = _owner.FeOptions.SelectedOutputResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape && 
                                                                                                      a.IsEigenValueBuckling);

            bool hasImperfectFullStiffness = _owner.FeOptions.SelectedOutputResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness);

            bool hasImperfectSoftened = _owner.FeOptions.SelectedOutputResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened);

            WpfIsPerfectShape_EigenvalueBucking_Required = hasPerfectShape_EigenvalueBuckling || hasImperfectFullStiffness || hasImperfectSoftened;

            
            if (hasImperfectFullStiffness)
            {
                WpfIsImperfectShapeFullStiffness_StaticAnalysis_Required = true;

                bool has_EigenvalueBuckling = _owner.FeOptions.SelectedOutputResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                              a.IsEigenValueBuckling);

                WpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required = has_EigenvalueBuckling;
            }
            else // Nothing imperfect FullStiffness is required
            {
                WpfIsImperfectShapeFullStiffness_StaticAnalysis_Required = false;
                WpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required = false;
            }


            if (hasImperfectSoftened)
            {
                WpfIsImperfectShapeSoftened_StaticAnalysis_Required = true;

                bool has_EigenvalueBuckling = _owner.FeOptions.SelectedOutputResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                              a.IsEigenValueBuckling);

                WpfIsImperfectShapeSoftened_EigenvalueBuckling_Required = has_EigenvalueBuckling;
            }
            else // Nothing imperfect Softened is required
            {
                WpfIsImperfectShapeSoftened_StaticAnalysis_Required = false;
                WpfIsImperfectShapeSoftened_EigenvalueBuckling_Required = false;
            }
        }

        private bool _wpfIsPerfectShape_EigenvalueBucking_Required;
        public bool WpfIsPerfectShape_EigenvalueBucking_Required
        {
            get => _wpfIsPerfectShape_EigenvalueBucking_Required;
            set => SetProperty(ref _wpfIsPerfectShape_EigenvalueBucking_Required, value);
        }

        private bool _wpfIsImperfectShapeFullStiffness_StaticAnalysis_Required;
        public bool WpfIsImperfectShapeFullStiffness_StaticAnalysis_Required
        {
            get => _wpfIsImperfectShapeFullStiffness_StaticAnalysis_Required;
            set => SetProperty(ref _wpfIsImperfectShapeFullStiffness_StaticAnalysis_Required, value);
        }

        private bool _wpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required;
        public bool WpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required
        {
            get => _wpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required;
            set => SetProperty(ref _wpfIsImperfectShapeFullStiffness_EigenvalueBuckling_Required, value);
        }

        private bool _wpfIsImperfectShapeSoftened_StaticAnalysis_Required;
        public bool WpfIsImperfectShapeSoftened_StaticAnalysis_Required
        {
            get => _wpfIsImperfectShapeSoftened_StaticAnalysis_Required;
            set => SetProperty(ref _wpfIsImperfectShapeSoftened_StaticAnalysis_Required, value);
        }

        private bool _wpfIsImperfectShapeSoftened_EigenvalueBuckling_Required;
        public bool WpfIsImperfectShapeSoftened_EigenvalueBuckling_Required
        {
            get => _wpfIsImperfectShapeSoftened_EigenvalueBuckling_Required;
            set => SetProperty(ref _wpfIsImperfectShapeSoftened_EigenvalueBuckling_Required, value);
        }

        #endregion
    }
    
    public enum FeSolverTypeEnum
    {
        Ansys,
        // Sap2000
    }
    public enum MainAxisDirectionEnum
    {
        xPos,
        xNeg,

        yPos,
        yNeg,

        zPos,
        zNeg
    }
}
