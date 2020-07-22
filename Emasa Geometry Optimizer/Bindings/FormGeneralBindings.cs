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
            set => SetProperty(ref _currentProblem, value);
        }

         #region Display Variables

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
                    
                    if (CurrentProblem.Status == SolverStatus.NotStarted) {
                        // Set a Title to the Busy Overlay
                        CustomOverlayBindings.I.Title = "Initializing the Solver.";

                        CurrentProblem.SetSolverManager();
                    }

                    // Set a Title to the Busy Overlay
                    CustomOverlayBindings.I.Title = "Solving the Problem.";
                    CustomOverlayBindings.I.MessageText = $"Solving the problem {CurrentProblem.ProblemFriendlyName} using the {CurrentProblem.SolverTypeListWithCaptions[CurrentProblem.SolverType]}";

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
            
            // Asks for confirmation depending on the solve status
            if (CurrentProblem.Status != SolverStatus.NotStarted)
            {
                MessageBoxResult r = MessageBox.Show("This will delete the current data. Are you sure you want to proceed?", "Please confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.No) return;
            }

            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    CustomOverlayBindings.I.Title = "Resetting the problem.";

                    // Cleans-up the solver
                    CurrentProblem.CleanUpSolver_NewSolve();

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
