using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt
{
    public class ProblemConfig_Options : BindableBase
    {
        public ProblemConfig_Options()
        {
            #region Section Assignment for the Configuration Combinations

            AvailableSectionList_View = (new CollectionViewSource()
                {
                Source = FeSection.GetAllSections()
                }).View;
            //AvailableSectionList_View.SortDescriptions.Add(new SortDescription("FirstSortDimension", ListSortDirection.Ascending));
            #endregion

            UpdateAvailableIntegers();

            #region Integers
            _availableIntegers.CollectionChanged += _availableIntegers_CollectionChanged;
            AvailableIntegerList_View = (new CollectionViewSource() {Source = _availableIntegers }).View;
            #endregion
        }



        #region Problem Configurations - User Assignments - FeSections
        private List<FeSection> _allFeSections => FeSection.GetAllSections();
        public ICollectionView AvailableSectionList_View { get; }
        public FastObservableCollection<FeSection> WpfSelected_Sections { get; } = new FastObservableCollection<FeSection>();

        private LineList_GhGeom_ParamDef _currentlySelectedLineListForDetails;
        public LineList_GhGeom_ParamDef CurrentlySelectedLineListForDetails
        {
            get => _currentlySelectedLineListForDetails;
            set => SetProperty(ref _currentlySelectedLineListForDetails, value);
        }


        public void Wpf_FeSections_SelectCenterOfEachFamily()
        {
            WpfSelected_Sections.AddItems(FeSection.GetMiddleOfFamilies(), true);
        }
        public void Wpf_FeSections_SelectAll()
        {
            WpfSelected_Sections.AddItems(AvailableSectionList_View.OfType<FeSection>().ToList(), true);
        }
        public void Wpf_FeSections_ClearSelection()
        {
            WpfSelected_Sections.Clear();
        }
        #endregion

        
        #region Problem Configurations - User Assignments - Integers
        private FastObservableCollection<int> _availableIntegers = new FastObservableCollection<int>();
        private void _availableIntegers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WpfSelected_Integers.AddItems(_availableIntegers, true);
        }
        public ICollectionView AvailableIntegerList_View { get; }
        public FastObservableCollection<int> WpfSelected_Integers { get; } = new FastObservableCollection<int>();

        private Integer_GhConfig_ParamDef _currentlySelectedIntegerConfigForDetails;
        public Integer_GhConfig_ParamDef CurrentlySelectedIntegerConfigForDetails
        {
            get => _currentlySelectedIntegerConfigForDetails;
            set => SetProperty(ref _currentlySelectedIntegerConfigForDetails, value);
        }



        private int _rangeMin = 2;
        public int RangeMin
        {
            get => _rangeMin;
            set
            {
                if (value > RangeMax) throw new InvalidOperationException($"Minimum value must be lower than or equal to the maximum value.");
                SetProperty(ref _rangeMin, value);
                UpdateAvailableIntegers();
            }
        }
        private int _rangeMax = 10;
        public int RangeMax
        {
            get => _rangeMax;
            set
            {
                if (value > RangeMin) throw new InvalidOperationException($"Maximum value must be higher than or equal to the minimum value.");
                SetProperty(ref _rangeMax, value);
                UpdateAvailableIntegers();
            }
        }

        private void UpdateAvailableIntegers()
        {
            // Builds the list of integers
            int[] intArray = new int[RangeMax - RangeMin + 1];
            for (int i = 0; i < intArray.Length; i++)
            {
                intArray[i] = i + RangeMin;
            }
            _availableIntegers.AddItems(intArray, true);
        }

        public void Wpf_Integers_SelectAll()
        {
            WpfSelected_Integers.AddItems(AvailableIntegerList_View.OfType<int>().ToList(), true);
        }
        public void Wpf_Integers_ClearSelection()
        {
            WpfSelected_Integers.Clear();
        }
        public void wpf_Integers_SelectOdd()
        {
            WpfSelected_Integers.AddItems(AvailableIntegerList_View.OfType<int>().Where(a => a % 2 == 0).ToList(), true);
        }
        public void wpf_Integers_SelectEven()
        {
            WpfSelected_Integers.AddItems(AvailableIntegerList_View.OfType<int>().Where(a => a % 2 == 1).ToList(), true);
        }
        #endregion
    }
}
