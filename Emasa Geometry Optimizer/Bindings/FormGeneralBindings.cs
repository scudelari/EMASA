extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using Microsoft.Win32;
using Prism.Commands;
using RhinoInterfaceLibrary;
using System.Data.SQLite;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AccordHelper.FEA;
using AccordHelper.Opt;
using AccordHelper.Opt.ParamDefinitions;
using BaseWPFLibrary;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Others;
using Emasa_Geometry_Optimizer.GHVars;
using Emasa_Geometry_Optimizer.ProblemDefs;
using Emasa_Geometry_Optimizer.Properties;
using r3dm::Rhino.Geometry;

namespace Emasa_Geometry_Optimizer.Bindings
{
    public class FormGeneralBindings : BindableSingleton<FormGeneralBindings>
    {
        private FormGeneralBindings(){}

        public override async void SetOrReset()
        {
            try
            {
                // Puts the active Rhino Instance in the Singleton
                RhinoModel.Initialize();
                RhinoModel.RM.RhinoVisible = true;

                // Gets the current GH file. Will throw if not available
                GrasshopperFullFileName = RhinoModel.RM.GrasshopperFullFileName;

                // Adding a list of Problems that have been implemented
                _possibleProblems = new List<ProblemBase>()
                    {
                    new FindTriangleProblem(),
                    new BestArchProblem()
                    };
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Rhino/Grasshopper Initialization Issue.");
                Application.Current.Shutdown(1);
            }

            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    CustomOverlayBindings.I.Title = "Getting the Grasshopper Input and Output variable list.";

                    #region Managing the GH Parameters
                    // First, gets a list of the input variables and their types from the GH definition
                    List<(string Name, string Type)> tmpFolderInputVars = new List<(string Name, string Type)>();
                    string inputVarFolder = RhinoStaticMethods.GH_Auto_InputVariableFolder(RhinoModel.RM.GrasshopperFullFileName);
                    foreach (string inputFile in Directory.GetFiles(inputVarFolder))
                    {
                        string name = Path.GetFileNameWithoutExtension(inputFile);
                        string extension = Path.GetExtension(inputFile).Trim(new[] { '.' });

                        tmpFolderInputVars.Add((name, extension));
                    }
                    if (tmpFolderInputVars.Count == 0) throw new Exception($"Could not find input definitions for the current Grasshopper file.");

                    // First, gets a list of the output variables and their types from the GH definition
                    List<(string Name, string Type)> tmpFolderOutputVars = new List<(string Name, string Type)>();
                    string outputVarFolder = RhinoStaticMethods.GH_Auto_OutputVariableFolder(RhinoModel.RM.GrasshopperFullFileName);
                    foreach (string outputFile in Directory.GetFiles(outputVarFolder))
                    {
                        string name = Path.GetFileNameWithoutExtension(outputFile);
                        string extension = Path.GetExtension(outputFile).Trim(new[] { '.' });

                        tmpFolderOutputVars.Add((name, extension));
                    }
                    if (tmpFolderOutputVars.Count == 0) throw new Exception($"Could not find intermediate (Grasshopper outputs) definitions for the current Grasshopper file.");

                    CustomOverlayBindings.I.Title = "Finding, in the problem library, a problem that can solve this Grasshopper file.";

                    // Marks the problems that match the current GH file
                    int countValid = 0;
                    foreach (ProblemBase problem in _possibleProblems)
                    {
                        if (!problem.ObjectiveFunction.InputDefs.All(a =>
                            tmpFolderInputVars.Any(b =>
                                b.Name == a.Name && b.Type == a.TypeName))) continue;

                        if (!problem.ObjectiveFunction.IntermediateDefs.All(a =>
                            tmpFolderOutputVars.Any(b =>
                                b.Name == a.Name && b.Type == a.TypeName))) continue;

                        problem.SolvesCurrentGHFile = true;
                        countValid++;
                    }

                    // Checks the number of valid problems
                    if (countValid == 0)
                        throw new Exception($"Could not find any problem that has the variables matching the ones given in the Grasshopper file.{Environment.NewLine}Please select a valid Grasshopper file or write the problem to this geometry.");

                    if (countValid > 1)
                        throw new Exception($"Found two or more possible problems for this Grasshopper file. This is currently not supported.");

                    // We only found one possible Problem, thus we are good to go.


                    // Finds a file that has been saved and loads the current status
                    if (File.Exists(RhinoStaticMethods.GH_Auto_SavedStateFileFull()))
                    {
                        throw new NotImplementedException($"Does not support Loading - yet.");
                    }
                    else // Could not find any problem that has been saved - Sets the default one as the current
                    {
                        CurrentProblem = _possibleProblems.First(a => a.SolvesCurrentGHFile);
                    }
                    #endregion

                    // Asks the main window to come back to the front
                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
                Application.Current.Shutdown(1);
            }
            finally
            {
                OnEndCommand();
            }
        }

