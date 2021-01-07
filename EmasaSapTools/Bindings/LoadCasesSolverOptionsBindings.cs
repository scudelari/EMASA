using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.SapObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EmasaSapTools.Bindings
{
    public class LoadCasesSolverOptionsBindings : BindableSingleton<LoadCasesSolverOptionsBindings>
    {
        private LoadCasesSolverOptionsBindings()
        {
        }

        public override void SetOrReset()
        {
            OnlyFailed_IsChecked = true;

            // Sets the view of the collection
            NonLinearCasesSolverDataGrid_ViewItems = CollectionViewSource.GetDefaultView(NonLinearCasesSolverDataGrid_RawItems);
            NonLinearCasesSolverDataGrid_ViewItems.Filter = NonLinearCasesSolverDataGrid_ViewItemsFilter;

            // Adds a response to the change of the selected items in the grid
            NonLinearCasesSolverDataGrid_Selected.CollectionChanged += NonLinearCasesSolverDataGrid_Selected_CollectionChanged;
        }

        public ICollectionView NonLinearCasesSolverDataGrid_ViewItems { get; private set; }
        private bool NonLinearCasesSolverDataGrid_ViewItemsFilter(object inObj)
        {
            if (inObj is LCNonLinear lcNonLinear)
            {
                if (OnlyFailed_IsChecked)
                {
                    if (lcNonLinear.Status == LCStatus.CouldNotStart || lcNonLinear.Status != LCStatus.NotFinished)
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(RegExFilter_Text))
                {
                    if (_RegExFilter != null && !_RegExFilter.IsMatch(lcNonLinear.Name)) return false;
                }
            }

            return true;
        }

        public FastObservableCollection<LCNonLinear> NonLinearCasesSolverDataGrid_RawItems { get; } = new FastObservableCollection<LCNonLinear>();
        public FastObservableCollection<LCNonLinear> NonLinearCasesSolverDataGrid_Selected { get; } = new FastObservableCollection<LCNonLinear>();
        private void NonLinearCasesSolverDataGrid_Selected_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Gets the first selection to fill the boxes underneath
            if (NonLinearCasesSolverDataGrid_Selected.Count == 0)
            {
                SelectedCase = null;
                SelectedCase_IsEnabled = false;
            }
            else
            {
                SelectedCase = NonLinearCasesSolverDataGrid_Selected[0];
                SelectedCase_IsEnabled = true;
            }
        }

        private LCNonLinear _selectedCase = null;
        public LCNonLinear SelectedCase
        {
            get => _selectedCase;
            set => SetProperty(ref _selectedCase, value);
        }

        private bool _SelectedCase_IsEnabled;
        public bool SelectedCase_IsEnabled
        {
            get => _SelectedCase_IsEnabled;
            set => SetProperty(ref _SelectedCase_IsEnabled, value);
        }

        private bool _OnlyFailed_IsChecked;
        public bool OnlyFailed_IsChecked
        {
            get => _OnlyFailed_IsChecked;
            set
            {
                SetProperty(ref _OnlyFailed_IsChecked, value);
                NonLinearCasesSolverDataGrid_ViewItems?.Refresh();
            }
        }

        private string _RegExFilter_Text;
        public string RegExFilter_Text
        {
            get => _RegExFilter_Text;
            set
            {
                SetProperty(ref _RegExFilter_Text, value);

                if (string.IsNullOrWhiteSpace(value))
                {
                    _RegExFilter = null;
                }
                else
                {
                    _RegExFilter = new Regex(_RegExFilter_Text);
                }

                NonLinearCasesSolverDataGrid_ViewItems?.Refresh();
            }
        }
        private Regex _RegExFilter = null;

        #region Actions
        private DelegateCommand _getNlCasesButtonCommand;
        public DelegateCommand GetNlCasesButtonCommand =>
            _getNlCasesButtonCommand ?? (_getNlCasesButtonCommand = new DelegateCommand(ExecuteGetNlCasesButtonCommand));
        public async void ExecuteGetNlCasesButtonCommand()
        {
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Cleans up the current list
                    BusyOverlayBindings.I.Title = "Non-Linear Cases";

                    List<LCNonLinear> caseList = S2KModel.SM.LCMan.GetNonLinearStaticLoadCaseList(inUpdateInterface:true);

                    // Updates the list if, and only if, it is a new list.
                    NonLinearCasesSolverDataGrid_RawItems.ReplaceItemsIfNew(caseList);
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Could not get the Non-Linear Case List and Data.");
            }
            finally
            {
                OnEndCommand();
            }
        }

        private DelegateCommand _updateSelectedCasesButtonCommand;
        public DelegateCommand UpdateSelectedCasesButtonCommand =>
            _updateSelectedCasesButtonCommand ?? (_updateSelectedCasesButtonCommand = new DelegateCommand(ExecuteUpdateSelectedCasesButtonCommand));
        public async void ExecuteUpdateSelectedCasesButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = "Updating Non-Linear load case run parameters.";
                    BusyOverlayBindings.I.SetDeterminate("", "Load Case");

                    for (int i = 0; i < NonLinearCasesSolverDataGrid_Selected.Count; i++)
                    {
                        LCNonLinear item = NonLinearCasesSolverDataGrid_Selected[i];
                        BusyOverlayBindings.I.UpdateProgress(i, NonLinearCasesSolverDataGrid_Selected.Count, item.Name);

                        try
                        {
                            BusyOverlayBindings.I.MessageText = "Deleting Old Results.";
                            S2KModel.SM.AnalysisMan.DeleteResultsOfLoadCase(item.Name);

                            BusyOverlayBindings.I.MessageText = $"Updating Solution Controls.";
                            S2KModel.SM.LCMan.UpdateNLSolControlParams(item, SelectedCase.SolControlParams);

                            BusyOverlayBindings.I.MessageText = $"Updating Target Force Controls.";
                            S2KModel.SM.LCMan.UpdateNLTargetForceParams(item, SelectedCase.TargetForceParams);

                            BusyOverlayBindings.I.MessageText = $"Updating Node Control.";
                            S2KModel.SM.LCMan.UpdateNLLoadApplication(item, new LCNonLinear_LoadApplicationOptions()
                                {
                                DOF = LCNonLinear_DOF.U1,
                                Displacement = 0d,
                                DispType = LCNonLinear_DispType.MonitoredDisplacement,
                                GeneralizedDisplacementName = "",
                                LoadControl = LCNonLinear_LoadControl.FullLoad,
                                Monitor = LCNonLinear_Monitor.DisplacementAtSpecifiedPoint,
                                PointName = "KH_600006_IJ_2"
                            });


                            BusyOverlayBindings.I.MessageText = $"Updating Results Saved Controls.";
                            switch (item.NLSubType)
                            {
                                case LCNonLinear_SubType.Nonlinear:
                                    S2KModel.SM.LCMan.UpdateNLResultsSavedNL(item,
                                        SelectedCase.ResultsSavedNL);
                                    break;

                                case LCNonLinear_SubType.StagedConstruction:
                                    S2KModel.SM.LCMan.UpdateNLResultsSavedStaged(item,
                                        SelectedCase.ResultsSavedStaged);
                                    break;

                                default:
                                    break;
                            }
                        }
                        catch
                        {
                            endMessages.AppendLine(item.Name);
                        }
                    }
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Could not update the load case options.");
            }
            finally
            {
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) 
                    OnMessage("The following Non-Linear cases could not be updated", endMessages.ToString());
            }
        }

        private DelegateCommand _addModalToSelectedCasesButtonCommand;
        public DelegateCommand AddModalToSelectedCasesButtonCommand =>
            _addModalToSelectedCasesButtonCommand ?? (_addModalToSelectedCasesButtonCommand = new DelegateCommand(ExecuteAddModalToSelectedCasesButtonCommand));
        public async void ExecuteAddModalToSelectedCasesButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.SetDeterminate("Adding Modal Case.", "Load Case");

                    for (int i = 0; i < NonLinearCasesSolverDataGrid_Selected.Count; i++)
                    {
                        LCNonLinear item = NonLinearCasesSolverDataGrid_Selected[i];
                        BusyOverlayBindings.I.UpdateProgress(i, NonLinearCasesSolverDataGrid_Selected.Count, item.Name);

                        try
                        {
                            string modalCaseName = $"M_{item.Name}";
                            S2KModel.SM.LCMan.AddNew_Modal(modalCaseName, item.Name);
                        }
                        catch
                        {
                            endMessages.AppendLine(item.Name);
                        }
                    }
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0) 
                    OnMessage("The following Non-Linear cases could not have modal created", endMessages.ToString());
            }
        }
        #endregion
    }
}