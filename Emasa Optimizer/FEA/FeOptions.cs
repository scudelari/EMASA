extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Loads;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.Properties;
using Emasa_Optimizer.WpfResources;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;
using MPOC = MintPlayer.ObservableCollection;

namespace Emasa_Optimizer.FEA
{
    public class FeOptions : BindableBase
    {
        public FeOptions()
        {
            // Creates the ResultClass Selection List
            _resultOutputs = new MPOC.ObservableCollection<FeResultClassification>
                {
                // Perfect
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.PerfectShape),

                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, false, FeAnalysisShapeEnum.PerfectShape),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, false, FeAnalysisShapeEnum.PerfectShape),
                
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Te, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.PerfectShape),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.PerfectShape),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.PerfectShape),

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

                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, false, FeAnalysisShapeEnum.PerfectShape),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, false, FeAnalysisShapeEnum.PerfectShape),




                // ImperfectShape_FullStiffness
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Te, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),

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

                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, false, FeAnalysisShapeEnum.ImperfectShape_FullStiffness),


                // ImperfectShape_Softened
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Fz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_My, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Reaction_Mz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ux, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Uz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Ry, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_Rz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Nodal_Displacement_UTotal, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Fx, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_SFz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Tq, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_My, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Force_Mz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ex, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_SEz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Te, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Ky, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Strain_Kz, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SDir, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SByB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_Stress_SBzB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S1, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S2, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_S3, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SInt, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Stress_SEqv, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT1, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT2, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTT3, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTInt, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.SectionNode_Strain_EPTTEqv, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                new FeResultClassification(FeResultTypeEnum.ElementNodal_CodeCheck, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Element_StrainEnergy, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor, false, FeAnalysisShapeEnum.ImperfectShape_Softened),
                new FeResultClassification(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor, false, FeAnalysisShapeEnum.ImperfectShape_Softened),

                };

            // Creates the view for the user interface selection of desired output
            Wpf_AllResultsForInterface = (new CollectionViewSource() { Source = _resultOutputs }).View;
            Wpf_AllResultsForInterface.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedByCurrentSolver);
            Wpf_AllResultsForInterface.GroupDescriptions.Add(new PropertyGroupDescription("TargetShape"));
            Wpf_AllResultsForInterface.GroupDescriptions.Add(new PropertyGroupDescription("ResultFamily"));
            Wpf_AllResultsForInterface.SortDescriptions.Add(new SortDescription("TargetShape", ListSortDirection.Ascending));
            Wpf_AllResultsForInterface.SortDescriptions.Add(new SortDescription("ResultFamily", ListSortDirection.Ascending));
           // Wpf_AllResultsForInterface.SortDescriptions.Add(new SortDescription("ResultType", ListSortDirection.Ascending));
            // Setting live shaping
            if (Wpf_AllResultsForInterface is ICollectionViewLiveShaping ls1)
            {
                ls1.LiveFilteringProperties.Add("IsSupportedByCurrentSolver");
                ls1.IsLiveFiltering = true;
            }
            else throw new Exception($"List does not accept ICollectionViewLiveShaping.");

            // Creates the view for the Selected Output Results
            CollectionViewSource selectedResults_cvs = new CollectionViewSource() {Source = _resultOutputs};
            Wpf_SelectedFiniteElementResultsForOutput = selectedResults_cvs.View;
            Wpf_SelectedFiniteElementResultsForOutput.Filter += inO => ((inO is FeResultClassification item) && item.IsSupportedByCurrentSolver && item.OutputData_IsSelected);
            // Setting live shaping
            if (Wpf_SelectedFiniteElementResultsForOutput is ICollectionViewLiveShaping ls2)
            {
                ls2.LiveFilteringProperties.Add("IsSupportedByCurrentSolver");
                ls2.LiveFilteringProperties.Add("OutputData_IsSelected");
                ls2.IsLiveFiltering = true;
            }
            else throw new Exception($"List does not accept ICollectionViewLiveShaping.");

            Wpf_SelectedFiniteElementResultsForOutput.CollectionChanged += Wpf_SelectedFiniteElementResultsForOutputOnCollectionChanged;
        }
        
        #region Solver Type and Basic Options
        private FeSolverTypeEnum _feSolverType_Selected = (FeSolverTypeEnum)Enum.Parse(typeof(FeSolverTypeEnum), Settings.Default.Default_FeSolverType);
        public FeSolverTypeEnum FeSolverType_Selected
        {
            get => _feSolverType_Selected;
            set
            {
                SetProperty(ref _feSolverType_Selected, value);

                // Updates dependent properties
                RaisePropertyChanged("IsFeProblem");

                // Updates the lists as they depend on the flag
                Wpf_SelectedFiniteElementResultsForOutput.Refresh();
                Wpf_AllResultsForInterface.Refresh();
            }
        }
        public Visibility IsFeProblem => FeSolverType_Selected == FeSolverTypeEnum.NotFeProblem ? Visibility.Hidden : Visibility.Visible;

        public Dictionary<FeSolverTypeEnum, string> WfpCaption_FeSolverTypeEnum => ListDescSH.I.FeSolverTypeEnumStaticDescriptions;

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

        public Dictionary<MainAxisDirectionEnum, string> WfpCaption_MainAxisDirectionEnum => ListDescSH.I.MainAxisDirectionEnumStaticDescriptions;
        #endregion

        #region Point Loads
        private GhGeom_ParamDefBase _addNewPointLoad_SelectedGeometry;
        public GhGeom_ParamDefBase AddNewPointLoad_SelectedGeometry
        {
            get => _addNewPointLoad_SelectedGeometry;
            set => SetProperty(ref _addNewPointLoad_SelectedGeometry, value);
        }
        private double _addNewPointLoad_NominalX = 0d;
        public double AddNewPointLoad_NominalX
        {
            get => _addNewPointLoad_NominalX;
            set => SetProperty(ref _addNewPointLoad_NominalX, value);
        }
        private double _addNewPointLoad_NominalY = 0d;
        public double AddNewPointLoad_NominalY
        {
            get => _addNewPointLoad_NominalY;
            set => SetProperty(ref _addNewPointLoad_NominalY, value);
        }
        private double _addNewPointLoad_NominalZ = 0d;
        public double AddNewPointLoad_NominalZ
        {
            get => _addNewPointLoad_NominalZ;
            set => SetProperty(ref _addNewPointLoad_NominalZ, value);
        }
        private double _addNewPointLoad_Factor = 1d;
        public double AddNewPointLoad_Factor
        {
            get => _addNewPointLoad_Factor;
            set => SetProperty(ref _addNewPointLoad_Factor, value);
        }

        private FastObservableCollection<FeLoad_Point> _pointLoads = new FastObservableCollection<FeLoad_Point>();
        public FastObservableCollection<FeLoad_Point> PointLoads
        {
            get => _pointLoads;
            set => SetProperty(ref _pointLoads, value);
        }

        public void WpfCommand_AddNewPointLoad()
        {
            try
            {
                if (AddNewPointLoad_NominalX == 0d && AddNewPointLoad_NominalY == 0d && AddNewPointLoad_NominalZ == 0d) throw new InvalidOperationException($"The nominal force must not be 0.");

                // Adds the definition to the list
                PointLoads.Add(new FeLoad_Point(AddNewPointLoad_SelectedGeometry, new Vector3d(AddNewPointLoad_NominalX, AddNewPointLoad_NominalY, AddNewPointLoad_NominalZ), AddNewPointLoad_Factor));
            }
            catch (Exception e)
            {
                ExceptionViewer.Show(e, "Could not Add Point Load.");
            }
        }
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
        private readonly MPOC.ObservableCollection<FeResultClassification> _resultOutputs;
        public ICollectionView Wpf_AllResultsForInterface { get; } // *grouped* and *sorted* - Used in the selection for output interface
        public ICollectionView Wpf_SelectedFiniteElementResultsForOutput { get; }
        private void Wpf_SelectedFiniteElementResultsForOutputOnCollectionChanged(object inSender, NotifyCollectionChangedEventArgs inE)
        {
            // Items have been removed from the list. This means that we must delete them from the lists of selected solution outputs for calculations
            if (inE.Action == NotifyCollectionChangedAction.Remove)
            {
                if (inE.OldItems != null && inE.OldItems.Count > 0)
                {
                    // For each removed item
                    foreach (FeResultClassification resClass in inE.OldItems)
                    {
                        AppSS.I.ProbQuantMgn.DeleteAllQuantities(resClass);
                    }
                } 
            }
        }

        public void SelectOrDeselect_AllResultsInShapeFamily(FeResultFamilyEnum inFamily, FeAnalysisShapeEnum inShape)
        {
            // Selects all or Deselects all
            bool allSelected = _resultOutputs.Where(a => a.ResultFamily == inFamily && a.TargetShape == inShape).All(b => b.OutputData_IsSelected);

            foreach (FeResultClassification feResultClassification in _resultOutputs.Where(a => a.ResultFamily == inFamily && a.TargetShape == inShape))
            {
                feResultClassification.OutputData_IsSelected = !allSelected;
            }
        }

        public void EvaluateRequiredFeOutputs()
        {
            bool hasPerfectShape_EigenvalueBuckling = AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Any(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape && a.IsEigenValueBuckling);

            bool hasImperfectFullStiffness = AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness);

            bool hasImperfectSoftened = AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened);

            IsPerfectShape_EigenvalueBucking_Required = hasPerfectShape_EigenvalueBuckling || hasImperfectFullStiffness || hasImperfectSoftened;


            if (hasImperfectFullStiffness)
            {
                IsImperfectShapeFullStiffness_StaticAnalysis_Required = true;

                bool has_EigenvalueBuckling = AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                            a.IsEigenValueBuckling);

                IsImperfectShapeFullStiffness_EigenvalueBuckling_Required = has_EigenvalueBuckling;
            }
            else // Nothing imperfect FullStiffness is required
            {
                IsImperfectShapeFullStiffness_StaticAnalysis_Required = false;
                IsImperfectShapeFullStiffness_EigenvalueBuckling_Required = false;
            }


            if (hasImperfectSoftened)
            {
                IsImperfectShapeSoftened_StaticAnalysis_Required = true;

                bool has_EigenvalueBuckling = AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                           a.IsEigenValueBuckling);

                IsImperfectShapeSoftened_EigenvalueBuckling_Required = has_EigenvalueBuckling;
            }
            else // Nothing imperfect Softened is required
            {
                IsImperfectShapeSoftened_StaticAnalysis_Required = false;
                IsImperfectShapeSoftened_EigenvalueBuckling_Required = false;
            }
        }
        public bool IsPerfectShape_EigenvalueBucking_Required;
        public bool IsImperfectShapeFullStiffness_StaticAnalysis_Required;
        public bool IsImperfectShapeFullStiffness_EigenvalueBuckling_Required;
        public bool IsImperfectShapeSoftened_StaticAnalysis_Required;
        public bool IsImperfectShapeSoftened_EigenvalueBuckling_Required;

        public IEnumerable<FeResultClassification> PerfectShapeRequestedResults => Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape);
        public IEnumerable<FeResultClassification> ImperfectShapeFullStiffnessRequestedResults => Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness);
        public IEnumerable<FeResultClassification> ImperfectShapeSoftenedRequestedResults => Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened);

        #endregion

        #region Wpf
        private IProblemQuantitySource _selectedDisplayDataResultClassification;
        public IProblemQuantitySource SelectedDisplayDataResultClassification
        {
            get => _selectedDisplayDataResultClassification;
            set => SetProperty(ref _selectedDisplayDataResultClassification, value);
        }
        #endregion
    }
    
    public enum FeSolverTypeEnum
    {
        NotFeProblem,
        Ansys,
        Sap2000
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