        private List<ProblemBase> _possibleProblems;

        private ProblemBase _currentProblem;
        public ProblemBase CurrentProblem
        {
            get => _currentProblem;
            set
            {
                SetProperty(ref _currentProblem, value);

                // Sets the solver control variables 
                SolverOptions_SelectedSolverType = _currentProblem.SolverType;
                SolverOptions_SelectedStartPositionType = _currentProblem.StartPositionType;

                Dictionary<FeaSoftwareEnum, string> tempFeaSoftwareList = new Dictionary<FeaSoftwareEnum, string>();
                foreach (FeaSoftwareEnum supportedFeaSoftware in _currentProblem.SupportedFeaSoftwares)
                {
                    switch (supportedFeaSoftware)
                    {
                        case FeaSoftwareEnum.Ansys:
                            tempFeaSoftwareList.Add(FeaSoftwareEnum.Ansys, "Ansys");
                            break;

                        case FeaSoftwareEnum.Sap2000:
                            tempFeaSoftwareList.Add(FeaSoftwareEnum.Sap2000, "Sap2000");
                            break;

                        case FeaSoftwareEnum.NoFea:
                            tempFeaSoftwareList.Add(FeaSoftwareEnum.NoFea, "No Fea");
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                FeaSoftwareListWithCaptions = tempFeaSoftwareList;

                ChangeLimit = _currentProblem.ChangeLimit;
            }
        }



        #region Solver Options - Display!
        public Dictionary<SolverType, string> SolverTypeListWithCaptions { get; } = new Dictionary<SolverType, string>()
            {
                {SolverType.Cobyla, "Cobyla"},
                {SolverType.BoundedBroydenFletcherGoldfarbShanno, "B-BFGS"},
                {SolverType.AugmentedLagrangian, "Aug. Lagrangian"},
                {SolverType.Genetic, "Genetic"},
                {SolverType.ConjugateGradient_FletcherReeves, "Conj. Gradient - Fletcher Reeves"},
                {SolverType.ConjugateGradient_PolakRibiere, "Conj. Gradient - Polak Ribiere"},
                {SolverType.ConjugateGradient_PositivePolakRibiere, "Conj. Gradient - [+] Polak Ribiere"},
                {SolverType.NelderMead, "Nelder Mead"},
                {SolverType.ResilientBackpropagation, "Resilient Backpropagation"},
            };
        private SolverType _solverOptions_SelectedSolverType = SolverType.Cobyla;
        public SolverType SolverOptions_SelectedSolverType
        {
            get => _solverOptions_SelectedSolverType;
            set => SetProperty(ref _solverOptions_SelectedSolverType, value);
        }

        public Dictionary<StartPositionType, string> StartPositionTypeListWithCaptions { get; } = new Dictionary<StartPositionType, string>()
            {
                {StartPositionType.Random, "Random"},
                {StartPositionType.CenterOfRange, "Center of Input"},
                {StartPositionType.TenPercentRandomFromCenter, "10% from Center"},
            };
        private StartPositionType _solverOptions_SelectedStartPositionType = StartPositionType.CenterOfRange;
        public StartPositionType SolverOptions_SelectedStartPositionType
        {
            get => _solverOptions_SelectedStartPositionType;
            set => SetProperty(ref _solverOptions_SelectedStartPositionType, value);
        }


        private Dictionary<FeaSoftwareEnum, string> _feaSoftwareListWithCaptions = new Dictionary<FeaSoftwareEnum, string>();
        public Dictionary<FeaSoftwareEnum, string> FeaSoftwareListWithCaptions
        {
            get => _feaSoftwareListWithCaptions;
            set
            {
                SetProperty(ref _feaSoftwareListWithCaptions, value);

                if (_feaSoftwareListWithCaptions.Count > 0)
                {
                    SolverOptions_SelectedFeaSoftware = _feaSoftwareListWithCaptions.First().Key;
                    if (_feaSoftwareListWithCaptions.Count == 1) SolverOptions_FeaSoftwareIsEnabled = false;
                    else SolverOptions_FeaSoftwareIsEnabled = true;
                }
                else SolverOptions_FeaSoftwareIsEnabled = false;
            }
        }
        private FeaSoftwareEnum _solverOptions_SelectedFeaSoftware = FeaSoftwareEnum.NoFea;
        public FeaSoftwareEnum SolverOptions_SelectedFeaSoftware
        {
            get => _solverOptions_SelectedFeaSoftware;
            set => SetProperty(ref _solverOptions_SelectedFeaSoftware, value);
        }
        private bool _solverOptions_FeaSoftwareIsEnabled = false;
        public bool SolverOptions_FeaSoftwareIsEnabled
        {
            get => _solverOptions_FeaSoftwareIsEnabled;
            set => SetProperty(ref _solverOptions_FeaSoftwareIsEnabled, value);
        }


        private double _changeLimit;
        public double ChangeLimit
        {
            get => _changeLimit;
            set => SetProperty(ref _changeLimit, value);
        }

        private double _targetResidual = 1e-3d;
        public double TargetResidual
        {
            get => _targetResidual;
            set => SetProperty(ref _targetResidual, value);
        }

        private bool _isEnabledForm = true;
        public bool IsEnabled_Form
        {
            get => _isEnabledForm;
            set => SetProperty(ref _isEnabledForm, value);
        }

        private bool _isEnabled_SolveManagement = true;
        public bool IsEnabled_SolveManagement
        {
            get => _isEnabled_SolveManagement;
            set => SetProperty(ref _isEnabled_SolveManagement, value);
        }

        #endregion


        public string GrasshopperFullFileName
        {
            get;
            private set;
        }
        public string GrasshopperDirectory
        {
            get => Path.GetDirectoryName(GrasshopperFullFileName);
        }
        public string GrasshopperFileName
        {
            get => Path.GetFileName(GrasshopperFullFileName);
        }

        //private bool ReadProblemDataFromDisk(string inFileName, out ProblemBase outProblem)
        //{
        //    outProblem = null;
        //    if (!File.Exists(inFileName)) return false;
            
        //    try
        //    {
        //        ProblemBase savedProblem = null;

        //        IFormatter formatter = new BinaryFormatter();

        //        // First, tries to simply read the binary data
        //        using (Stream stream = new FileStream(inFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        //        {
        //            savedProblem = (ProblemBase) formatter.Deserialize(stream);
        //        }

        //        // Validates if the object that was read is of the selected type
        //        if (savedProblem.GetType() != SelectedProblemInList.GetType())
        //        {
        //            throw new Exception("The type of the problem saved in the file does not match the selected type.");
        //        }

        //        outProblem = savedProblem;
        //    }
        //    catch
        //    {
        //        // Deletes the currently existing file
        //        File.Delete(inFileName);
        //        return false;
        //    }

        //    return true;

        //}

        private DelegateCommand _solveButtonCommand;
        public DelegateCommand SolveButtonCommand =>
            _solveButtonCommand ?? (_solveButtonCommand = new DelegateCommand(ExecuteSolveButtonCommand));
        public async void ExecuteSolveButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    IsEnabled_SolveManagement = false;
                    
                    if (CurrentProblem.Status == SolverStatus.NotInitialized) {
                        // Set a Title to the Busy Overlay
                        CustomOverlayBindings.I.Title = "Initializing the Solver.";

                        // Sets the selected variables
                        CurrentProblem.TargetResidual = TargetResidual;
                        CurrentProblem.ChangeLimit = ChangeLimit;
                        CurrentProblem.FeaType = SolverOptions_SelectedFeaSoftware;
                        CurrentProblem.StartPositionType = SolverOptions_SelectedStartPositionType;
                        CurrentProblem.SolverType = SolverOptions_SelectedSolverType;

                        CurrentProblem.ResetSolver();
                    }

                    // Set a Title to the Busy Overlay
                    CustomOverlayBindings.I.Title = "Solving the Problem.";
                    CustomOverlayBindings.I.MessageText = $"Solving the problem {CurrentProblem.ProblemFriendlyName} using the {SolverTypeListWithCaptions[SolverOptions_SelectedSolverType]}";

                    CurrentProblem.Solve();
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

        private DelegateCommand _newSolverCommand;
        public DelegateCommand NewSolverCommand =>
            _newSolverCommand ?? (_newSolverCommand = new DelegateCommand(ExecuteNewSolverCommand));
        public async void ExecuteNewSolverCommand()
        {
            StringBuilder endMessages = new StringBuilder();
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    CustomOverlayBindings.I.Title = "Resetting the problem.";

                    // Cleans-up the solver
                    CurrentProblem.CleanUpSolver();

                    IsEnabled_SolveManagement = true;
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
                    OnMessage("Messages: ", endMessages.ToString());
            }
        }

        private DelegateCommand _cancelSolveCommand;
        public DelegateCommand CancelSolveCommand => _cancelSolveCommand ?? (_cancelSolveCommand = new DelegateCommand(ExecuteCancelSolveCommand));
        public void ExecuteCancelSolveCommand()
        {
            if (!CurrentProblem.CancelSource.IsCancellationRequested) CurrentProblem.CancelSource.Cancel();
        }
    }

}
