using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EmasaSapTools.DataGridTypes;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using MoreLinq.Extensions;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class ConstraintCleanupBindings : BindableSingleton<ConstraintCleanupBindings>
    {
        private ConstraintCleanupBindings()
        {
        }

        public override void SetOrReset()
        {
            ConstraintCleanupDataGrid_ViewItems = CollectionViewSource.GetDefaultView(ConstraintCleanupDataGrid_RawItems);
            ConstraintCleanupDataGrid_ViewItems.Filter = ConstraintListFilter;
            ConstraintCleanupDataGrid_ViewItems.CollectionChanged +=ConstraintCleanupDataGrid_ViewItemsOnCollectionChanged;

            ConstraintCleanupDataGrid_Selected.CollectionChanged += ConstraintCleanupDataGrid_Selected_CollectionChanged;
        }

        private void ConstraintCleanupDataGrid_ViewItemsOnCollectionChanged(object inSender, NotifyCollectionChangedEventArgs inE)
        {
            List<ConstraintCleanupDataGridType> currentViewList = ConstraintCleanupDataGrid_ViewItems.OfType<ConstraintCleanupDataGridType>().ToList();
            ConstraintReportSummaryTextBlock_Text = $"{currentViewList.Count} Constraints.";

            int unusedCount = (from a in currentViewList where a.NumberPoints <= 1 select a).Count();
            DeleteUnusedConstraintsButton_Content = $"Delete {unusedCount} Unused";
            DeleteUnusedConstraintsButton_IsEnabled = unusedCount > 0;

            DeleteSelectedConstraintButton_IsEnabled = false;

            // Clears the grid selection
            ConstraintCleanupDataGrid_Selected.Clear();
        }
        private bool ConstraintListFilter(object inObj)
        {
            if (inObj is ConstraintCleanupDataGridType constData)
            {
                if (!string.IsNullOrWhiteSpace(RegExFilter_Text))
                {
                    if (_RegExFilter != null && !_RegExFilter.IsMatch(constData.ConstraintName)) return false;
                }
            }

            return true;
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

                ConstraintCleanupDataGrid_ViewItems?.Refresh();
            }
        }
        private Regex _RegExFilter = null;

        private bool _DeleteUnusedConstraintsButton_IsEnabled = false;

        public bool DeleteUnusedConstraintsButton_IsEnabled
        {
            get => _DeleteUnusedConstraintsButton_IsEnabled;
            set => SetProperty(ref _DeleteUnusedConstraintsButton_IsEnabled, value);
        }

        private string _DeleteUnusedConstraintsButton_Content;

        public string DeleteUnusedConstraintsButton_Content
        {
            get => _DeleteUnusedConstraintsButton_Content;
            set => SetProperty(ref _DeleteUnusedConstraintsButton_Content, value);
        }

        private bool _DeleteSelectedConstraintButton_IsEnabled = false;

        public bool DeleteSelectedConstraintButton_IsEnabled
        {
            get => _DeleteSelectedConstraintButton_IsEnabled;
            set => SetProperty(ref _DeleteSelectedConstraintButton_IsEnabled, value);
        }

        private string _ConstraintReportSummaryTextBlock_Text;

        public string ConstraintReportSummaryTextBlock_Text
        {
            get => _ConstraintReportSummaryTextBlock_Text;
            set => SetProperty(ref _ConstraintReportSummaryTextBlock_Text, value);
        }

        private void ConstraintCleanupDataGrid_Selected_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ConstraintCleanupDataGrid_Selected.Count == 0)
            {
                DeleteSelectedConstraintButton_IsEnabled = false;
                return;
            }

            DeleteSelectedConstraintButton_IsEnabled = true;

            ConstraintCleanupDataGrid_Selected_CollectionChanged_asyncWork();
        }
        private async void ConstraintCleanupDataGrid_Selected_CollectionChanged_asyncWork()
        {
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Selecting Joints of Selected Constraints in SAP2000";

                    S2KModel.SM.ClearSelection();

                    BusyOverlayBindings.I.SetDeterminate("","Constraint");
                    for (int index = 0; index < ConstraintCleanupDataGrid_Selected.Count; index++)
                    {
                        ConstraintCleanupDataGridType selConstraint = ConstraintCleanupDataGrid_Selected[index];
                        BusyOverlayBindings.I.UpdateProgress(index, ConstraintCleanupDataGrid_Selected.Count, selConstraint.ConstraintName);
                        S2KModel.SM.PointMan.SelectElements(selConstraint.PointNames);
                    }

                    S2KModel.SM.RefreshView();
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
            }
        }

        public ICollectionView ConstraintCleanupDataGrid_ViewItems { get; set; }
        public FastObservableCollection<ConstraintCleanupDataGridType> ConstraintCleanupDataGrid_RawItems { get; } = new FastObservableCollection<ConstraintCleanupDataGridType>();
        public FastObservableCollection<ConstraintCleanupDataGridType> ConstraintCleanupDataGrid_Selected { get; } = new FastObservableCollection<ConstraintCleanupDataGridType>();

        private DelegateCommand _getConstraintReportButtonCommand;
        public DelegateCommand GetConstraintReportButtonCommand =>
            _getConstraintReportButtonCommand ?? (_getConstraintReportButtonCommand = new DelegateCommand(ExecuteGetConstraintReportButtonCommand));
        public async void ExecuteGetConstraintReportButtonCommand()
        {
            try
            {
                OnBeginCommand();

                RegExFilter_Text = "";

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Getting Constraint Report";

                    // Cleans-up the current list
                    ConstraintCleanupDataGrid_RawItems.Clear();

                    List<ConstraintCleanupDataGridType> cList = new List<ConstraintCleanupDataGridType>();

                    List<string> allCtes = S2KModel.SM.JointConstraintMan.GetConstraintList(true);
                    List<SapPoint> allPoints = S2KModel.SM.PointMan.GetAll(true);

                    allPoints.FillAllJointConstraints(true);

                    BusyOverlayBindings.I.SetDeterminate("Getting which point belongs to each constraint.");
                    for (int i = 0; i < allCtes.Count; i++)
                    {
                        string cte = allCtes[i];
                        BusyOverlayBindings.I.UpdateProgress(i, allCtes.Count);

                        ConstraintCleanupDataGridType data = new ConstraintCleanupDataGridType
                            {
                            ConstraintName = cte,
                            ConstraintType = S2KModel.SM.JointConstraintMan.GetConstraintType(cte)
                            };

                        // Finds the points
                        IEnumerable<SapPoint> pntsInCte = from a in allPoints
                            where a.JointConstraintNames.Contains(cte)
                            select a;

                        data.NumberPoints = pntsInCte.Count();

                        // Gets the maximum distance between all of them
                        double? maxDist = null;
                        if (data.NumberPoints > 1)
                        {
                            data.PointNames = (from a in pntsInCte select a.Name).ToArray();

                            maxDist = double.MinValue;
                            foreach (SapPoint curr in pntsInCte)
                            foreach (SapPoint other in pntsInCte)
                                if (curr != other)
                                {
                                    double dist = curr.Point.DistanceTo(other.Point);
                                    if (dist > maxDist) maxDist = dist;
                                }
                        }

                        data.PointMaxDistance = maxDist;

                        // Adds the data
                        cList.Add(data);
                    }

                    ConstraintCleanupDataGrid_RawItems.AddItems(cList);
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
            }
        }

        private DelegateCommand _deleteUnusedButtonCommand;
        public DelegateCommand DeleteUnusedButtonCommand =>
            _deleteUnusedButtonCommand ?? (_deleteUnusedButtonCommand = new DelegateCommand(ExecuteDeleteUnusedButtonCommand));
        public async void ExecuteDeleteUnusedButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Deleting Unused Constraints";

                    // Filters list of constraint to delete
                    var toDelete = (from a in ConstraintCleanupDataGrid_ViewItems.OfType<ConstraintCleanupDataGridType>()
                        where a.NumberPoints == 0 || a.NumberPoints == 1
                        select a).ToList();

                    if (toDelete.Count == 0)
                    {
                        S2KStaticMethods.ShowWarningMessageBox(
                            "There is no currently shown constraint with either 0 or 1 point attached to it. Therefore, there is nothing to delete.");
                        return;
                    }

                    BusyOverlayBindings.I.SetDeterminate("Deleting", "Constraint");
                    for (int i = 0; i < toDelete.Count; i++)
                    {
                        ConstraintCleanupDataGridType cte = toDelete[i];
                        BusyOverlayBindings.I.UpdateProgress(i, toDelete.Count, cte.ConstraintName);

                        if (!S2KModel.SM.JointConstraintMan.DeleteConstraint(cte.ConstraintName))
                            endMessages.AppendLine(cte.ConstraintName);
                    }

                    ConstraintCleanupDataGrid_RawItems.Clear();
                    
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
                RegExFilter_Text = "";

                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0)
                    OnMessage("Could not delete the following constraints", endMessages.ToString());
            }
        }

        private DelegateCommand _deleteSelectedButtonCommand;
        public DelegateCommand DeleteSelectedButtonCommand =>
            _deleteSelectedButtonCommand ?? (_deleteSelectedButtonCommand = new DelegateCommand(ExecuteDeleteSelectedButtonCommand));
        public async void ExecuteDeleteSelectedButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Deleting Selected Constraints";

                    // Gets the selected items
                    if (ConstraintCleanupDataGrid_Selected == null ||
                        ConstraintCleanupDataGrid_Selected.Count == 0) return;
                    
                    BusyOverlayBindings.I.SetDeterminate("", "Constraint");
                    for (int index = 0; index < ConstraintCleanupDataGrid_Selected.Count; index++)
                    {
                        ConstraintCleanupDataGridType selCons = ConstraintCleanupDataGrid_Selected[index];
                        BusyOverlayBindings.I.UpdateProgress(index, ConstraintCleanupDataGrid_Selected.Count, selCons.ConstraintName);

                        if (!S2KModel.SM.JointConstraintMan.DeleteConstraint(selCons.ConstraintName))
                            endMessages.AppendLine(selCons.ConstraintName);

                        ConstraintCleanupDataGrid_RawItems.Remove(selCons);
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
                    OnMessage("Could not delete the following constraints", endMessages.ToString());
            }
        }
    }
}