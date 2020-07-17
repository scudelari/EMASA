using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class FormBasicRefreshingBindings : BindableSingleton<FormBasicRefreshingBindings>
    {
        private FormBasicRefreshingBindings()
        {
        }

        public override void SetOrReset()
        {
            Global_Cases_Available = new ReadOnlyCollection<string>(new List<string>());

            Global_Groups_Available = new ReadOnlyCollection<string>(new List<string>());
        }



        #region NEW SECTION

        

        #endregion




        public void SetSapModelName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                SapModelName = "You must open SAP2000 in order to work with the SAP2000 functions.";
                SapAvailableIsEnabled = false;
            }
            else
            {
                SapModelName = name;
                SapAvailableIsEnabled = true;
            }
        }

        /// <summary>
        /// Don't Access DIRECTLY! Use SetSapModelName Instead!
        /// </summary>
        private string _SapModelName;

        public string SapModelName
        {
            get => _SapModelName;
            set => SetProperty(ref _SapModelName, value);
        }

        private bool _SapAvailableIsEnabled;

        public bool SapAvailableIsEnabled
        {
            get => _SapAvailableIsEnabled;
            set => SetProperty(ref _SapAvailableIsEnabled, value);
        }

        private ReadOnlyCollection<string> _currentGroups = null;

        private ReadOnlyCollection<string> _Global_Groups_Available;
        public ReadOnlyCollection<string> Global_Groups_Available
        {
            get => _Global_Groups_Available;
            set
            {
                if (_currentGroups == null)
                {
                    SetProperty(ref _Global_Groups_Available, value);
                    _currentGroups = value;
                    return;
                }

                // If the sequence changed
                if (!_currentGroups.SequenceEqual(value))
                {
                    //// Saves the current selections
                    //var currentResultsDisplacements_Groups_Selected = ResultsDisplacements_Groups_Selected;
                    //string currentParallelGhostStifferGroupComboBox_SelectedGroup = ParallelGhostStifferGroupComboBox_SelectedGroup;

                    //SetProperty(ref _Global_Groups_Available, value);
                    //_currentGroups = value;

                    //// Reinstates the current selections
                    //if (currentResultsDisplacements_Groups_Selected.All(a => value.Contains(a))) 
                    //    ResultsDisplacements_Groups_Selected = currentResultsDisplacements_Groups_Selected;
                    //if (value.Contains(currentParallelGhostStifferGroupComboBox_SelectedGroup))
                    //    ParallelGhostStifferGroupComboBox_SelectedGroup = currentParallelGhostStifferGroupComboBox_SelectedGroup;
                }
            }
        }

        //private string _ParallelGhostStifferGroupComboBox_SelectedGroup;
        //public string ParallelGhostStifferGroupComboBox_SelectedGroup
        //{
        //    get => _ParallelGhostStifferGroupComboBox_SelectedGroup;
        //    set => SetProperty(ref _ParallelGhostStifferGroupComboBox_SelectedGroup, value);
        //}

        private ReadOnlyCollection<string> _currentCases = null;

        private ReadOnlyCollection<string> _Global_Cases_Available;
        public ReadOnlyCollection<string> Global_Cases_Available
        {
            get => _Global_Cases_Available;
            set
            {
                if (_currentCases == null)
                {
                    SetProperty(ref _Global_Cases_Available, value);
                    _currentCases = value;
                    return;
                }

                // If the sequence changed
                if (!_currentCases.SequenceEqual(value))
                {
                    //// Saves the current selections
                    //var currentResultsDisplacements_Cases_Selected = ResultsDisplacements_Cases_Selected;

                    //SetProperty(ref _Global_Cases_Available, value);
                    //_currentCases = value;

                    //// Reinstates the current selections
                    //if (currentResultsDisplacements_Cases_Selected.All(a => value.Contains(a)))
                    //    ResultsDisplacements_Cases_Selected = currentResultsDisplacements_Cases_Selected;
                }
            }
        }

        private ReadOnlyCollection<string> _currentNonLinearCases = null;

        private ReadOnlyCollection<string> _Global_NonLinearCases_Available;
        public ReadOnlyCollection<string> Global_NonLinearCases_Available
        {
            get => _Global_NonLinearCases_Available;
            set
            {
                if (_currentNonLinearCases == null)
                {
                    SetProperty(ref _Global_NonLinearCases_Available, value);
                    _currentNonLinearCases = value;
                    return;
                }

                // If the sequence changed
                if (!_currentNonLinearCases.SequenceEqual(value))
                {
                    // Saves the current selections
                    string currentTrussSolverLoadCase_Selected = TrussSolverLoadCase_Selected;

                    SetProperty(ref _Global_NonLinearCases_Available, value);
                    _currentNonLinearCases = value;

                    // Reinstates the current selections
                    if (value.Contains(currentTrussSolverLoadCase_Selected))
                        TrussSolverLoadCase_Selected = currentTrussSolverLoadCase_Selected;
                }
            }
        }

        private string _TrussSolverLoadCase_Selected;

        public string TrussSolverLoadCase_Selected
        {
            get => _TrussSolverLoadCase_Selected;
            set => SetProperty(ref _TrussSolverLoadCase_Selected, value);
        }

        private ReadOnlyCollection<string> _currentFrameSections = null;

        private ReadOnlyCollection<string> _Global_FrameSections_Available;

        public ReadOnlyCollection<string> Global_FrameSections_Available
        {
            get => _Global_FrameSections_Available;
            set
            {
                if (_currentFrameSections == null)
                {
                    SetProperty(ref _Global_FrameSections_Available, value);
                    _currentFrameSections = value;
                    return;
                }

                // If the sequence changed
                if (!_currentFrameSections.SequenceEqual(value))
                {
                    // Saves the current selections
                    string currentManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection =
                        ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection;

                    SetProperty(ref _Global_FrameSections_Available, value);
                    _currentFrameSections = value;

                    // Reinstates the current selections
                    if (value.Contains(currentManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection))
                        ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection =
                            currentManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection;
                }
            }
        }

        private string _ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection;

        public string ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection
        {
            get => _ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection;
            set => SetProperty(ref _ManipulateItemsSubstitute_LinksToFrames_SelectedFrameSection, value);
        }
    }
}