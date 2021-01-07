using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt
{
    public class ProblemQuantityManager : BindableBase
    {
        public ProblemQuantityManager()
        {
            #region Initializes the Problem Quantities Views
            WpfProblemQuantities_All = new CollectionViewSource() { Source = _problemQuantities }.View;

            WpfProblemQuantities_OutputOnly = (new CollectionViewSource() { Source = _problemQuantities }).View;
            WpfProblemQuantities_OutputOnly.Filter += inO => (inO is ProblemQuantity pq && pq.IsOutputOnly);

            WpfProblemQuantities_ObjectiveFunction = (new CollectionViewSource() { Source = _problemQuantities }).View;
            WpfProblemQuantities_ObjectiveFunction.Filter += inO => (inO is ProblemQuantity pq && pq.IsObjectiveFunctionMinimize);

            WpfProblemQuantities_Constraint = (new CollectionViewSource() { Source = _problemQuantities }).View;
            WpfProblemQuantities_Constraint.Filter += inO => (inO is ProblemQuantity pq && pq.IsConstraint);
            #endregion

            // This list will contain both the Available Fe Outputs and the Gh Defined Double Lists
            // Adds the Grasshopper Quantities
            ProblemQuantityAvailableTypes.AddRange(AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<IProblemQuantitySource>());
            // Adds all available Fe Quantities quantities
            ProblemQuantityAvailableTypes.AddRange(AppSS.I.FeOpt.Wpf_AllResultsForInterface.OfType<IProblemQuantitySource>());

            WpfProblemQuantityAvailableTypes = (new CollectionViewSource() { Source = ProblemQuantityAvailableTypes }).View;
            WpfProblemQuantityAvailableTypes.Filter += inO =>
            {
                if ((inO is IProblemQuantitySource item)) return item.IsSupportedByCurrentSolver && item.OutputData_IsSelected;
                return false;
            };
            // Setting live shaping
            if (WpfProblemQuantityAvailableTypes is ICollectionViewLiveShaping ls2)
            {
                ls2.LiveFilteringProperties.Add("IsSupportedByCurrentSolver");
                ls2.LiveFilteringProperties.Add("OutputData_IsSelected");
                ls2.IsLiveFiltering = true;
            }
            else throw new Exception($"List does not accept ICollectionViewLiveShaping.");
            
            WpfProblemQuantityAvailableTypes.SortDescriptions.Add(new SortDescription("IsFiniteElementData", ListSortDirection.Ascending));
        }

        #region Problem Quantity Available Types
        public MintPlayer.ObservableCollection.ObservableCollection<IProblemQuantitySource> ProblemQuantityAvailableTypes { get; } = new MintPlayer.ObservableCollection.ObservableCollection<IProblemQuantitySource>();
        public ICollectionView WpfProblemQuantityAvailableTypes { get; }

        private IProblemQuantitySource _wpf_SelectedProblemQuantityTypeForOutputDisplay;
        public IProblemQuantitySource Wpf_SelectedProblemQuantityTypeForOutputDisplay
        {
            get => _wpf_SelectedProblemQuantityTypeForOutputDisplay;
            set
            {
                SetProperty(ref _wpf_SelectedProblemQuantityTypeForOutputDisplay, value);

                // Sets the Source to display the results
                AppSS.I.NlOptDetails_DisplayAggregator.WpfDisplayProblemQuantitySource = Wpf_SelectedProblemQuantityTypeForOutputDisplay;
                // Tells the interface that the display aggregator changed
                AppSS.I.NlOptDetails_DisplayAggregator_Changed();

                // Warns also that there is a change also in the Points' Quantity Outputs
                if (AppSS.I.SolveMgr.Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint != null)
                { // Only if there is indeed a selected function point. It can happen that the Problem Config completely failed.
                    foreach (var nlOpt_Point_ProblemQuantity_Output in AppSS.I.SolveMgr.Wpf_CurrentlySelected_ProblemConfig.Wpf_SelectedDisplayFunctionPoint.ProblemQuantityOutputs)
                    {
                        nlOpt_Point_ProblemQuantity_Output.Value.Wpf_UsesSelected_IProblemQuantitySource_Updated();
                    }
                }
            }
        }

        #endregion

        #region Problem Quantities Selection
        public int ProblemQuantityMaxIndex { get; set; } = 1;

        private readonly FastObservableCollection<ProblemQuantity> _problemQuantities = new FastObservableCollection<ProblemQuantity>();
        public ICollectionView WpfProblemQuantities_OutputOnly { get; }
        public ICollectionView WpfProblemQuantities_ObjectiveFunction { get; }
        public ICollectionView WpfProblemQuantities_Constraint { get; }
        public ICollectionView WpfProblemQuantities_All { get; }
        public void AddProblemQuantity(ProblemQuantity inProblemQuantity, bool inIsAddingAll = false)
        {
            _problemQuantities.Add(inProblemQuantity);

            if (!inIsAddingAll)
            {
                if (inProblemQuantity.IsOutputOnly) AppSS.I.BringListChildIntoView("ProbQuantMgn_SelectedQuantities_OutputOnly_ListBox", inProblemQuantity, AppSS.FirstReferencedWindow);
                else if (inProblemQuantity.IsConstraint) AppSS.I.BringListChildIntoView("ProbQuantMgn_SelectedQuantities_Constraints_ListBox", inProblemQuantity, AppSS.FirstReferencedWindow);
                else if (inProblemQuantity.IsObjectiveFunctionMinimize) AppSS.I.BringListChildIntoView("ProbQuantMgn_SelectedQuantities_ObjectiveFunction_ListBox", inProblemQuantity, AppSS.FirstReferencedWindow);
            }
        }
        public void DeleteProblemQuantity(ProblemQuantity inProblemQuantity)
        {
            _problemQuantities.Remove(inProblemQuantity);
        }
        public void DeleteAllQuantities(FeResultClassification inResultClassification = null)
        {
            if (inResultClassification == null) _problemQuantities.Clear();
            else
            {
                List<ProblemQuantity> toRemove = _problemQuantities.Where(a => a.QuantitySource_AsFeResult == inResultClassification).ToList();
                _problemQuantities.RemoveItems(toRemove);
            }
        }

        public void AddAllProblemQuantity_OutputOnly()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                AddProblemQuantity(new ProblemQuantity(ghDoubleList, Quantity_TreatmentTypeEnum.OutputOnly), true);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                AddProblemQuantity(new ProblemQuantity(feResultClassification, Quantity_TreatmentTypeEnum.OutputOnly), true);
            }
        }
        public void AddAllProblemQuantity_ConstraintObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                AddProblemQuantity(new ProblemQuantity(ghDoubleList, Quantity_TreatmentTypeEnum.Constraint), true);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                AddProblemQuantity(new ProblemQuantity(feResultClassification, Quantity_TreatmentTypeEnum.Constraint), true);
            }
        }
        public void AddAllProblemQuantity_FunctionObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                AddProblemQuantity(new ProblemQuantity(ghDoubleList, Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize), true);
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                AddProblemQuantity(new ProblemQuantity(feResultClassification, Quantity_TreatmentTypeEnum.ObjectiveFunctionMinimize), true);
            }
        }
        #endregion
    }
}
