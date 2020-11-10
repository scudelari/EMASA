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
            CollectionViewSource problemQuantities_All_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_All = problemQuantities_All_cvs.View;

            CollectionViewSource problemQuantities_OutputOnly_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_OutputOnly = problemQuantities_OutputOnly_cvs.View;
            WpfProblemQuantities_OutputOnly.Filter += inO => (inO is ProblemQuantity pq && pq.IsOutputOnly);

            CollectionViewSource problemQuantities_ObjectiveFunction_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_ObjectiveFunction = problemQuantities_ObjectiveFunction_cvs.View;
            WpfProblemQuantities_ObjectiveFunction.Filter += inO => (inO is ProblemQuantity pq && pq.IsObjectiveFunctionMinimize);

            CollectionViewSource problemQuantities_Constraints_cvs = new CollectionViewSource() { Source = _problemQuantities };
            WpfProblemQuantities_Constraint = problemQuantities_Constraints_cvs.View;
            WpfProblemQuantities_Constraint.Filter += inO => (inO is ProblemQuantity pq && pq.IsConstraint);
            #endregion

            CollectionViewSource problemQuantities_AvailableTypes_cvs = new CollectionViewSource() { Source = ProblemQuantityAvailableTypes };
            WpfProblemQuantityAvailableTypes = problemQuantities_AvailableTypes_cvs.View;
            WpfProblemQuantityAvailableTypes.SortDescriptions.Add(new SortDescription("IsFiniteElementData", ListSortDirection.Ascending));
        }

        #region Problem Quantity Available Types
        public FastObservableCollection<IProblemQuantitySource> ProblemQuantityAvailableTypes { get; } = new FastObservableCollection<IProblemQuantitySource>();
        public ICollectionView WpfProblemQuantityAvailableTypes { get; }
        #endregion

        #region Problem Quantities Selection
        public int ProblemQuantityMaxIndex { get; set; } = 1;

        private readonly FastObservableCollection<ProblemQuantity> _problemQuantities = new FastObservableCollection<ProblemQuantity>();
        public ICollectionView WpfProblemQuantities_OutputOnly { get; }
        public ICollectionView WpfProblemQuantities_ObjectiveFunction { get; }
        public ICollectionView WpfProblemQuantities_Constraint { get; }
        public ICollectionView WpfProblemQuantities_All { get; }
        public void AddProblemQuantity(ProblemQuantity inProblemQuantity)
        {
            _problemQuantities.Add(inProblemQuantity);
            //WpfProblemQuantities_ObjectiveFunction.Refresh();
        }
        public void DeleteProblemQuantity(ProblemQuantity inProblemQuantity)
        {
            _problemQuantities.Remove(inProblemQuantity);
            //WpfProblemQuantities_ObjectiveFunction.Refresh();
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
                ghDoubleList.AddProblemQuantity_OutputOnly();
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_OutputOnly();
            }
        }
        public void AddAllProblemQuantity_ConstraintObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                ghDoubleList.AddProblemQuantity_ConstraintObjective();
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_ConstraintObjective();
            }
        }
        public void AddAllProblemQuantity_FunctionObjective()
        {
            // Adds one for each Grasshopper double list
            foreach (DoubleList_GhGeom_ParamDef ghDoubleList in AppSS.I.Gh_Alg.GeometryDefs_DoubleList_View.OfType<DoubleList_GhGeom_ParamDef>())
            {
                ghDoubleList.AddProblemQuantity_FunctionObjective();
            }

            // Adds one for each selected output results
            foreach (FeResultClassification feResultClassification in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                feResultClassification.AddProblemQuantity_FunctionObjective();
            }
        }
        #endregion
    }
}
