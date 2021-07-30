extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaseWPFLibrary;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Extensions;
using BaseWPFLibrary.Others;
using CsvHelper;
using CsvHelper.Configuration;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Loads;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ProbQuantity;
using Prism.Modularity;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA
{
    public class FeSolverBase_Ansys : FeSolverBase
    {
        private string _ansysExeLocation = @"C:\Program Files\ANSYS Inc\v201\ansys\bin\winx64\ANSYS201.exe";
        private const string _jobName = "fe_job";

        public FeSolverBase_Ansys() 
        {
        }

        private Process _commandLineProcess = null;
        public override void CleanUpDirectory()
        {
            int count = 0;
            while (true)
            {
                try
                {
                    // Attempts to delete the folder
                    if (Directory.Exists(FeWorkFolder)) Directory.Delete(FeWorkFolder, true);

                    // Waits until the directory is really deleted - the Directory. Delete just marks the directory for deletion by the OS.
                    if (!Directory.Exists(FeWorkFolder)) break;
                }
                catch {}

                Thread.Sleep(50);
                count++;
                if (count > 100) throw new IOException($"Could not clean-up the folder to be used as Ansys's buffer: {FeWorkFolder}");
            }
        }
        protected override void InitializeSoftware()
        {
            string jobEaterString = $@"! JOB EATER
! -------------------------------------

! Sets some parameters for the charts
/GRA,POWER
/GST,ON
/PLO,INFO,3
/GRO,CURL,ON
/CPLANE,1   
/REPLOT,RESIZE  
WPSTYLE,,,,,,,,0

/NERR,50000,50000,,OFF,0        ! Sets the error handling to avoid breaking the calculations.

! Sets the directory
/CWD,'{FeWorkFolder}'

! Writes a file to signal that Ansys is Ready
/OUTPUT,'ems_signal_ansys_ready','control'
/OUTPUT

:BEGIN

/CLEAR   ! Clears the current problem

! Waits for the start signal
start_exists = 0
end_exists = 0

/WAIT,0.050 ! Waits on the iteration

/INQUIRE,start_exists,EXIST,'ems_signal_start','control'            ! Gets the file exist info
/INQUIRE,end_exists,EXIST,'ems_signal_terminate','control'          ! Gets the file exist info

*IF,start_exists,EQ,1,THEN ! Start signal received
	/INPUT,'model_input_file','dat',,,1                             ! Reads the new iteration file

    SAVE,ALL               ! Saves the database
    FINISH                 ! Closes the data

	/DELETE,'ems_signal_start','control'                            ! Deletes the signal file - should take time and thus the start signal file should be released by then

	! Writes a file to signal that the iteration has finished
	/OUTPUT,'ems_signal_iteration_finish','control'
	/OUTPUT
*ENDIF

*IF,end_exists,EQ,0,:BEGIN                                 ! If the end signal file does not exist, jumps to the beginning

/DELETE,'ems_signal_terminate','control'                             ! Deletes the signal file";
            string jobEaterFileName = Path.Combine(FeWorkFolder, "Job_Eater.dat");

            // Writes the output file. Tries twice
            if (!Directory.Exists(FeWorkFolder))
            {
                Directory.CreateDirectory(FeWorkFolder);
                // wait until directory exists
            }
            File.WriteAllText(jobEaterFileName, jobEaterString);

            _commandLineProcess = new Process
            {
                StartInfo =
                    {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = FeWorkFolder,
                    CreateNoWindow = true,
                    UseShellExecute = false
                    }
            };

            _commandLineProcess.Start();

            _commandLineProcess.StandardInput.Write($"CD \"{FeWorkFolder}\"");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANSYS201_PRODUCT=ANSYS");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANS_CONSEC=YES");
            _commandLineProcess.StandardInput.Flush();

            // Launches the Ansys process that will keep consuming the iterations
            string cmdString = $"\"{_ansysExeLocation}\" -s noread -b -j {_jobName} -i \"{jobEaterFileName}\" -o \"{jobEaterFileName + "_out"}\"";

            // Issues the Ansys command
            _commandLineProcess.StandardInput.WriteLine(cmdString);
            _commandLineProcess.StandardInput.Flush();

            // Waits for the Ansys software to finish loading
            string ansysReadyFile = Path.Combine(FeWorkFolder, "ems_signal_ansys_ready.control");
            while (true)
            {
                if (File.Exists(ansysReadyFile)) break;
            }
            while (File.Exists(ansysReadyFile))
            {
                try
                {
                    File.Delete(ansysReadyFile);
                }
                catch { }
            }
        }
        protected override void CloseApplication()
        {
            // Updates the reference to the running instance
            _commandLineProcess = GetRunningCmdInstance();

            if (_commandLineProcess != null)
            {
                // Writes the termination signal file
                string endSignalPath = Path.Combine(FeWorkFolder, "ems_signal_terminate.control");
                if (File.Exists(endSignalPath)) File.Delete(endSignalPath);
                File.WriteAllText(endSignalPath, " ");

                try
                {
                    // This will be available only if Ansys had been already opened before
                    _commandLineProcess.StandardInput.Close();
                }
                catch { }

                _commandLineProcess.WaitForExit();
                _commandLineProcess.Dispose();

                _commandLineProcess = null; 
            }
        }


        public Process GetRunningCmdInstance()
        {
            List<Process> ansysProcs = GetAllRunningProcesses();
            
            // If nothing found, returns null
            if (ansysProcs.Count == 0) return null;

            // If found more than one, clear all list
            if (ansysProcs.Count > 1)
            {
                KillAllRunningProcesses(ansysProcs);
                return null;
            }

            // Has only one, but it isn't a cmd
            if (ansysProcs.First().ProcessName != "cmd")
            {
                KillAllRunningProcesses(ansysProcs);
                return null;
            }

            return ansysProcs.First();
        }
        public override List<Process> GetAllRunningProcesses()
        {
            // Gets all Ansys processes
            List<Process> ansysProcs = Process.GetProcesses().Where(a => a.ProcessName.Contains(Path.GetFileNameWithoutExtension(_ansysExeLocation))).ToList();

            // If nothings was found, simply returns the empty list
            if (ansysProcs.Count == 0) return ansysProcs;

            // Makes a new list
            List<Process> ansysOrParentCmd = new List<Process>();
            foreach (Process ansysProc in ansysProcs)
            {
                Process parentCmd = ansysProc.Parent();
                if (parentCmd != null && parentCmd.ProcessName == ("cmd"))
                {
                    ansysOrParentCmd.Add(parentCmd);
                }
                else
                {
                    ansysOrParentCmd.Add(ansysProc);
                }
            }

            return ansysOrParentCmd;
        }

        public override void ResetSoftwareData()
        {
            AppSS.I.UpdateOverlayTopMessage("Ansys: Clearing Old Solution Files (will keep trying for 2 seconds).", "");

            Stopwatch attemptStopWatch = Stopwatch.StartNew();

            // Must clean-up the data for the next iteration
            DirectoryInfo dInfo = new DirectoryInfo(FeWorkFolder);
            if (!dInfo.Exists) throw new FeSolverException("Could not reset the Ansys folder between iterations.", new IOException($"The model directory does not exist; something is really wrong."));

            while (true)
            {
                try
                {
                    // Deletes the output data
                    foreach (FileInfo fileInfo in dInfo.GetFiles("ems_output_*"))
                    {
                        fileInfo.Delete();
                    }

                    // Deletes the created images
                    foreach (FileInfo fileInfo in dInfo.GetFiles("ems_image_*.png"))
                    {
                        fileInfo.Delete();
                    }

                    // Deletes the model input file
                    if (File.Exists(Path.Combine(dInfo.FullName, "model_input_file.dat"))) File.Delete(Path.Combine(dInfo.FullName, "model_input_file.dat"));

                    // Perhaps some other files should also be deleted 
                    break;
                }
                catch (Exception e)
                {
                    // Stops to check the elapsed time
                    attemptStopWatch.Stop();
                    if (attemptStopWatch.Elapsed.TotalSeconds > 2d) throw new FeSolverException("Could not reset the Ansys folder between iterations. Retried for 2 seconds.", e);

                    // Resumes counting time
                    attemptStopWatch.Start();
                    Thread.Sleep(250);
                }
            }
        }
        
        internal readonly StringBuilder Sb = new StringBuilder();
        private FeModel _model = null;

        private HashSet<string> ExpectedOutputNotConsumedList = new HashSet<string>();

        /// <summary>
        /// Based on the information existing in the _model, will create a complete APDL into the Sb string builder
        /// </summary>
        private void CreateInputFile()
        {
            try
            {
                #region Writes the Input File

                #region Perfect Shape Static Analysis

                // Writes the commands for the perfect analysis
                File_AppendHeader("Model generated using EMASA's Rhino Automator.", $"NlOpt_Point: {_model.Owner.PointIndex} - {_model.Owner.NlOptPointCalcType}", "Perfect Shape - Static Analysis Setup And Run");
                WriteHelper_PerfectAnalysis();

                // Writes the Perfect Analysis' Results - with the exception of the Eigenvalue Buckling
                File_AppendHeader("Perfect Shape - Static Analysis Results Data");

                // Clears the buffer of the touched families
                _writeResultTouchedTypes.Clear();

                foreach (FeResultClassification feResultSelectStatusClass in
                    AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape &&
                                                                      !a.IsEigenValueBuckling))
                {
                    WriteHelper_WriteResults(feResultSelectStatusClass);
                }

                // Writes the special screenshots that are due to the problem definition
                File_AppendHeader("Perfect Shape - Overview Screenshots - Keypoints");
                WriteHelper_WriteScreenShots(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance);
                File_AppendHeader("Perfect Shape - Overview Screenshots - Lines");
                WriteHelper_WriteScreenShots(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotLinesInstance);

                // Writes the ScreenShot for the Perfect Analysis' Results - with the exception of the Eigenvalue Buckling
                File_AppendHeader("Perfect Shape - Static Analysis Results ScreenShots");
                foreach (FeResultClassification feResultSelectStatusClass in
                    AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape &&
                                                                      !a.IsEigenValueBuckling))
                {
                    WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                }
                #endregion

                // Should we add more analysis and results?
                if (AppSS.I.FeOpt.IsPerfectShape_EigenvalueBucking_Required ||
                    AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required ||
                    AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
                {

                    #region Perfect Shape Eigenvalue Buckling Analysis

                    // Sets and runs the Eigenvalue Buckling
                    File_AppendHeader("Perfect Shape - Eigenvalue Buckling Setup And Run");
                    WriteHelper_EigenvalueBucklingAnalysis();

                    // Output results for the Eigenvalue Buckling analysis
                    File_AppendHeader("Perfect Shape - Eigenvalue Buckling Results Data");
                    foreach (FeResultClassification feResultSelectStatusClass in
                        AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape &&
                                                                          a.IsEigenValueBuckling))
                    {
                        WriteHelper_WriteResults(feResultSelectStatusClass);
                    }

                    // Outputs the Screenshots for the Eigenvalue Buckling analysis
                    File_AppendHeader("Perfect Shape - Eigenvalue Buckling Results ScreenShots");
                    foreach (FeResultClassification feResultSelectStatusClass in
                        AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape &&
                                                                          a.IsEigenValueBuckling))
                    {
                        WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                    }

                    #endregion

                    if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required ||
                        AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
                    {
                        // Uses the selected eigenvalue buckling shape to deform the original geometry
                        File_AppendHeader("Imperfect Shape - Update Geometry With Eigenvalue Buckling Analysis' Deformations");
                        WriteHelper_ImperfectAnalysis_UpdateGeometryUsingEigenvalueBucklingShape();

                        #region Imperfect Shape Static Analysis - Full Stiffness

                        if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required)
                        {
                            // Runs the new Imperfect Geometry 
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Static Analysis Setup And Run");
                            WriteHelper_ImperfectAnalysis_Solve();

                            // Writes the Imperfect Analysis' Results - with the exception of the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Static Analysis Results Data");
                            // Clears the buffer of the touched families
                            _writeResultTouchedTypes.Clear();
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                  !a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteResults(feResultSelectStatusClass);
                            }

                            // Writes the ScreenShot for the Imperfect Analysis' Results - with the exception of the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Static Analysis Results ScreenShots");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                  !a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                            }
                        }

                        #endregion

                        #region Imperfect Shape Eigenvalue Buckling Analysis - Full Stiffness

                        if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_EigenvalueBuckling_Required)
                        {
                            // Sets and runs the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Eigenvalue Buckling Setup And Run");
                            WriteHelper_EigenvalueBucklingAnalysis();

                            // Output results for the Eigenvalue Buckling analysis
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Eigenvalue Buckling Results Data");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                  a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteResults(feResultSelectStatusClass);
                            }

                            // Outputs the Screenshots for the Eigenvalue Buckling analysis
                            File_AppendHeader("Imperfect Shape - Full Stiffness - Eigenvalue Buckling Results ScreenShots");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness &&
                                                                                  a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                            }
                        }

                        #endregion

                        #region Imperfect Shape Static Analysis - Softened

                        if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
                        {
                            // Runs the new Imperfect Geometry 
                            File_AppendHeader("Imperfect Shape - Softened - Static Analysis Setup And Run");
                            WriteHelper_ImperfectAnalysis_Soften();
                            WriteHelper_ImperfectAnalysis_Solve();

                            // Writes the Imperfect Analysis' Results - with the exception of the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Softened - Static Analysis Results Data");
                            // Clears the buffer of the touched families
                            _writeResultTouchedTypes.Clear();
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                  !a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteResults(feResultSelectStatusClass);
                            }

                            // Writes the ScreenShot for the Imperfect Analysis' Results - with the exception of the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Softened - Static Analysis Results ScreenShots");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                  !a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                            }
                        }

                        #endregion

                        #region Imperfect Shape Eigenvalue Buckling Analysis - Softened

                        if (AppSS.I.FeOpt.IsImperfectShapeSoftened_EigenvalueBuckling_Required)
                        {
                            // Sets and runs the Eigenvalue Buckling
                            File_AppendHeader("Imperfect Shape - Softened - Eigenvalue Buckling Setup And Run");
                            WriteHelper_EigenvalueBucklingAnalysis();

                            // Output results for the Eigenvalue Buckling analysis
                            File_AppendHeader("Imperfect Shape - Softened - Eigenvalue Buckling Results Data");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                  a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteResults(feResultSelectStatusClass);
                            }

                            // Outputs the Screenshots for the Eigenvalue Buckling analysis
                            File_AppendHeader("Imperfect Shape - Softened - Eigenvalue Buckling Results ScreenShots");
                            foreach (FeResultClassification feResultSelectStatusClass in
                                AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>().Where(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened &&
                                                                                  a.IsEigenValueBuckling))
                            {
                                WriteHelper_WriteScreenShots(feResultSelectStatusClass);
                            }
                        }

                        #endregion
                    }
                }

                #endregion
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Could not create the input file text. {e.Message}");
            }
        }

        public override void RunAnalysisAndCollectResults([NotNull] FeModel inModel)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));
            
            // Refreshes the link to the running Apdl instance, if it exists
            _commandLineProcess = GetRunningCmdInstance();
            if (_commandLineProcess == null)
            {
                Stopwatch sw = Stopwatch.StartNew();

                AppSS.I.UpdateOverlayTopMessage("Ansys: Initializing APDL Solver.", "");

                // Starts the software
                CleanUpDirectory();
                InitializeSoftware();

                sw.Stop();
                InitializingSoftwareTimeSpan += sw.Elapsed;
            }

            // Cleanup of previous analysis
            try
            {
                ResetSoftwareData();
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Error while resetting the Ansys software data. {e.Message}");
            }
            _errorLogLines.Clear();
            Sb.Clear();
            _model = null;
            ExpectedOutputNotConsumedList.Clear();

            // Saves temporarily a reference to the model for simpler access in the helper functions
            _model = inModel;
            AppSS.I.UpdateOverlayTopMessage("Ansys: Generating Analysis Input File.", "");
            CreateInputFile();

            try
            {
                // Writes the file
                File.WriteAllText(Path.Combine(FeWorkFolder, "model_input_file.dat"), Sb.ToString());
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Could not write the input file to the disk. {e.Message}");
            }

            AppSS.I.UpdateOverlayTopMessage("Ansys: Solving and Writing Results.", "");

            #region Sends a signal to start the analysis
            try
            {
                string startSignalPath = Path.Combine(FeWorkFolder, "ems_signal_start.control");
                if (File.Exists(startSignalPath)) File.Delete(startSignalPath);
                File.WriteAllText(startSignalPath, " ");
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Could not send (write to disk) the ems_signal_start.control signal. {e.Message}");
            }
            #endregion

            #region Pools for the analysis finished message - will monitor for errors and if Ansys is still up and running...
            string finishFile = Path.Combine(FeWorkFolder, "ems_signal_iteration_finish.control");
            while (true)
            {
                // Will check for errors in the log file and throw if errors are found.
                // Also throws if the process terminated
                GetErrorsAndThrow();


                // If the finish file exists, delete it and release the loop
                if (File.Exists(finishFile))
                {
                    int finishFileAttemptFailedCount = 0;
                    while (true)
                    {
                        try
                        {
                            File.Delete(finishFile);
                            break;
                        }
                        catch // The deletion *may* fail as Ansys may still have the signal file open - retry 5 times.
                        {
                            Thread.Sleep(50);
                            finishFileAttemptFailedCount++;
                            if (finishFileAttemptFailedCount > 5) throw new IOException($"The ems_signal_iteration_finish.control was created by Ansys but the optimizer cannot get hold of it.");
                        }
                    }

                    break;
                }

                Thread.Sleep(50);
            }
            #endregion

            AppSS.I.UpdateOverlayTopMessage("Ansys: Reading the Results.", "");

            try
            {
                #region Reading the results

                // Consumes the nodal locations
                string nodalLocationsFile = Path.Combine(FeWorkFolder, "ems_output_meshinfo_nodal_locations.txt");
                while (true)
                {
                    if (File.Exists(nodalLocationsFile))
                    {
                        if (ReadHelper_ReadResultFile(nodalLocationsFile))
                        {
                            // The file matched a result and was read
                            ExpectedOutputNotConsumedList.Remove(Path.GetFileName(nodalLocationsFile));
                            File.Delete(nodalLocationsFile);
                            break;
                        }

                        throw new FeSolverException($"Problem reading the ems_output_meshinfo_nodal_locations file.txt");
                    }

                    Thread.Sleep(50);
                }

                // Consumes the ems_output_meshinfo_elements_of_lines
                string elementOfLinesFile = Path.Combine(FeWorkFolder, "ems_output_meshinfo_elements_of_lines.txt");
                while (true)
                {
                    if (File.Exists(elementOfLinesFile))
                    {
                        if (ReadHelper_ReadResultFile(elementOfLinesFile))
                        {
                            // The file matched a result and was read
                            ExpectedOutputNotConsumedList.Remove(Path.GetFileName(elementOfLinesFile));
                            File.Delete(elementOfLinesFile);
                            break;
                        }

                        throw new FeSolverException($"Problem reading the ems_output_meshinfo_elements_of_lines file.txt");
                    }

                    Thread.Sleep(50);
                }

                // Consumes the rest of th results
                while (true)
                {
                    // Gets the list of files in the directory - the files will be consumed (deleted) as they appear in each waiting iteration
                    string[] files = Directory.GetFiles(FeWorkFolder); // Includes the Path!
                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).Contains("ems_output_"))
                        {
                            if (ReadHelper_ReadResultFile(file))
                            {
                                // The file matched a result and was read
                                ExpectedOutputNotConsumedList.Remove(Path.GetFileName(file));
                                File.Delete(file);
                            }
                        }
                        else if (Path.GetFileName(file).Contains("ems_image_"))
                        {
                            if (ReadHelper_ReadScreenShotFile(file))
                            {
                                // The file matched an image and was read
                                ExpectedOutputNotConsumedList.Remove(Path.GetFileName(file));
                                File.Delete(file);
                            }
                        }
                    }

                    // Waits a little bit
                    Thread.Sleep(50);

                    // If the result file exists in the list gotten at the beginning (ensures that we will grab all results).
                    if (ExpectedOutputNotConsumedList.Count == 0) break;
                }
                #endregion
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Failed to read results. {e.Message}", e);
            }
        }
        public override void GeneratePointModel(FeModel inModel, string inSaveFolder, string inFileName)
        {
            _errorLogLines.Clear();
            Sb.Clear();
            _model = null;
            ExpectedOutputNotConsumedList.Clear();
            _model = inModel;
            CreateInputFile();

            // Creates the Input File
            CreateInputFile();

            // Saves the file
            try
            {
                // Writes the file
                File.WriteAllText(Path.Combine(inSaveFolder, $"{inFileName}.dat"), Sb.ToString());
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Could not write the input file to the disk. {e.Message}");
            }

            void lf_LaunchAnsys()
            {
                // Starts the APDL program loading the input file
                _commandLineProcess = new Process
                    {
                    StartInfo =
                        {
                        FileName = _ansysExeLocation,
                        Arguments = $"-s noread -g -j {_jobName} -dir \"{inSaveFolder}\"",
                        WorkingDirectory = inSaveFolder,
                        }
                    };
                _commandLineProcess.Start();
            }
            Task t = new Task(lf_LaunchAnsys);
            t.Start();
        }

        #region Ansys Functions to Write the Input File
        private void WriteHelper_PerfectAnalysis()
        {
            if (_model.Frames.Count == 0) throw new Exception("The model must have defined frames!");

            File_StartSection("Setting-up Basic Configuration");
            Sb.AppendLine($"/TITLE,'{_model.ModelName}'");
            File_AppendCommandWithComment("/PREP7", "Preprocessing Environment");
            File_EndSection();

            File_StartSection("Element Type");
            Sb.AppendLine("ET,1,BEAM189");
            Sb.AppendLine("beam_element_type = 1");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,1,0", "Warping DOF 0 Six degrees of freedom per node, unrestrained warping (default)");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,2,0", "XSection Scaling Cross-section is scaled as a function of axial stretch (default); applies only if NLGEOM,ON has been invoked");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,4,1", "Shear Stress Options 1 - Output only flexure-related transverse-shear stresses");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,6,1", "Active only when OUTPR,ESOL is active: Output section forces/moments and strains/curvatures at integration points along the length (default) plus current section area");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,7,2", "Active only when OUTPR,ESOL is active: Output control at section integration point Maximum and minimum stresses/strains plus stresses and strains at each section point");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,9,3", "Active only when OUTPR,ESOL is active: Output control for values extrapolated to the element and section nodes Maximum and minimum stresses/strains plus stresses and strains along the exterior boundary of the cross-section plus stresses and strains at all section nodes");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,11,0", "Set section properties Automatically determine if preintegrated section properties can be used (default)");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,12,0", "Tapered section treatment Linear tapered section analysis");
            File_AppendCommandWithComment("KEYOPT,beam_element_type,15,0", "Results file format: Store averaged results at each section corner node (default)");
            File_EndSection();

            File_StartSection("Material Definitions - Full Stiffness");
            File_AppendCommandWithComment($"*DIM,mat_prop_table,ARRAY,{_model.Materials.Max(a => a.Id)},10", "Defines the target material array");
            Sb.AppendLine();
            foreach (FeMaterial feMaterial in _model.Materials)
            {
                File_AppendCommandWithComment($"matid_{feMaterial.Name}_full={feMaterial.Id}", "Saves a variable for future use");
                File_AppendCommandWithComment($"MAT,{feMaterial.Id}", $"Material Name {feMaterial.Name} # Full Stiffness");
                File_AppendCommandWithComment($"MP,EX,{feMaterial.Id},{feMaterial.YoungModulus}", "Setting Young's Modulus");
                File_AppendCommandWithComment($"MP,PRXY,{feMaterial.Id},{feMaterial.Poisson}", "Setting Poisson's Ratio");
                File_AppendCommandWithComment($"MP,DENS,{feMaterial.Id},{feMaterial.Density}", "Setting Density");
                // Adding the custom values for the UC
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},1) = {feMaterial.Fy}", "Column 1 has the Fy");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},2) = {feMaterial.Fu}", "Column 2 has the Fu");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},3) = {feMaterial.AISC_360_16_phi_compression}", "Column 3 has the AISC_360_16_phi_compression");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},4) = {feMaterial.AISC_360_16_phi_bending}", "Column 4 has the AISC_360_16_phi_bending");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},5) = {feMaterial.AISC_360_16_phi_shear}", "Column 5 has the AISC_360_16_phi_shear");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},6) = {feMaterial.AISC_360_16_phi_torsion}", "Column 6 has the AISC_360_16_phi_torsion");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},7) = {feMaterial.AISC_360_16_phi_tension}", "Column 7 has the AISC_360_16_phi_tension");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},8) = {feMaterial.AISC_360_16_phi_tension_rupturenet}", "Column 8 has the AISC_360_16_phi_tension_rupturenet");
                File_AppendCommandWithComment($"mat_prop_table({feMaterial.Id},9) = {feMaterial.YoungModulus}", "Column 9 has the Youngs Modulus");
                Sb.AppendLine();
            }
            File_EndSection();

            File_StartSection("Sections");
            Sb.AppendLine($"*DIM,sec_prop_table,ARRAY,{_model.Sections.Max(a => a.Id)},10 ! Defines the target array");
            foreach (FeSection feSection in _model.Sections)
            {
                File_AppendCommandWithComment(feSection.AnsysSecTypeLine, $"OptimizationSection {feSection}");
                Sb.AppendLine(feSection.AnsysSecDataLine);
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},1) = {feSection.Area}", "Column 1 has the Area");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},2) = {feSection.PlasticModulus2}", "Column 2 has the Plastic Modulus 2");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},3) = {feSection.PlasticModulus3}", "Column 3 has the Plastic Modulus 3");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},4) = {feSection.LeastGyrationRadius}", "Column 4 has the Least Gyration Radius");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},5) = {feSection.Dimensions.Values.ElementAt(0)}", "Column 5 has the Dimension 1");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},6) = {feSection.Dimensions.Values.ElementAt(1)}", "Column 6 has the Dimension 2");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},7) = {feSection.TorsionalConstant}", "Column 7 has the Torsional Constant");
                File_AppendCommandWithComment($"sec_prop_table({feSection.Id},8) = {feSection.TorsionalShearConstant}", "Column 8 has the Torsional Shear Constant");

                Sb.AppendLine();
            }
            File_EndSection();

            File_StartSection("ORDERED KeyPoints");
            foreach (var feJoint in _model.Joints.OrderBy(a => int.Parse(a.Key)))
            {
                File_AppendCommandWithComment($"K,{feJoint.Value.Id},{feJoint.Value.Point.X},{feJoint.Value.Point.Y},{feJoint.Value.Point.Z}", $"CSharp Joint ID: {feJoint.Key}");
            }
            File_EndSection();

            File_StartSection("ORDERED Lines");
            foreach (var feFrame in _model.Frames.OrderBy(a => int.Parse(a.Key)))
            {
                File_AppendCommandWithComment($"L,{feFrame.Value.IJoint.Id},{feFrame.Value.JJoint.Id}", $"CSharp Line ID: {feFrame.Value.Id}");
            }
            File_EndSection();

            File_StartSection("OptimizationSection and Material Assignments to the *Lines*");
            foreach (FeSection feSection in _model.Sections)
            {
                File_AppendCommandWithComment("LSEL,NONE", $"Selecting the lines of section {feSection.Name}");
                foreach (var feFrame in _model.Frames.Where(a => a.Value.Section == feSection))
                {
                    Sb.AppendLine($"LSEL,A,LINE,,{feFrame.Value.Id}");
                }
                File_AppendCommandWithComment($"LATT,{feSection.Material.Id}, ,beam_element_type, , , ,{feSection.Id}", "Sets the #1 Material; #3 ElementType, #7 OptimizationSection for all selected lines");
                Sb.AppendLine();
            }
            File_EndSection();

            // Ansys Groups are Deprecated

            File_StartSection("Mesh Creation");
            File_AppendCommandWithComment("LSEL,ALL", "Selects all elements to assign mesh parameters");
            File_AppendCommandWithComment($"LESIZE,ALL, , , {AppSS.I.FeOpt.Mesh_ElementsPerFrame}, , 1, , ,1", "Specifies the mesh with the given number of divisions pr frame");
            File_AppendCommandWithComment("LMESH,ALL", "Mesh all selected the lines");
            File_EndSection();

            File_AppendHeader("Boundaries - Loads and Restraints");

            File_AppendCommandWithComment("/SOLU", "Solution Environment");

            File_StartSection("Restraint Assignment");
            List<FeRestraint> distinctRestraints = (from a in _model.Joints select a.Value.Restraint).Distinct().ToList();
            foreach (FeRestraint r in distinctRestraints.Where(a => a.ExistAny))
            {
                File_AppendCommandWithComment("KSEL,NONE", $"Selecting KeyPoints with restraint {r}");
                foreach (KeyValuePair<string, FeJoint> kv in _model.Joints.Where(a => a.Value.Restraint == r))
                {
                    Sb.AppendLine($"KSEL,A,KP,,{kv.Key}");
                }

                if (r.IsFullyFixed) File_AppendCommandWithComment("DK,ALL,ALL,0", "Sets Fully Fixed for previously selected joints");
                else if (r.IsPinned) File_AppendCommandWithComment("DK,ALL,UX,0,,,UY,UZ", "Sets Pinned for previously selected joints");
                else
                {
                    if (r.U1) File_AppendCommandWithComment("DK,ALL,UX,0", "Sets UX for previously selected joints");
                    if (r.U2) File_AppendCommandWithComment("DK,ALL,UY,0", "Sets UY for previously selected joints");
                    if (r.U3) File_AppendCommandWithComment("DK,ALL,UZ,0", "Sets UZ for previously selected joints");
                    if (r.R1) File_AppendCommandWithComment("DK,ALL,ROTX,0", "Sets ROTX for previously selected joints");
                    if (r.R2) File_AppendCommandWithComment("DK,ALL,ROTY,0", "Sets ROTY for previously selected joints");
                    if (r.R3) File_AppendCommandWithComment("DK,ALL,ROTZ,0", "Sets ROTZ for previously selected joints");
                }

                Sb.AppendLine();
            }
            File_EndSection();

            File_StartSection("Frame Releases Assignment");
            if (_model.Groups.Values.Any(a => a.Release != null && a.Release.Released && a.Frames.Count > 0)) throw new FeSolverException("Ansys solver does not support Releases.");
            File_EndSection();

            File_StartSection("Loads");
            foreach (FeLoad feLoadBase in _model.Loads)
            {
                switch (feLoadBase)
                {
                    case FeLoad_Inertial feLoad_Inertial:
                        File_AppendCommandWithComment(feLoad_Inertial.AnsysInertialLoadLine, $"{feLoad_Inertial}");
                        break;

                    case FeLoad_Point feLoad_Point:
                        File_AppendCommandWithComment("KSEL,NONE", $"Selecting joints for Point Load");
                        foreach (FeJoint j in _model.Groups[feLoad_Point.GhGeom.FeGroupNameHelper].Joints)
                        {
                            Sb.AppendLine($"KSEL,A,KP,,{j.Id}");
                        }
                        
                        if (feLoad_Point.Nominal.X != 0d) File_AppendCommandWithComment($"FK,ALL,FX,{feLoad_Point.Nominal.X * feLoad_Point.Multiplier}", $"Adding the Point Load - X Direction");
                        if (feLoad_Point.Nominal.Y != 0d) File_AppendCommandWithComment($"FK,ALL,FY,{feLoad_Point.Nominal.Y * feLoad_Point.Multiplier}", $"Adding the Point Load - Y Direction");
                        if (feLoad_Point.Nominal.Z != 0d) File_AppendCommandWithComment($"FK,ALL,FZ,{feLoad_Point.Nominal.Z * feLoad_Point.Multiplier}", $"Adding the Point Load - Z Direction");
                        
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(feLoadBase));
                }
            }
            File_EndSection();

            File_StartSection("Perfect Shape - Solving");
            File_AppendCommandWithComment("ANTYPE,STATIC,NEW", "Analysis Type - Static");

            if (AppSS.I.FeOpt.LargeDeflections_IsSet) File_AppendCommandWithComment("NLGEOM,ON", "Large Deflections = ENABLED");
            else File_AppendCommandWithComment("NLGEOM,OFF", "Large Deflections = DISABLED");

            File_AppendCommandWithComment("PSTRES,1", "Stores the pre-stress values because they will be used for the future eigenvalue buckling analysis.");
            File_AppendCommandWithComment("OUTPR,ALL", "Prints all solution");
            File_AppendCommandWithComment("ALLSEL,ALL", "Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.");
            File_AppendCommandWithComment("AUTOTS,ON", "Sets Automatic Time Stepping.");
            File_AppendCommandWithComment("LNSRCH,AUTO", "Sets the usage of Line Search to Auto.");
            File_AppendCommandWithComment("PRED,AUTO", "Turns Automatic DOF Prediction to Auto.");
            File_AppendCommandWithComment("NSUBST,20,1000,20", "Sets the number of time steps for this analysis.");
            File_AppendCommandWithComment("KBC,0", "Sets the load application to be ramped.");
            File_AppendCommandWithComment("TIME,1", "Sets the time at the end of this load step.");
            File_AppendCommandWithComment("SOLVE", "Solves the problem");
            File_EndSection();

            File_AppendHeader("Perfect Shape - Basic Mesh Results");

            File_StartSection("Post-Process - Write Mesh Information File Basic Configuration");
            File_AppendCommandWithComment("/POST1", "Post-Processing Environment");
            File_AppendCommandWithComment("SET,LAST", "Points to the Last Results in the Set");
            File_AppendCommandWithComment("ALLSEL,ALL", "Selects *everything* for output");
            File_AppendCommandWithComment("/HEADER,OFF,OFF,OFF,OFF,ON,OFF", "Fixing the formats for the output files");
            File_AppendCommandWithComment("/PAGE,,,-100000000,240,0", "Fixing the formats for the output files");
            File_AppendCommandWithComment("/FORMAT,10,G,20,6,,,", "Fixing the formats for the output files");
            File_EndSection();

            File_StartSection("Post-Process - Write Mesh Information: Mesh Nodal Locations - File <ems_output_meshinfo_nodal_locations.txt>");
            Sb.AppendLine(@"
/OUTPUT,'ems_output_meshinfo_nodal_locations','txt'
NLIST
/OUTPUT
");
            File_EndSection();
            ExpectedOutputNotConsumedList.Add("ems_output_meshinfo_nodal_locations.txt");

            File_StartSection("Post-Process - Write Mesh Information: BEAM Elements of each Line - File <ems_output_meshinfo_elements_of_lines.txt>");
            Sb.AppendLine(@"
! GETS LIST OF LINES WITH ELEMENTS AND NODES I,J,K

! LSEL,ALL ! Selects all lines
*GET,total_line_count,LINE,0,COUNT ! Gets count of all lines

! ESEL,ALL ! Selects all elements
*GET,total_element_count,ELEM,0,COUNT ! Gets count of all elements

*DIM,line_element_match,ARRAY,total_element_count,15 ! Defines the target array

! Makes a loop on the lines
currentline = 0 !  Declares a start number for the lines
elemindex = 1 ! The element index in the array

*DO,i,1,total_line_count ! loops on all lines
	
	LSEL,ALL ! Reselects all lines
	*GET,nextline,LINE,currentline,NXTH ! Gets the number of the next line in the selected set
	
	LSEL,S,LINE,,nextline,,,1 ! Selects the next line and its associated elements
	*GET,currentLineMat,LINE,nextline,ATTR,MAT ! Gets the number of the material of this Line
    *GET,currentLineLength,LINE,nextline,LENG ! Gets the length of this Line

	currentelement = 0 ! Declares a start number for the current element
	*GET,lecount,ELEM,0,COUNT ! Gets the number of selected elements
	
	*DO,j,1,lecount ! loops on the selected elements in this line
		*GET,nextelement,ELEM,currentelement,NXTH ! Gets the number of the next element in the selected set
		
		! Getting the nodes of each element
		*GET,e_i,ELEM,nextelement,NODE,1
		*GET,e_j,ELEM,nextelement,NODE,2
		*GET,e_k,ELEM,nextelement,NODE,3

        ! Getting the section of the element
        *GET,elemSection,ELEM,nextelement,ATTR,SECN
		
		! Stores into the array
		line_element_match(elemindex,1) = nextline
		line_element_match(elemindex,2) = nextelement
		line_element_match(elemindex,3) = e_i
		line_element_match(elemindex,4) = e_j
		line_element_match(elemindex,5) = e_k
        line_element_match(elemindex,6) = currentLineMat
        line_element_match(elemindex,7) = elemSection
        line_element_match(elemindex,8) = currentLineLength
		
		currentelement = nextelement ! updates for the next iteration
		elemindex = elemindex + 1 ! Increments the element index counter
	*ENDDO
	
	currentline = nextline ! updates for the next iteration
*ENDDO

! Writes the data to file
*CFOPEN,'ems_output_meshinfo_elements_of_lines','txt' ! Opens the file

! Writes the header
*VWRITE,'LINE', ',' ,'ELEM', ',' , 'INODE' , ',' , 'JNODE', ',' , 'KNODE'
(A4,A1,A4,A1,A5,A1,A5,A1,A5)

! Writes the data
*VWRITE,line_element_match(1,1), ',' ,line_element_match(1,2), ',' , line_element_match(1,3) , ',' , line_element_match(1,4), ',' , line_element_match(1,5)
%I%C%I%C%I%C%I%C%I

*CFCLOSE");
            File_EndSection();
            ExpectedOutputNotConsumedList.Add("ems_output_meshinfo_elements_of_lines.txt");
        }
        private void WriteHelper_EigenvalueBucklingAnalysis()
        {
            File_StartSection("Setting and running the Eigenvalue Buckling Analysis");

            // Eigenvalue Buckling analysis cannot start from a static analysis where the NLGEOM was turned on
            if (AppSS.I.FeOpt.LargeDeflections_IsSet)
            {
                File_AppendCommentLine("NLGEOM was turned on - turns it off and re-run to create the basis for the eigenvalue buckling");
                File_AppendCommandWithComment("/SOLU", "Solution Environment");
                File_AppendCommandWithComment("ANTYPE,STATIC,NEW", "Analysis Type - Static");
                File_AppendCommandWithComment("NLGEOM,OFF", "Large Deflections = DISABLED");
                File_AppendCommandWithComment("PSTRES,1", "Stores the pre-stress values because they will be used for the future eigenvalue buckling analysis.");
                File_AppendCommandWithComment("OUTPR,ALL", "Prints all solution");
                File_AppendCommandWithComment("ALLSEL,ALL", "Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.");
                File_AppendCommandWithComment("AUTOTS,ON", "Sets Automatic Time Stepping.");
                File_AppendCommandWithComment("LNSRCH,AUTO", "Sets the usage of Line Search to Auto.");
                File_AppendCommandWithComment("PRED,AUTO", "Turns Automatic DOF Prediction to Auto.");
                File_AppendCommandWithComment("NSUBST,20,1000,20", "Sets the number of time steps for this analysis.");
                File_AppendCommandWithComment("KBC,0", "Sets the load application to be ramped.");
                File_AppendCommandWithComment("TIME,1", "Sets the time at the end of this load step.");
                File_AppendCommandWithComment("SOLVE", "Solves the problem - without NLGEOM to be used in the Eigenvalue Buckling analysis");

            }

            File_AppendCommandWithComment("/POST1", "Post-Processing Environment");
            File_AppendCommandWithComment("SET,LAST", "Makes the last solution set to be used as buckling input");
            File_AppendCommandWithComment("/SOLU", "Goes back to the Solution Environment");
            File_AppendCommandWithComment("ANTYPE,BUCKLE,NEW", "Sets the analysis type as Eigenvalue Buckling");
            File_AppendCommandWithComment($"BUCOPT,LANB,{AppSS.I.FeOpt.EigenvalueBuckling_ShapesToCapture},,,RANGE", $"Sets the options and to capture the first {AppSS.I.FeOpt.EigenvalueBuckling_ShapesToCapture} shapes");
            File_AppendCommandWithComment("MXPAND, ALL, , , 1,,,,", "Tells that we want to know the displacements at the nodes. Energy outputs may also be given, if required. TODO");
            File_AppendCommandWithComment("OUTPR,ALL", "Prints all solution");
            File_AppendCommandWithComment("ALLSEL,ALL", "Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.");
            File_AppendCommandWithComment("AUTOTS,ON", "Sets Automatic Time Stepping.");
            File_AppendCommandWithComment("LNSRCH,AUTO", "Sets the usage of Line Search to Auto.");
            File_AppendCommandWithComment("PRED,AUTO", "Turns Automatic DOF Prediction to Auto.");
            File_AppendCommandWithComment("NSUBST,20,1000,20", "Sets the number of time steps for this analysis.");
            File_AppendCommandWithComment("KBC,0", "Sets the load application to be ramped.");
            File_AppendCommandWithComment("TIME,1", "Sets the time at the end of this load step.");
            File_AppendCommandWithComment("SOLVE", "Solves the problem");

            File_AppendCommandWithComment("/POST1", "Post-Processing Environment");
            File_EndSection();
        }
        private void WriteHelper_ImperfectAnalysis_UpdateGeometryUsingEigenvalueBucklingShape()
        {
            // Decides on the factor to be used
            double eigenModeScale = AppSS.I.FeOpt.Imperfect_MultiplierFromBoundingBox ? (_model.Joints_BoundingBox_MaxLength / 1000d) : AppSS.I.FeOpt.Imperfect_Multiplier;

            File_StartSection("Add the deformations from the Eigenvalue Buckling");
            File_AppendCommandWithComment("/PREP7", "Preprocessing Environment");
            File_AppendCommandWithComment($"UPGEOM,{eigenModeScale},1,{AppSS.I.FeOpt.Imperfect_EigenvalueBucklingMode},{_jobName},rst", $"Adds displacements from a previous analysis and updates the geometry of the finite element model to the deformed configuration.");
            File_EndSection();
        }
        private void WriteHelper_ImperfectAnalysis_Solve()
        {
            File_StartSection("Imperfect Shape - Optimize");

            File_AppendCommandWithComment("/SOLU", "Solution Environment");
            File_AppendCommandWithComment("ANTYPE,STATIC,NEW", "Analysis Type - Static");

            if (AppSS.I.FeOpt.LargeDeflections_IsSet) File_AppendCommandWithComment("NLGEOM,ON", "Large Deflections = ENABLED");
            else File_AppendCommandWithComment("NLGEOM,OFF", "Large Deflections = DISABLED");

            File_AppendCommandWithComment("PSTRES,1", "Stores the pre-stress values because they will be used for the future eigenvalue buckling analysis.");
            File_AppendCommandWithComment("OUTPR,ALL", "Prints all solution");
            File_AppendCommandWithComment("ALLSEL,ALL", "Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.");
            File_AppendCommandWithComment("AUTOTS,ON", "Sets Automatic Time Stepping.");
            File_AppendCommandWithComment("LNSRCH,AUTO", "Sets the usage of Line Search to Auto.");
            File_AppendCommandWithComment("PRED,AUTO", "Turns Automatic DOF Prediction to Auto.");
            File_AppendCommandWithComment("NSUBST,20,1000,20", "Sets the number of time steps for this analysis.");
            File_AppendCommandWithComment("KBC,0", "Sets the load application to be ramped.");
            File_AppendCommandWithComment("TIME,1", "Sets the time at the end of this load step.");
            File_AppendCommandWithComment("SOLVE", "Solves the problem");
            File_EndSection();

            File_StartSection("Post-Process - Write Mesh Information File Basic Configuration");
            File_AppendCommandWithComment("/POST1", "Post-Processing Environment");
            File_AppendCommandWithComment("SET,LAST", "Points to the Last Results in the Set");
            File_AppendCommandWithComment("ALLSEL,ALL", "Selects *everything* for output");
            File_AppendCommandWithComment("/HEADER,OFF,OFF,OFF,OFF,ON,OFF", "Fixing the formats for the output files");
            File_AppendCommandWithComment("/PAGE,,,-100000000,240,0", "Fixing the formats for the output files");
            File_AppendCommandWithComment("/FORMAT,10,G,20,6,,,", "Fixing the formats for the output files");
            File_EndSection();
        }
        private void WriteHelper_ImperfectAnalysis_Soften()
        {
            File_StartSection("Softens the Materials");
            File_AppendCommandWithComment("/PREP7", "Preprocessing Environment");
            foreach (FeMaterial feMaterial in _model.Materials)
            {
                File_AppendCommandWithComment($"MP,EX,{feMaterial.Id},{feMaterial.YoungModulus_Soft}", "Setting Young's Modulus");
                Sb.AppendLine();
            }

            File_EndSection();
        }

        private readonly HashSet<FeResultTypeEnum> _writeResultTouchedTypes = new HashSet<FeResultTypeEnum>();
        private void WriteHelper_WriteResults(FeResultClassification inRes)
        {
            // It has already been treated, aborts
            if (_writeResultTouchedTypes.Contains(inRes.ResultType)) return;

            string outFileName = inRes.ResultFileName;
            string tempFileName = $"result_temp";

            File_StartSection($"Post-Process - Results {inRes.ResultFamily} - File <{outFileName}.txt>");
            File_AppendCommandWithComment("ALLSEL,ALL", "Selects Everything");
            switch (inRes.ResultFamily)
            {
                case FeResultFamilyEnum.Nodal_Reaction:
                    Sb.AppendLine($@"/OUTPUT,'{tempFileName}','txt'
PRRSOL
/OUTPUT");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                    // Adds to the treated hashset
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fx);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fy);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Mx);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_My);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Mz);
                    break;

                case FeResultFamilyEnum.Nodal_Displacement:
                    Sb.AppendLine($@"
*GET,total_node_count,NODE,0,COUNT ! Gets count of all elements
*DIM,n_dof,ARRAY,total_node_count,6
*VGET,n_dof(1,1),NODE,1,U,X  ! Displacement X
*VGET,n_dof(1,2),NODE,1,U,Y  ! Displacement Y
*VGET,n_dof(1,3),NODE,1,U,Z  ! Displacement Z
*VGET,n_dof(1,4),NODE,1,ROT,X  ! Rotation X
*VGET,n_dof(1,5),NODE,1,ROT,Y  ! Rotation Y
*VGET,n_dof(1,6),NODE,1,ROT,Z  ! Rotation Z

! Writes the data to file
*CFOPEN,'{tempFileName}','txt' ! Opens the file

! Writes the header
*VWRITE,'NODE', ',' ,'UX', ',' , 'UY' , ',' , 'UZ', ',' , 'RX', ',' , 'RY', ',' , 'RZ'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,n_dof(1,1), ',' , n_dof(1,2) , ',' , n_dof(1,3), ',' , n_dof(1,4), ',' , n_dof(1,5), ',' , n_dof(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE");
                    
                    File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Ux);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Uy);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Uz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Rx);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Ry);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Rz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_UTotal);
                    break;

                case FeResultFamilyEnum.SectionNode_Stress:
                    Sb.AppendLine($@"
/OUTPUT,'{tempFileName}','txt'
PRESOL,S,PRIN
/OUTPUT");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S1);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S2);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S3);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_SInt);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_SEqv);
                    break;

                case FeResultFamilyEnum.SectionNode_Strain:
                    Sb.AppendLine($@"
/OUTPUT,'{tempFileName}','txt'
PRESOL,EPTT,PRIN
/OUTPUT");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Strain_EPTT1);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Strain_EPTT2);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Strain_EPTT3);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Strain_EPTTInt);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Strain_EPTTEqv);
                    break;

                case FeResultFamilyEnum.ElementNodal_BendingStrain:
                    Sb.AppendLine($@"
ETABLE, iEPELDIR, SMISC, 41 ! Axial strain at the end
ETABLE, iEPELByT, SMISC, 42 ! Bending strain on the element +Y side of the beam.
ETABLE, iEPELByB, SMISC, 43 ! Bending strain on the element -Y side of the beam.
ETABLE, iEPELBzT, SMISC, 44 ! Bending strain on the element +Z side of the beam.
ETABLE, iEPELBzB, SMISC, 45 ! Bending strain on the element -Z side of the beam.

! WRITE: I NODE BASIC DIRECTIONAL STRAIN
*DIM,ibasic_dirstrain,ARRAY,total_element_count,5
*VGET,ibasic_dirstrain(1,1),ELEM,1,ETAB,iEPELDIR
*VGET,ibasic_dirstrain(1,2),ELEM,1,ETAB,iEPELByT
*VGET,ibasic_dirstrain(1,3),ELEM,1,ETAB,iEPELByB
*VGET,ibasic_dirstrain(1,4),ELEM,1,ETAB,iEPELBzT
*VGET,ibasic_dirstrain(1,5),ELEM,1,ETAB,iEPELBzB

! Writes the data to file
*CFOPEN,'{tempFileName}_inode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iEPELDIR', ',' , 'iEPELByT' , ',' , 'iEPELByB', ',' , 'iEPELBzT', ',' , 'iEPELBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstrain(1,1), ',' , ibasic_dirstrain(1,2) , ',' , ibasic_dirstrain(1,3), ',' , ibasic_dirstrain(1,4), ',' , ibasic_dirstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE

ETABLE, jEPELDIR, SMISC, 46 ! Axial strain at the end
ETABLE, jEPELByT, SMISC, 47 ! Bending strain on the element +Y side of the beam.
ETABLE, jEPELByB, SMISC, 48 ! Bending strain on the element -Y side of the beam.
ETABLE, jEPELBzT, SMISC, 49 ! Bending strain on the element +Z side of the beam.
ETABLE, jEPELBzB, SMISC, 50 ! Bending strain on the element -Z side of the beam.

! WRITE: J NODE BASIC DIRECTIONAL STRAIN
*DIM,jbasic_dirstrain,ARRAY,total_element_count,5
*VGET,jbasic_dirstrain(1,1),ELEM,1,ETAB,jEPELDIR
*VGET,jbasic_dirstrain(1,2),ELEM,1,ETAB,jEPELByT
*VGET,jbasic_dirstrain(1,3),ELEM,1,ETAB,jEPELByB
*VGET,jbasic_dirstrain(1,4),ELEM,1,ETAB,jEPELBzT
*VGET,jbasic_dirstrain(1,5),ELEM,1,ETAB,jEPELBzB

! Writes the data to file
*CFOPEN,'{tempFileName}_jnode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jEPELDIR', ',' , 'jEPELByT' , ',' , 'jEPELByB', ',' , 'jEPELBzT', ',' , 'jEPELBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasic_dirstrain(1,1), ',' , jbasic_dirstrain(1,2) , ',' , jbasic_dirstrain(1,3), ',' , jbasic_dirstrain(1,4), ',' , jbasic_dirstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE
");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_inode,txt,,{outFileName}_inode,txt", "Renames the result file");
                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_jnode,txt,,{outFileName}_jnode,txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_inode.txt");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_jnode.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB);
                    break;

                case FeResultFamilyEnum.ElementNodal_Force:
                    Sb.AppendLine($@"
ETABLE, iFx, SMISC, 1
ETABLE, iMy, SMISC, 2
ETABLE, iMz, SMISC, 3
ETABLE, iTq, SMISC, 4  ! Torsional Moment
ETABLE, iSFz, SMISC, 5  ! Shear Force Z
ETABLE, iSFy, SMISC, 6  ! Shear Force Z

! WRITE: I NODE BASIC FORCE DATA
*DIM,ibasicf,ARRAY,total_element_count,6
*VGET,ibasicf(1,1),ELEM,1,ETAB,iFx
*VGET,ibasicf(1,2),ELEM,1,ETAB,iMy
*VGET,ibasicf(1,3),ELEM,1,ETAB,iMz
*VGET,ibasicf(1,4),ELEM,1,ETAB,iTq
*VGET,ibasicf(1,5),ELEM,1,ETAB,iSFz
*VGET,ibasicf(1,6),ELEM,1,ETAB,iSFy

! Writes the data to file
*CFOPEN,'{tempFileName}_inode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iFx', ',' , 'iMy' , ',' , 'iMz', ',' , 'iTq', ',' , 'iSFz', ',' , 'iSFy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasicf(1,1), ',' , ibasicf(1,2) , ',' , ibasicf(1,3), ',' , ibasicf(1,4), ',' , ibasicf(1,5), ',' , ibasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE

ETABLE, jFx, SMISC, 14
ETABLE, jMy, SMISC, 15
ETABLE, jMz, SMISC, 16
ETABLE, jTq, SMISC, 17 ! Torsional Moment
ETABLE, jSFz, SMISC, 18  ! Shear Force Z
ETABLE, jSFy, SMISC, 19  ! Shear Force Z

! WRITE: J NODE BASIC FORCE DATA
*DIM,jbasicf,ARRAY,total_element_count,6
*VGET,jbasicf(1,1),ELEM,1,ETAB,jFx
*VGET,jbasicf(1,2),ELEM,1,ETAB,jMy
*VGET,jbasicf(1,3),ELEM,1,ETAB,jMz
*VGET,jbasicf(1,4),ELEM,1,ETAB,jTq
*VGET,jbasicf(1,5),ELEM,1,ETAB,jSFz
*VGET,jbasicf(1,6),ELEM,1,ETAB,jSFy

! Writes the data to file
*CFOPEN,'{tempFileName}_jnode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jFx', ',' , 'jMy' , ',' , 'jMz', ',' , 'jTq', ',' , 'jSFz', ',' , 'jSFy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasicf(1,1), ',' , jbasicf(1,2) , ',' , jbasicf(1,3), ',' , jbasicf(1,4), ',' , jbasicf(1,5), ',' , jbasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE
");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_inode,txt,,{outFileName}_inode,txt", "Renames the result file");
                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_jnode,txt,,{outFileName}_jnode,txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_inode.txt");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_jnode.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Fx);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_SFy);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_SFz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Tq);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_My);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Mz);
                    break;

                case FeResultFamilyEnum.ElementNodal_Strain:
                    Sb.AppendLine($@"
ETABLE, iEx, SMISC, 7  ! Axial Strain
ETABLE, iKy, SMISC, 8  ! Curvature Y
ETABLE, iKz, SMISC, 9  ! Curvature Z
ETABLE, iTe, SMISC, 10 ! Torsional Strain
ETABLE, iSEz, SMISC, 11  ! Strain Z
ETABLE, iSEy, SMISC, 12  ! Strain Y


! WRITE: I NODE BASIC STRAIN DATA
*DIM,ibasics,ARRAY,total_element_count,6
*VGET,ibasics(1,1),ELEM,1,ETAB,iEx
*VGET,ibasics(1,2),ELEM,1,ETAB,iKy
*VGET,ibasics(1,3),ELEM,1,ETAB,iKz
*VGET,ibasics(1,4),ELEM,1,ETAB,iSEz
*VGET,ibasics(1,5),ELEM,1,ETAB,iSEy
*VGET,ibasics(1,6),ELEM,1,ETAB,iTe

! Writes the data to file
*CFOPEN,'{tempFileName}_inode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iEx', ',' , 'iKy' , ',' , 'iKz', ',' , 'iSEz', ',' , 'iSEy', ',' , 'iTe'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasics(1,1), ',' , ibasics(1,2) , ',' , ibasics(1,3), ',' , ibasics(1,4), ',' , ibasics(1,5), ',' , ibasics(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE

ETABLE, jEx, SMISC, 20  ! Axial Strain
ETABLE, jKy, SMISC, 21  ! Curvature Y
ETABLE, jKz, SMISC, 22  ! Curvature Z
ETABLE, jTe, SMISC, 23 ! Torsional Strain
ETABLE, jSEz, SMISC, 24  ! Strain Z
ETABLE, jSEy, SMISC, 25  ! Strain Y

! WRITE: J NODE BASIC STRAIN DATA
*DIM,jbasics,ARRAY,total_element_count,6
*VGET,jbasics(1,1),ELEM,1,ETAB,jEx
*VGET,jbasics(1,2),ELEM,1,ETAB,jKy
*VGET,jbasics(1,3),ELEM,1,ETAB,jKz
*VGET,jbasics(1,4),ELEM,1,ETAB,jSEz
*VGET,jbasics(1,5),ELEM,1,ETAB,jSEy
*VGET,jbasics(1,6),ELEM,1,ETAB,jTe

! Writes the data to file
*CFOPEN,'{tempFileName}_jnode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jEx', ',' , 'jKy' , ',' , 'jKz', ',' , 'jSEz', ',' , 'jSEy', ',' , 'jTe'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasics(1,1), ',' , jbasics(1,2) , ',' , jbasics(1,3), ',' , jbasics(1,4), ',' , jbasics(1,5), ',' , jbasics(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE
");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_inode,txt,,{outFileName}_inode,txt", "Renames the result file");
                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_jnode,txt,,{outFileName}_jnode,txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_inode.txt");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_jnode.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Strain_Ex);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Strain_Ky);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Strain_Kz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Strain_SEz);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Strain_SEy);
                    break;

                case FeResultFamilyEnum.ElementNodal_Stress:
                    Sb.AppendLine($@"
ETABLE, iSDIR, SMISC, 31 ! Axial Direct Stress
ETABLE, iSByT, SMISC, 32 ! Bending stress on the element +Y side of the beam
ETABLE, iSByB, SMISC, 33 ! Bending stress on the element -Y side of the beam
ETABLE, iSBzT, SMISC, 34 ! Bending stress on the element +Z side of the beam
ETABLE, iSBzB, SMISC, 35 ! Bending stress on the element -Z side of the beam

! WRITE: I NODE BASIC DIRECTIONAL STRESS
*DIM,ibasic_dirstress,ARRAY,total_element_count,5
*VGET,ibasic_dirstress(1,1),ELEM,1,ETAB,iSDIR
*VGET,ibasic_dirstress(1,2),ELEM,1,ETAB,iSByT
*VGET,ibasic_dirstress(1,3),ELEM,1,ETAB,iSByB
*VGET,ibasic_dirstress(1,4),ELEM,1,ETAB,iSBzT
*VGET,ibasic_dirstress(1,5),ELEM,1,ETAB,iSBzB

! Writes the data to file
*CFOPEN,'{tempFileName}_inode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iSDIR', ',' , 'iSByT' , ',' , 'iSByB', ',' , 'iSBzT', ',' , 'iSBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstress(1,1), ',' , ibasic_dirstress(1,2) , ',' , ibasic_dirstress(1,3), ',' , ibasic_dirstress(1,4), ',' , ibasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE

ETABLE, jSDIR, SMISC, 36 ! Axial Direct Stress
ETABLE, jSByT, SMISC, 37 ! Bending stress on the element +Y side of the beam
ETABLE, jSByB, SMISC, 38 ! Bending stress on the element -Y side of the beam
ETABLE, jSBzT, SMISC, 39 ! Bending stress on the element +Z side of the beam
ETABLE, jSBzB, SMISC, 40 ! Bending stress on the element -Z side of the beam

! WRITE: J NODE BASIC DIRECTIONAL STRESS
*DIM,jbasic_dirstress,ARRAY,total_element_count,5
*VGET,jbasic_dirstress(1,1),ELEM,1,ETAB,jSDIR
*VGET,jbasic_dirstress(1,2),ELEM,1,ETAB,jSByT
*VGET,jbasic_dirstress(1,3),ELEM,1,ETAB,jSByB
*VGET,jbasic_dirstress(1,4),ELEM,1,ETAB,jSBzT
*VGET,jbasic_dirstress(1,5),ELEM,1,ETAB,jSBzB

! Writes the data to file
*CFOPEN,'{tempFileName}_jnode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jSDIR', ',' , 'jSByT' , ',' , 'jSByB', ',' , 'jSBzT', ',' , 'jSBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasic_dirstress(1,1), ',' , jbasic_dirstress(1,2) , ',' , jbasic_dirstress(1,3), ',' , jbasic_dirstress(1,4), ',' , jbasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE
");

                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_inode,txt,,{outFileName}_inode,txt", "Renames the result file");
                    File_AppendCommandWithComment($"/RENAME,{tempFileName}_jnode,txt,,{outFileName}_jnode,txt", "Renames the result file");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_inode.txt");
                    ExpectedOutputNotConsumedList.Add($"{outFileName}_jnode.txt");

                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Stress_SDir);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Stress_SByT);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Stress_SByB);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Stress_SBzT);
                    _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Stress_SBzB);
                    break;

                case FeResultFamilyEnum.Others:
                    switch (inRes.ResultType)
                    {
                        case FeResultTypeEnum.ElementNodal_CodeCheck:
                            Sb.AppendLine($@"
! Reminder: There is a line_element_match array that was created for the <ems_output_meshinfo_elements_of_lines.txt>

ETABLE, iFx, SMISC, 1
ETABLE, iMy, SMISC, 2
ETABLE, iMz, SMISC, 3
ETABLE, iTq, SMISC, 4  ! Torsional Moment
ETABLE, iSFz, SMISC, 5  ! Shear Force Z
ETABLE, iSFy, SMISC, 6  ! Shear Force Z

ETABLE, jFx, SMISC, 14
ETABLE, jMy, SMISC, 15
ETABLE, jMz, SMISC, 16
ETABLE, jTq, SMISC, 17 ! Torsional Moment
ETABLE, jSFz, SMISC, 18  ! Shear Force Z
ETABLE, jSFy, SMISC, 19  ! Shear Force Z

! WRITE: I NODE BASIC FORCE DATA
*DIM,uctable,ARRAY,total_element_count,75

*VGET,uctable(1,1),ELEM,1,ETAB,iFx
*VGET,uctable(1,2),ELEM,1,ETAB,iMy
*VGET,uctable(1,3),ELEM,1,ETAB,iMz
*VGET,uctable(1,4),ELEM,1,ETAB,iTq
*VGET,uctable(1,5),ELEM,1,ETAB,iSFz
*VGET,uctable(1,6),ELEM,1,ETAB,iSFy

*VGET,uctable(1,7),ELEM,1,ETAB,jFx
*VGET,uctable(1,8),ELEM,1,ETAB,jMy
*VGET,uctable(1,9),ELEM,1,ETAB,jMz
*VGET,uctable(1,10),ELEM,1,ETAB,jTq
*VGET,uctable(1,11),ELEM,1,ETAB,jSFz
*VGET,uctable(1,12),ELEM,1,ETAB,jSFy

! *MOPER,sort_vec,line_element_match,SORT,,2 ! Sorts the line_element_table based on col 2 

! line_element_match(1,1) => (element's line)
! line_element_match(1,2) => (element number)
! line_element_match(1,6) => (element material)
! line_element_match(1,7) => (element section)
! line_element_match(1,8) => (element's line length)

*GET,uctable_lines,PARM,uctable,DIM,1
*DO,i,1,uctable_lines ! loops on all elements at the result array
    ! i => current element
    uctable(i,13) = i ! Saves the element number
    
    ! This is all to search the for the current element in the line_element_match table so that we can find the material and the section of the current element
    *VFILL,line_element_match(1,10),RAMP,i,0                                             ! Fills col 10 with the current element number (all the same, no ramp - I hate APDL)
    *VOPER,line_element_match(1,11),line_element_match(1,10),EQ,line_element_match(1,2)  ! Matches them for first search
    *VSCFUN,elemLine,FIRST,line_element_match(1,11)                                      ! The line at the line_element_match
    
    c_elementLine = line_element_match(elemLine,1) ! Grabs the line of the current element
    c_elementLineLength = line_element_match(elemLine,8) ! Grabs the length of the line of the current element
    c_matLine = line_element_match(elemLine,6) ! Grabs the material ID [Matches the line number in the mat_prop_table]
    c_secLine = line_element_match(elemLine,7) ! Grabs the section ID [Matches the line number in the sec_prop_table]

    c_matFy = mat_prop_table(c_matLine,1)
    c_matFu = mat_prop_table(c_matLine,2)
    c_matPhiCompression = mat_prop_table(c_matLine,3)
    c_matPhiBending = mat_prop_table(c_matLine,4)
    c_matPhiShear = mat_prop_table(c_matLine,5)
    c_matPhiTorsion = mat_prop_table(c_matLine,6)
    c_matPhiTension = mat_prop_table(c_matLine,7)
    c_matPhiTensionRuptureNet = mat_prop_table(c_matLine,8)
    c_matYoungModulus = mat_prop_table(c_matLine,9)

    c_secArea = sec_prop_table(c_secLine,1)
    c_secPlMod2 = sec_prop_table(c_secLine,2)
    c_secPlMod3 = sec_prop_table(c_secLine,3)
    c_secLeastGyrRadius = sec_prop_table(c_secLine,4)
    c_secDim1 = sec_prop_table(c_secLine,5)
    c_secDim2 = sec_prop_table(c_secLine,6)
    c_secTorsionalShearConstant = sec_prop_table(c_secLine,8)

    ! Fills the array for further processing
    uctable(i,14) = c_elementLine
    uctable(i,15) = c_elementLineLength

    uctable(i,16) = c_matFy
    uctable(i,17) = c_matFu
    uctable(i,18) = c_matPhiCompression
    uctable(i,19) = c_matPhiBending
    uctable(i,20) = c_matPhiShear
    uctable(i,21) = c_matPhiTorsion
    uctable(i,22) = c_matPhiTension
    uctable(i,23) = c_matPhiTensionRuptureNet
    uctable(i,24) = c_matYoungModulus

    uctable(i,27) = c_secArea
    uctable(i,28) = c_secPlMod2
    uctable(i,29) = c_secPlMod3
    uctable(i,30) = c_secDim1
    uctable(i,31) = c_secDim2
    uctable(i,31) = c_secTorsionalShearConstant
	uctable(i,32) = c_secLeastGyrRadius

    ! Checking Slenderness
    !------------------------------------------
    uctable(i,35) = c_elementLineLength/c_secLeastGyrRadius     ! Line Slenderness

    i_slendernessLimit = 0
    *IF,uctable(i,1),GT,0,THEN ! TENSION
        i_slendernessLimit = 300
    *ELSE ! COMPRESSION
        i_slendernessLimit = 200
    *ENDIF
    j_slendernessLimit = 0
    *IF,uctable(i,7),GT,0,THEN ! TENSION
        j_slendernessLimit = 300
    *ELSE ! COMPRESSION
        j_slendernessLimit = 200
    *ENDIF

    uctable(i,36) = i_slendernessLimit
    uctable(i,37) = uctable(i,35)/uctable(i,36)     ! Line Slenderness I Node Factor
    uctable(i,38) = i_slendernessLimit
    uctable(i,39) = uctable(i,35)/uctable(i,38)     ! Line Slenderness J Node Factor


    ! Checking Tension
    !------------------------------------------
    pn_tension_D2_1 = c_matFy*c_secArea
    pn_tension_D2_2 = c_matFu*c_secArea

    pn_phi_tension = 0
    *IF,pn_tension_D2_1,LT,pn_tension_D2_2,THEN
        pn_phi_tension = pn_tension_D2_1*c_matPhiTension
    *ELSE
        pn_phi_tension = pn_tension_D2_2*c_matPhiTensionRuptureNet
    *ENDIF

    uctable(i,40) = pn_phi_tension

    *IF,uctable(i,1),GT,0,THEN ! TENSION
        uctable(i,41) = ABS(uctable(i,1))/pn_phi_tension
    *ELSE ! COMPRESSION
        uctable(i,41) = 0
    *ENDIF

    *IF,uctable(i,7),GT,0,THEN ! TENSION
        uctable(i,42) = ABS(uctable(i,7))/pn_phi_tension
    *ELSE ! COMPRESSION
        uctable(i,42) = 0
    *ENDIF


    ! Checking Compression
    !------------------------------------------
    elastic_buckling_stress = (((3.1415927**2)*c_matYoungModulus)/(uctable(i,35)**2))
    comp_slend_threshold = c_matFy/elastic_buckling_stress
    
    comp_critical_stress = 0
    *IF,comp_slend_threshold,LE,2.25,THEN
        comp_critical_stress = (0.658**comp_slend_threshold)*c_matFy
    *ELSE
        comp_critical_stress = 0.877*elastic_buckling_stress
    *ENDIF

    pn_phi_compression = c_matPhiCompression*comp_critical_stress*c_secArea

    uctable(i,44) = elastic_buckling_stress
    uctable(i,45) = comp_critical_stress
    uctable(i,46) = pn_phi_compression

    *IF,uctable(i,1),GT,0,THEN ! TENSION
        uctable(i,47) = 0
    *ELSE ! COMPRESSION
        uctable(i,47) = ABS(uctable(i,1))/pn_phi_compression
    *ENDIF

    *IF,uctable(i,7),GT,0,THEN ! TENSION
        uctable(i,48) = 0
    *ELSE ! COMPRESSION
        uctable(i,48) = ABS(uctable(i,7))/pn_phi_compression
    *ENDIF

    ! Checking Flexure (Both Axis)
    !------------------------------------------    
    
    mn_phi_2 = c_matPhiBending*c_matFy*c_secPlMod2
    mn_phi_3 = c_matPhiBending*c_matFy*c_secPlMod3
    ! Local Buckling not Applicable for Compact - the software filters for compact only

    uctable(i,50) = mn_phi_2
    uctable(i,51) = mn_phi_3

    uctable(i,52) = ABS(uctable(i,2))/mn_phi_2
    uctable(i,53) = ABS(uctable(i,3))/mn_phi_3

    uctable(i,54) = ABS(uctable(i,8))/mn_phi_2
    uctable(i,55) = ABS(uctable(i,9))/mn_phi_3


    ! Checking for Shear
    !------------------------------------------
        
    shear_fcr = (0.78*c_matYoungModulus)/((c_secDim1/c_secDim2)**1.5)

    vn_phi = (shear_fcr*c_secArea*c_matPhiShear) / 2

    uctable(i,57) = vn_phi
    uctable(i,58) = ABS(uctable(i,5))/vn_phi
    uctable(i,59) = ABS(uctable(i,6))/vn_phi
    uctable(i,60) = SQRT(uctable(i,5)**2+uctable(i,6)**2)/vn_phi

    uctable(i,61) = ABS(uctable(i,11))/vn_phi
    uctable(i,62) = ABS(uctable(i,12))/vn_phi
    uctable(i,63) = SQRT(uctable(i,11)**2+uctable(i,12)**2)/vn_phi


    ! Checking for Torsion
    !-----------------------------------------
    
    tor_fcr_H3_2a = (1.23*c_matYoungModulus)/(SQRT(c_elementLineLength/c_secDim1)*(c_secDim1/c_secDim2)**(5/4))
    tor_fcr_H3_2b = (0.60*c_matYoungModulus)/((c_secDim1/c_secDim2)**(3/2))
    tor_fcr_H3_2c = 0.6*c_matFy

    tor_fcr = 0
    *IF,tor_fcr_H3_2a,GT,tor_fcr_H3_2b,THEN 
        tor_fcr = tor_fcr_H3_2a
    *ELSE 
        tor_fcr = tor_fcr_H3_2b
    *ENDIF
    *IF,tor_fcr,GT,tor_fcr_H3_2c,THEN 
        tor_fcr = tor_fcr_H3_2c
    *ENDIF

    tn_phi = c_matPhiTorsion*tor_fcr*c_secTorsionalShearConstant

    uctable(i,65) = tn_phi
    uctable(i,66) = ABS(uctable(i,4))/tn_phi
    uctable(i,67) = ABS(uctable(i,10))/tn_phi


    ! Checking Combined 
    !------------------------------------------

    axialRatio_1 = 0 
    *IF,uctable(i,1),GT,0,THEN ! TENSION
        axialRatio_1 = uctable(i,41) 
    *ELSE ! COMPRESSION
        axialRatio_1 = uctable(i,47) 
    *ENDIF

    comb_ratio_1 = 0
    *IF,uctable(i,66),LE,0.2,THEN  ! Torsion Ignored

        *IF,axialRatio_1,GE,0.2,THEN 
            comb_ratio_1 = axialRatio_1+(8/9)*SQRT(uctable(i,52)**2+uctable(i,53)**2)
        *ELSE
            comb_ratio_1 = (axialRatio_1/2)+SQRT(uctable(i,52)**2+uctable(i,53)**2)
        *ENDIF
    
    *ELSE
            ! Torsion Considered
        comb_ratio_1 = axialRatio_1+SQRT(uctable(i,52)**2+uctable(i,53)**2)+(SQRT(uctable(i,58)**2+uctable(i,59)**2)+uctable(i,66))**2

    *ENDIF


    axialRatio_2 = 0 
    *IF,uctable(i,7),GT,0,THEN ! TENSION
        axialRatio_2 = uctable(i,42) 
    *ELSE ! COMPRESSION
        axialRatio_2 = uctable(i,48) 
    *ENDIF

    
    comb_ratio_2 = 0
    *IF,uctable(i,67),LE,0.2,THEN   ! Torsion Ignored

        *IF,axialRatio_2,GE,0.2,THEN 
            comb_ratio_2 = axialRatio_2+(8/9)*SQRT(uctable(i,54)**2+uctable(i,55)**2)
        *ELSE
            comb_ratio_2 = (axialRatio_2/2)+SQRT(uctable(i,54)**2+uctable(i,55)**2)
        *ENDIF
    
    *ELSE
            ! Torsion Considered
        comb_ratio_2 = axialRatio_2+SQRT(uctable(i,54)**2+uctable(i,55)**2)+(SQRT(uctable(i,61)**2+uctable(i,62)**2)+uctable(i,67))**2
    *ENDIF

    uctable(i,69) = axialRatio_1
    uctable(i,70) = comb_ratio_1
    uctable(i,71) = axialRatio_2
    uctable(i,72) = comb_ratio_2

*ENDDO

! Puts them in an ETABLE for future data display and acquire
ETABLE, iUC, SMISC, 1    ! Grabs bogus data to define an ETABLE
*VPUT,uctable(1,70),ELEM,1,ETAB,iUC  ! Overwrites the element table data for printouts
ETABLE, jUC, SMISC, 14   ! Grabs bogus data to define an ETABLE
*VPUT,uctable(1,72),ELEM,1,ETAB,jUC  ! Overwrites the element table data for printouts

! Writes the data to file
*CFOPEN,'{tempFileName}_inode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT','Pr','MrMajor','MrMinor','VrMajor','VrMinor','Tr','PRatio','MMajRatio','MMinRatio','VMajRatio','VMinRatio','TorRatio','PcComp','PcTension','McMajor','McMinor','TotalRatio'
%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C

! Writes the data
*VWRITE,SEQU,uctable(1,1),uctable(1,2),uctable(1,3),uctable(1,5),uctable(1,6),uctable(1,4),uctable(1,69),uctable(1,52),uctable(1,53),uctable(1,58),uctable(1,59),uctable(1,66),uctable(1,46),uctable(1,40),uctable(1,50),uctable(1,51),uctable(1,70)
%I,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G

*CFCLOSE


! Writes the data to file
*CFOPEN,'{tempFileName}_jnode','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT','Pr','MrMajor','MrMinor','VrMajor','VrMinor','Tr','PRatio','MMajRatio','MMinRatio','VMajRatio','VMinRatio','TorRatio','PcComp','PcTension','McMajor','McMinor','TotalRatio'
%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C,%C

! Writes the data
*VWRITE,SEQU,uctable(1,7),uctable(1,8),uctable(1,9),uctable(1,11),uctable(1,12),uctable(1,10),uctable(1,71),uctable(1,54),uctable(1,55),uctable(1,61),uctable(1,62),uctable(1,67),uctable(1,46),uctable(1,40),uctable(1,50),uctable(1,51),uctable(1,72)
%I,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G,%30.6G


*CFCLOSE
");

                            File_AppendCommandWithComment($"/RENAME,{tempFileName}_inode,txt,,{outFileName}_inode,txt", "Renames the result file");
                            File_AppendCommandWithComment($"/RENAME,{tempFileName}_jnode,txt,,{outFileName}_jnode,txt", "Renames the result file");
                            ExpectedOutputNotConsumedList.Add($"{outFileName}_inode.txt");
                            ExpectedOutputNotConsumedList.Add($"{outFileName}_jnode.txt");

                            _writeResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_CodeCheck);
                            break;

                        case FeResultTypeEnum.Element_StrainEnergy:
                            Sb.AppendLine($@"
! Gets the ELEMENTAL strain energy data
ETABLE, e_StrEn, SENE

! WRITE: STRAIN ENERGY
*DIM,elem_strain_enery,ARRAY,total_element_count,1
*VGET,elem_strain_enery(1,1),ELEM,1,ETAB,e_StrEn

! Writes the data to file
*CFOPEN,'{tempFileName}','txt' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'e_StrEn'
(A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,elem_strain_enery(1,1)
%I%C%30.6G

*CFCLOSE
");

                            File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                            ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                            _writeResultTouchedTypes.Add(FeResultTypeEnum.Element_StrainEnergy);
                            break;

                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            Sb.AppendLine($@"
/ OUTPUT, '{tempFileName}', 'txt'
SET,LIST
/OUTPUT");

                            File_AppendCommandWithComment($"/RENAME,{tempFileName},txt,,{outFileName},txt", "Renames the result file");
                            ExpectedOutputNotConsumedList.Add($"{outFileName}.txt");

                            _writeResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor);
                            _writeResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor);
                            _writeResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor);
                            break;

                        // Ignores - Calculated in C#
                        case FeResultTypeEnum.Element_Weight:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(inRes.ResultType), inRes.ResultType, "The family other does not contain the given Result Type.");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            File_EndSection();
        }
        private void WriteHelper_WriteScreenShots(IProblemQuantitySource inProbQuantSource)
        {
            // It is a special overview screenshot
            if (inProbQuantSource is SpecialScreenShot_FeModelOverview_Keypoints)
            {
                // For each of the directions
                foreach (ImageCaptureViewDirectionEnum imageCaptureViewDirectionEnum in AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable)
                {
                    string outFilename = inProbQuantSource.ScreenShotFileName + $"_{imageCaptureViewDirectionEnum}";

                    File_StartSection($"Post-Process - ScreenShot Overview KeyPoints | {ScreenShotOptions.GetFriendlyEnumName(imageCaptureViewDirectionEnum)} - File <{outFilename}.png>");

                    File_AppendCommandWithComment("/RESET", "Resets the plot controls");
                    File_AppendCommandWithComment("ALLSEL,ALL", "Selects Everything");
                    File_AppendCommandWithComment("/VUP,1,Z", "Sets the view up-down to match Rhino");
                    File_AppendCommandWithComment("/ANG,1", "Resets screen angle");

                    File_AppendCommandWithComment($"/VIEW,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_XDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_YDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_ZDir(imageCaptureViewDirectionEnum)}", "Sets the view from given point to the origin");
                    if (!double.IsNaN(AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum))) File_AppendCommandWithComment($"/ANG,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum)},ZS,1", "Rotates screen to align axes");

                    // Sets the other plot parameters
                    File_AppendCommandWithComment("/SHRINK,0", "0 => No Shrinkage. Shrinks elements, lines, areas, and volumes for display clarity.");

                    File_AppendCommandWithComment($"/ESHAPE,{AppSS.I.ScreenShotOpt.ImageCapture_Extrude_Multiplier}", "Displays elements with shapes determined from the real constants, section definition, or other inputs. If 0-> Display as lines.");

                    File_AppendCommandWithComment("/EFACET,4", "Specifies the number of facets per element edge for PowerGraphics displays.");
                    File_AppendCommandWithComment("/RATIO,1,1,1", "Ensures no Distortion - Distorts the object geometry.");
                    File_AppendCommandWithComment("/CFORMAT,32,0", "Controls the graphical display of alphanumeric character strings for parameters, components, assemblies, and tables.");

                    //sb.AppendLine("QUALITY,1,0,0,0,4,0 ");
                    File_AppendCommandWithComment("/AUTO,1", "Resets the focus and distance specifications to automatically calculated.");
                    File_AppendCommandWithComment("/PBC,DEFA", "Resets to Default. Degree of freedom constraint, force load, and other symbols to displays.");

                    // Redirecting the plot
                    Sb.AppendLine($@"! Redirects the plot to a PNG file File will come out as {_jobName}000.png
/SHOW,PNG,,0
PNGR,COMP,1,-1  
PNGR,ORIENT,HORIZ   
PNGR,COLOR,2
PNGR,TMOD,1 
/GFILE,600
/CMAP,_TEMPCMAP_,CMP,,SAVE  
/RGB,INDEX,100,100,100,0
/RGB,INDEX,0,0,0,15 ");

                    Sb.AppendLine($@"! Giving the plot instructions for the Keypoints
/PNUM,KP,1  
/PNUM,LINE,0
/PNUM,AREA,0
/PNUM,VOLU,0
/PNUM,NODE,0
/PNUM,TABN,0
/PNUM,SVAL,0
/NUMBER,0   
/PNUM,ELEM,0
/PSF,DEFA, ,1,0,1   
/PBF,DEFA, ,1   
/PSYMB,CS,0 
/PSYMB,NDIR,0   
/PSYMB,ESYS,0   
/PSYMB,LDIV,0   
/PSYMB,LDIR,0   
/PSYMB,ADIR,0   
/PSYMB,ECON,0   
/PSYMB,XNODE,0  
/PSYMB,DOT,1
/PSYMB,PCONV,   
/PSYMB,LAYR,0   
/PSYMB,FBCS,0   
/PBC,ALL, ,1
LPLOT  ");

                    Sb.AppendLine($@"! Some Clean-Up
/CMAP,_TEMPCMAP_,CMP
/DELETE,_TEMPCMAP_,CMP  
/SHOW,CLOSE            ! **** Closes the image file
/DEVICE,VECTOR,0");

                    File_AppendCommandWithComment($"/RENAME,{_jobName}000,png,,{outFilename},png", "Renames the image file");
                    ExpectedOutputNotConsumedList.Add($"{outFilename}.png");

                    File_EndSection();
                }
            }
            else if (inProbQuantSource is SpecialScreenShot_FeModelOverview_Lines)
            {
                // For each of the directions
                foreach (ImageCaptureViewDirectionEnum imageCaptureViewDirectionEnum in AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable)
                {
                    string outFilename = inProbQuantSource.ScreenShotFileName + $"_{imageCaptureViewDirectionEnum}";

                    File_StartSection($"Post-Process - ScreenShot Overview Lines | {ScreenShotOptions.GetFriendlyEnumName(imageCaptureViewDirectionEnum)} - File <{outFilename}.png>");

                    File_AppendCommandWithComment("/RESET", "Resets the plot controls");
                    File_AppendCommandWithComment("ALLSEL,ALL", "Selects Everything");
                    File_AppendCommandWithComment("/VUP,1,Z", "Sets the view up-down to match Rhino");
                    File_AppendCommandWithComment("/ANG,1", "Resets screen angle");

                    File_AppendCommandWithComment($"/VIEW,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_XDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_YDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_ZDir(imageCaptureViewDirectionEnum)}", "Sets the view from given point to the origin");
                    if (!double.IsNaN(AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum))) File_AppendCommandWithComment($"/ANG,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum)},ZS,1", "Rotates screen to align axes");

                    // Sets the other plot parameters
                    File_AppendCommandWithComment("/SHRINK,0", "0 => No Shrinkage. Shrinks elements, lines, areas, and volumes for display clarity.");

                    File_AppendCommandWithComment($"/ESHAPE,{AppSS.I.ScreenShotOpt.ImageCapture_Extrude_Multiplier}", "Displays elements with shapes determined from the real constants, section definition, or other inputs. If 0-> Display as lines.");

                    File_AppendCommandWithComment("/EFACET,4", "Specifies the number of facets per element edge for PowerGraphics displays.");
                    File_AppendCommandWithComment("/RATIO,1,1,1", "Ensures no Distortion - Distorts the object geometry.");
                    File_AppendCommandWithComment("/CFORMAT,32,0", "Controls the graphical display of alphanumeric character strings for parameters, components, assemblies, and tables.");

                    //sb.AppendLine("QUALITY,1,0,0,0,4,0 ");
                    File_AppendCommandWithComment("/AUTO,1", "Resets the focus and distance specifications to automatically calculated.");
                    File_AppendCommandWithComment("/PBC,DEFA", "Resets to Default. Degree of freedom constraint, force load, and other symbols to displays.");

                    // Redirecting the plot
                    Sb.AppendLine($@"! Redirects the plot to a PNG file File will come out as {_jobName}000.png
/SHOW,PNG,,0
PNGR,COMP,1,-1  
PNGR,ORIENT,HORIZ   
PNGR,COLOR,2
PNGR,TMOD,1 
/GFILE,600
/CMAP,_TEMPCMAP_,CMP,,SAVE  
/RGB,INDEX,100,100,100,0
/RGB,INDEX,0,0,0,15 ");

                    Sb.AppendLine($@"! Giving the plot instructions for the Lines
/PNUM,KP,0  
/PNUM,LINE,1
/PNUM,AREA,0
/PNUM,VOLU,0
/PNUM,NODE,0
/PNUM,TABN,0
/PNUM,SVAL,0
/NUMBER,0   
/PNUM,ELEM,0
/PSF,DEFA, ,1,0,1   
/PBF,DEFA, ,1   
/PSYMB,CS,0 
/PSYMB,NDIR,0   
/PSYMB,ESYS,0   
/PSYMB,LDIV,0   
/PSYMB,LDIR,0   
/PSYMB,ADIR,0   
/PSYMB,ECON,0   
/PSYMB,XNODE,0  
/PSYMB,DOT,1
/PSYMB,PCONV,   
/PSYMB,LAYR,0   
/PSYMB,FBCS,0   
/PBC,ALL, ,1
LPLOT ");

                    Sb.AppendLine($@"! Some Clean-Up
/CMAP,_TEMPCMAP_,CMP
/DELETE,_TEMPCMAP_,CMP  
/SHOW,CLOSE            ! **** Closes the image file
/DEVICE,VECTOR,0");

                    File_AppendCommandWithComment($"/RENAME,{_jobName}000,png,,{outFilename},png", "Renames the image file");
                    ExpectedOutputNotConsumedList.Add($"{outFilename}.png");

                    File_EndSection();
                }
            }

            // It is a NORMAL Result
            else if (inProbQuantSource is FeResultClassification inRes)
            {
                if (inRes.ResultType == FeResultTypeEnum.Element_Weight) return;

                // For each of the directions
                foreach (ImageCaptureViewDirectionEnum imageCaptureViewDirectionEnum in AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable)
                {
                    string outFilename = inRes.ScreenShotFileName + $"_{imageCaptureViewDirectionEnum}";

                    File_StartSection($"Post-Process - ScreenShot {inRes.ResultType} | {ScreenShotOptions.GetFriendlyEnumName(imageCaptureViewDirectionEnum)} - File <{outFilename}.png>");

                    File_AppendCommandWithComment("/RESET", "Resets the plot controls");
                    File_AppendCommandWithComment("ALLSEL,ALL", "Selects Everything");
                    File_AppendCommandWithComment("/VUP,1,Z", "Sets the view up-down to match Rhino");
                    File_AppendCommandWithComment("/ANG,1", "Resets screen angle");

                    File_AppendCommandWithComment($"/VIEW,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_XDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_YDir(imageCaptureViewDirectionEnum)},{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_ZDir(imageCaptureViewDirectionEnum)}", "Sets the view from given point to the origin");
                    if (!double.IsNaN(AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum))) File_AppendCommandWithComment($"/ANG,1,{AppSS.I.ScreenShotOpt.ImageCapture_AnsysHelper_RotateToAlign(imageCaptureViewDirectionEnum)},ZS,1", "Rotates screen to align axes");

                    // Sets the other plot parameters
                    File_AppendCommandWithComment("/SHRINK,0", "0 => No Shrinkage. Shrinks elements, lines, areas, and volumes for display clarity.");

                    File_AppendCommandWithComment($"/ESHAPE,{AppSS.I.ScreenShotOpt.ImageCapture_Extrude_Multiplier}", "Displays elements with shapes determined from the real constants, section definition, or other inputs. If 0-> Display as lines.");

                    File_AppendCommandWithComment("/EFACET,4", "Specifies the number of facets per element edge for PowerGraphics displays.");
                    File_AppendCommandWithComment("/RATIO,1,1,1", "Ensures no Distortion - Distorts the object geometry.");
                    File_AppendCommandWithComment("/CFORMAT,32,0", "Controls the graphical display of alphanumeric character strings for parameters, components, assemblies, and tables.");

                    //sb.AppendLine("QUALITY,1,0,0,0,4,0 ");
                    File_AppendCommandWithComment("/AUTO,1", "Resets the focus and distance specifications to automatically calculated.");
                    File_AppendCommandWithComment("/PBC,DEFA", "Resets to Default. Degree of freedom constraint, force load, and other symbols to displays.");

                    //Configures the deformed shape
                    if (AppSS.I.ScreenShotOpt.ImageCapture_DeformedShape)
                    {
                        File_AppendCommandWithComment($"/DSCALE, 1, AUTO", "Sets the displacement multiplier for displacement displays. Auto => 5% of max length");
                    }
                    else
                    {
                        File_AppendCommandWithComment($"/DSCALE, 1, OFF", "Sets the displacement multiplier for displacement displays. Off => Remove");
                    }

                    // Redirecting the plot
                    Sb.AppendLine($@"! Redirects the plot to a PNG file File will come out as {_jobName}000.png
/SHOW,PNG,,0
PNGR,COMP,1,-1  
PNGR,ORIENT,HORIZ   
PNGR,COLOR,2
PNGR,TMOD,1 
/GFILE,600
/CMAP,_TEMPCMAP_,CMP,,SAVE  
/RGB,INDEX,100,100,100,0
/RGB,INDEX,0,0,0,15 ");

                    string undefShape = AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow ? "1" : "0";
                    string forcePlotOnDeformed = AppSS.I.ScreenShotOpt.ImageCapture_DeformedShape ? "1" : "0";

                    switch (inRes.ResultType)
                    {
                        case FeResultTypeEnum.SectionNode_Stress_S1:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, S,1, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_S2:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, S,2, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_S3:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, S,3, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_SInt:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, S,INT, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Stress_SEqv:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, S,EQV, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Ux:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,X, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Uy:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,Y, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Uz:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,Z, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Rx:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, ROT,X, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Ry:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, ROT,Y, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_Rz:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, ROT,Z, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Nodal_Displacement_UTotal:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,SUM, {undefShape},1.0 ");
                            break;



                        case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, EPTT,1, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, EPTT,2, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, EPTT,3, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, EPTT,INT, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, EPTT,EQV, {undefShape},1.0 ");
                            break;



                        case FeResultTypeEnum.ElementNodal_Force_Fx:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_IFX,SMISC, 1", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JFX,SMISC, 14", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_IFX,PLOT_JFX,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_My:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_IMY,SMISC, 2", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JMY,SMISC, 15", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_IMY,PLOT_JMY,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_Mz:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_IMZ,SMISC, 3", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JMZ,SMISC, 16", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_IMZ,PLOT_JMZ,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_Tq:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_ITQ,SMISC, 4", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JTQ,SMISC, 17", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_ITQ,PLOT_JTQ,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_SFz:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_ISFZ,SMISC, 5", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JSFZ,SMISC, 18", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_ISFZ,PLOT_JSFZ,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Force_SFy:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"ETABLE,PLOT_ISFY,SMISC, 6", "Gets the data at I");
                            File_AppendCommandWithComment($"ETABLE,PLOT_JSFY,SMISC, 19", "Gets the data at J");
                            Sb.AppendLine($"PLLS,PLOT_ISFZ,PLOT_JSFZ,1,{forcePlotOnDeformed},1");
                            break;



                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                            Sb.AppendLine($"SET,1,1");
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,SUM, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                            Sb.AppendLine($"SET,1,2");
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,SUM, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            Sb.AppendLine($"SET,1,3");
                            File_AppendCommandWithComment($"/GLINE,1,-1", "Hide mesh lines in plots");
                            Sb.AppendLine($"PLNSOL, U,SUM, {undefShape},1.0 ");
                            break;

                        case FeResultTypeEnum.ElementNodal_CodeCheck:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            File_AppendCommandWithComment($"PLLS,iUC,jUC,1,{forcePlotOnDeformed},1", "The ETABLES have been set when getting the results.");
                            break;

                        case FeResultTypeEnum.Element_StrainEnergy:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLESOL, SENE,, {undefShape},1.0 ");
                            break;


                        case FeResultTypeEnum.Nodal_Reaction_Fx:
                        case FeResultTypeEnum.Nodal_Reaction_Fz:
                        case FeResultTypeEnum.Nodal_Reaction_Fy:
                            File_AppendCommandWithComment($"/PBC,U,1", "Displays the boundary Conditions - Translation");
                            File_AppendCommandWithComment($"/PBC,ROT,1", "Displays the boundary Conditions - Rotation");
                            File_AppendCommandWithComment($"/PBC,RFOR,1", "Displays the Reaction Forces");
                            File_AppendCommandWithComment($"/SHRINK,0", "Display onto lines");
                            File_AppendCommandWithComment($"/ESHAPE,0.0", "Display onto lines");
                            File_AppendCommandWithComment($"/EFACET,1", "Display onto lines");
                            File_AppendCommandWithComment($"EPLOT", "Plots elements with the symbols");
                            break;

                        case FeResultTypeEnum.Nodal_Reaction_My:
                        case FeResultTypeEnum.Nodal_Reaction_Mz:
                        case FeResultTypeEnum.Nodal_Reaction_Mx:
                            File_AppendCommandWithComment($"/PBC,U,1", "Displays the boundary Conditions - Translation");
                            File_AppendCommandWithComment($"/PBC,ROT,1", "Displays the boundary Conditions - Rotation");
                            File_AppendCommandWithComment($"/PBC,RMOM,1", "Displays the Reaction Moments");
                            File_AppendCommandWithComment($"/SHRINK,0", "Display onto lines");
                            File_AppendCommandWithComment($"/ESHAPE,0.0", "Display onto lines");
                            File_AppendCommandWithComment($"/EFACET,1", "Display onto lines");
                            File_AppendCommandWithComment($"EPLOT", "Plots elements with the symbols");
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEPELDIR,jEPELDIR,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEPELByT,jEPELByT,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEPELByB,jEPELByB,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEPELBzT,jEPELBzT,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEPELBzB,jEPELBzB,1,{forcePlotOnDeformed},1");
                            break;



                        case FeResultTypeEnum.ElementNodal_Strain_Ex:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iEx,jEx,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Ky:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iKy,jKy,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Kz:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iKz,jKz,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_Te:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iTe,jTe,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_SEz:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSEz,jSEz,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Strain_SEy:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSEy,jSEy,1,{forcePlotOnDeformed},1");
                            break;


                        case FeResultTypeEnum.ElementNodal_Stress_SDir:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSDIR,jSDIR,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SByT:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSByT,jSByT,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SByB:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSByB,jSByB,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSBzT,jSBzT,1,{forcePlotOnDeformed},1");
                            break;

                        case FeResultTypeEnum.ElementNodal_Stress_SBzB:
                            File_AppendCommandWithComment($"/GLINE,1,0", "Show mesh lines in plots");
                            Sb.AppendLine($"PLLS,iSBzB,jSBzB,1,{forcePlotOnDeformed},1");
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Sb.AppendLine($@"! Some Clean-Up
/CMAP,_TEMPCMAP_,CMP
/DELETE,_TEMPCMAP_,CMP  
/SHOW,CLOSE            ! **** Closes the image file
/DEVICE,VECTOR,0");

                    File_AppendCommandWithComment($"/RENAME,{_jobName}000,png,,{outFilename},png", "Renames the image file");
                    ExpectedOutputNotConsumedList.Add($"{outFilename}.png");

                    File_EndSection();
                }
            }
        }
        #endregion

        #region Read Functions - From the Output Files
        private readonly HashSet<string> _errorLogLines = new HashSet<string>(); 
        private readonly Regex _errorMessageRegex = new Regex(@"\*\*\*(?<kind>.*?)\*\*\*\s*CP\s*=\s*(?<cp>[\d\.\+\-eE]*)\s*TIME\s*=\s*(?<time>[\d:]*)\s*(?<message>.*?(?=\*\*\*|$))");
        private void GetErrorsAndThrow()
        {
            string errorFileLogFilePath = Path.Combine(FeWorkFolder, $"{_jobName}0.err");

            // The new lines that may be founds in each pass
            string accumulatedErrors = string.Empty;

            void lf_ReadLog() 
            {
                try
                {
                    // Reads the lines from the log file
                    StringBuilder sbNewLinesInFile = new StringBuilder();
                    using (FileStream fs = File.Open(errorFileLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            string fullText = reader.ReadToEnd();
                            if (!fullText.EndsWith(Environment.NewLine)) return; // Aborts - it must end with a new line

                            // Breaks the fulltext line by line
                            string[] lines = Regex.Split(fullText, "\r\n|\r|\n");

                            foreach (string l in lines)
                            {
                                // If it is a new line, adds it to the StringBuilder
                                if (_errorLogLines.Add(l)) sbNewLinesInFile.Append(l);
                            }
                        }
                    }

                    if (sbNewLinesInFile.Length > 0)
                    {
                        foreach (Match m in _errorMessageRegex.Matches(sbNewLinesInFile.ToString()))
                        {
                            if (m.Success)
                            {
                                switch (m.Groups["kind"].Value.Trim())
                                {
                                    case "ERROR":
                                        accumulatedErrors += $"{Regex.Replace(m.Groups["message"].Value, @"\s+", " ")}{Environment.NewLine}";
                                        break;

                                    case "WARNING":
                                        _model.Owner.RuntimeWarningMessages.Add(new NlOpt_Point_Message(Regex.Replace(m.Groups["message"].Value, @"\s+", " "), NlOpt_Point_MessageSourceEnum.FiniteElementSolver, NlOpt_Point_MessageLevelEnum.Warning));
                                        break;

                                    default:
                                        accumulatedErrors += $"Could not find the type of Ansys message given by {m.Groups["kind"].Value}.{Environment.NewLine}";
                                        break;
                                }
                            }
                        }


                    }
                }
                catch
                {
                    // Ignores the errors while reading the log file.
                }
            }

            lf_ReadLog();

            // Checks if the solver process is still alive
            _commandLineProcess = GetRunningCmdInstance();
            if (_commandLineProcess == null)
            {
                // It may have failed during reading of the log file - reads one final time
                lf_ReadLog();

                string message = $"The Ansys APDL process has terminated during the analysis. Usually this means that it did not converge.";
                accumulatedErrors += message;
            }

            if (!string.IsNullOrWhiteSpace(accumulatedErrors))
            {
                throw new FeSolverException(accumulatedErrors) { SolutionNotConverged = accumulatedErrors.Contains("Solution not converged") };
            }
        }

        private bool ReadHelper_ReadResultFile(string inFullFileName)
        {
            // Checks if the file is locked
            if (EmasaWPFLibraryStaticMethods.IsFileLocked(new FileInfo(inFullFileName))) return false;
            string fileName = Path.GetFileNameWithoutExtension(inFullFileName);

            // Mesh Info - Nodal Locations
            if (fileName == "ems_output_meshinfo_nodal_locations")
            {
                string line = null;
                Regex lineRegex = new Regex(@"^(?<NODE>\s+[\-\+\.\dE]+)(?<X>\s+[\-\+\.\dE]+)(?<Y>\s+[\-\+\.\dE]+)(?<Z>\s+[\-\+\.\dE]+)(?<THXY>\s+[\-\+\.\dE]+)(?<THYZ>\s+[\-\+\.\dE]+)(?<THZX>\s+[\-\+\.\dE]+)\s*$");

                using (StreamReader reader = new StreamReader(inFullFileName))
                {
                    try
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            Match m = lineRegex.Match(line);
                            if (!m.Success) continue;

                            string nodeId = m.Groups["NODE"].Value.Trim();

                            // Do we have a matching joint?
                            Point3d nodeCoords = new Point3d(double.Parse(m.Groups["X"].Value), double.Parse(m.Groups["Y"].Value), double.Parse(m.Groups["Z"].Value));
                            nodeCoords = _model.RoundedPoint3d(nodeCoords);

                            _model.MeshNodes.Add(nodeId, new FeMeshNode(nodeId, nodeCoords, _model.Get_JointByCoordinate(nodeCoords)));
                        }
                    }
                    catch (Exception)
                    {
                        throw new FeSolverException($"Could not parse the line {line} into a FeMeshNode while reading the results.");
                    }
                }

                _model.HasMeshNodes = true;
                return true;
            }

            // Aborts subsequent - the nodal locations must be read first!
            if (!_model.HasMeshNodes) return false;
            
            // Mesh Info - Elements of Lines
            else if (fileName == "ems_output_meshinfo_elements_of_lines")
            {
                using (StreamReader reader = new StreamReader(inFullFileName))
                {
                    using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Configuration.TrimOptions = TrimOptions.Trim;

                        var line_element_data_def = new
                            {
                            LINE = default(string),
                            ELEM = default(string),
                            INODE = default(string),
                            JNODE = default(string),
                            KNODE = default(string),
                            };
                        var records = csv.GetRecords(line_element_data_def);

                        // Saves all the elements in the element list for quick queries down the line
                        HashSet<string> addedList = new HashSet<string>();
                        foreach (var r in records)
                        {
                            if (addedList.Add(r.ELEM))
                            {
                                FeMeshBeamElement feBeamElement = new FeMeshBeamElement(r.ELEM, _model.MeshNodes[r.INODE], _model.MeshNodes[r.JNODE], r.KNODE == "0" ? null : _model.MeshNodes[r.KNODE]);

                                _model.MeshBeamElements.Add(r.ELEM, feBeamElement);

                                // Saves a CROSS reference to the Frame what owns this Mesh Element
                                feBeamElement.OwnerFrame = _model.Frames[r.LINE];
                                _model.Frames[r.LINE].MeshBeamElements.Add(feBeamElement);

                                // Saves the reference of this Beam Element into its MeshNodes
                                _model.MeshNodes[r.INODE].LinkedElements.Add(feBeamElement);
                                _model.MeshNodes[r.JNODE].LinkedElements.Add(feBeamElement);
                                
                                if (r.KNODE != "0") _model.MeshNodes[r.KNODE].LinkedElements.Add(feBeamElement);
                            }
                        }

                        // Links them to the frames
                        foreach (var r in records)
                        {
                            FeFrame frame = _model.Frames[r.LINE];
                            frame.MeshBeamElements.Add(_model.MeshBeamElements[r.ELEM]);
                        }
                    }
                }

                _model.HasMeshBeams = true;
                return true;
            }

            // Aborts subsequent - the Elements of Lines must be read first!
            if (!_model.HasMeshBeams) return false;

            // Checks the Selected Results
            foreach (FeResultClassification feResult in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                // The result file name does not match this selected result - skip
                if (!fileName.Contains(feResult.ResultFileName)) continue;
                // We found the result we wanted!

                // Switch by result family
                switch (feResult.ResultFamily)
                {
                    case FeResultFamilyEnum.Nodal_Reaction:
                    {
                        using (StreamReader reader = new StreamReader(inFullFileName))
                        {
                            try
                            {
                                string line = null;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    string nodeId = null;
                                    FeResultValue_NodalReactions react = new FeResultValue_NodalReactions();
                                    try
                                    {
                                        int start = 1;
                                        nodeId = line.Substring(start, 10).Trim();
                                        start += 11;

                                        // Fx
                                        string dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.FX = double.Parse(dataChunk);
                                        start += 20;

                                        // Fy
                                        dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.FY = double.Parse(dataChunk);
                                        start += 20;

                                        // Fz
                                        dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.FZ = double.Parse(dataChunk);
                                        start += 20;


                                        dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.MX = double.Parse(dataChunk);
                                        start += 20;


                                        dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.MY = double.Parse(dataChunk);
                                        start += 20;

                                        dataChunk = line.Substring(start, 20);
                                        if (!string.IsNullOrWhiteSpace(dataChunk)) react.MZ = double.Parse(dataChunk);
                                    }
                                    catch {}
                                    finally
                                    {
                                        if (!string.IsNullOrWhiteSpace(nodeId) && react.ContainsAnyValue)
                                        {
                                            _model.Results.Add(new FeResultItem(
                                                inResultClass: feResult,
                                                inFeLocation: FeResultLocation.CreateMeshNodeLocation(_model, _model.MeshNodes[nodeId]),
                                                inResultValue: react));
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                throw new FeSolverException($"Could not parse file {inFullFileName} to read the Nodal Results.");
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.Nodal_Displacement:
                    {
                        using (StreamReader reader = new StreamReader(inFullFileName))
                        {
                            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                            {
                                csv.Configuration.TrimOptions = TrimOptions.Trim;

                                var anonymousTypeDef = new
                                    {
                                    NODE = default(string),
                                    UX = default(double),
                                    UY = default(double),
                                    UZ = default(double),
                                    RX = default(double),
                                    RY = default(double),
                                    RZ = default(double)
                                    };
                                var records = csv.GetRecords(anonymousTypeDef);

                                foreach (var r in records)
                                {
                                    _model.Results.Add(new FeResultItem(
                                        inResultClass: feResult,
                                        inFeLocation: FeResultLocation.CreateMeshNodeLocation(_model, _model.MeshNodes[r.NODE]),
                                        inResultValue: new FeResultValue_NodalDisplacements()
                                            {
                                            UX = r.UX,
                                            UY = r.UY,
                                            UZ = r.UZ,
                                            RX = r.RX,
                                            RY = r.RY,
                                            RZ = r.RZ
                                            }));
                                }
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.SectionNode_Stress:
                    {
                        Regex dataRegex = new Regex(@"^(?<SECNODE>\s+[\d]+)(?<D1>\s+[\-\+\.\dE]+)(?<D2>\s+[\-\+\.\dE]+)(?<D3>\s+[\-\+\.\dE]+)(?<D4>\s+[\-\+\.\dE]+)(?<D5>\s+[\-\+\.\dE]+)");
                        Regex elementIdRegex = new Regex(@"^\s*ELEMENT\s*=\s*(?<ID>\d*)\s*SECTION");
                        Regex nodeIdRegex = new Regex(@"^\s*ELEMENT NODE =\s*(?<ID>\d*)");

                        using (StreamReader reader = new StreamReader(inFullFileName))
                        {
                            try
                            {
                                string line = null;
                                string currentElementId = null;
                                string currentNodeId = null;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    Match elementIdMatch = elementIdRegex.Match(line);
                                    if (elementIdMatch.Success)
                                    {
                                        currentElementId = elementIdMatch.Groups["ID"].Value.Trim();
                                        continue;
                                    }

                                    Match nodeIdMatch = nodeIdRegex.Match(line);
                                    if (nodeIdMatch.Success)
                                    {
                                        currentNodeId = nodeIdMatch.Groups["ID"].Value.Trim();
                                        continue;
                                    }

                                    Match dataMatch = dataRegex.Match(line);
                                    if (dataMatch.Success)
                                    {
                                        int secNodeId = int.Parse(dataMatch.Groups["SECNODE"].Value);
                                        double d1 = double.Parse(dataMatch.Groups["D1"].Value);
                                        double d2 = double.Parse(dataMatch.Groups["D2"].Value);
                                        double d3 = double.Parse(dataMatch.Groups["D3"].Value);
                                        double d4 = double.Parse(dataMatch.Groups["D4"].Value);
                                        double d5 = double.Parse(dataMatch.Groups["D5"].Value);

                                        FeMeshBeamElement beam = _model.MeshBeamElements[currentElementId];
                                        FeMeshNode node = _model.MeshBeamElements[currentElementId].GetNodeById(currentNodeId);
                                        FeMeshNode_SectionNode e = node.SectionNodes_AddNewOrGet(secNodeId);

                                        FeResultValue_SectionNodalStress secNodeRes = new FeResultValue_SectionNodalStress()
                                            {
                                            S1 = d1,
                                            S2 = d2,
                                            S3 = d3,
                                            SINT = d4,
                                            SEQV = d5,
                                        };

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateSectionNodeLocation(_model, beam, node, e),
                                            inResultValue: secNodeRes));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                throw new FeSolverException($"Could not parse file {inFullFileName} to read the Nodal Stress Results by OptimizationSection Point.", e);
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.SectionNode_Strain:
                    {
                        Regex dataRegex = new Regex(@"^(?<SECNODE>\s+[\d]+)(?<D1>\s+[\-\+\.\dE]+)(?<D2>\s+[\-\+\.\dE]+)(?<D3>\s+[\-\+\.\dE]+)(?<D4>\s+[\-\+\.\dE]+)(?<D5>\s+[\-\+\.\dE]+)");
                        Regex elementIdRegex = new Regex(@"^\s*ELEMENT\s*=\s*(?<ID>\d*)\s*SECTION");
                        Regex nodeIdRegex = new Regex(@"^\s*ELEMENT NODE =\s*(?<ID>\d*)");

                        using (StreamReader reader = new StreamReader(inFullFileName))
                        {
                            double tableDiscrepancyTolerance = 0.0001d;

                            try
                            {
                                string line = null;
                                string currentElementId = null;
                                string currentNodeId = null;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    Match elementIdMatch = elementIdRegex.Match(line);
                                    if (elementIdMatch.Success)
                                    {
                                        currentElementId = elementIdMatch.Groups["ID"].Value.Trim();
                                        continue;
                                    }

                                    Match nodeIdMatch = nodeIdRegex.Match(line);
                                    if (nodeIdMatch.Success)
                                    {
                                        currentNodeId = nodeIdMatch.Groups["ID"].Value.Trim();
                                        continue;
                                    }

                                    Match dataMatch = dataRegex.Match(line);
                                    if (dataMatch.Success)
                                    {
                                        int secNodeId = int.Parse(dataMatch.Groups["SECNODE"].Value);
                                        double d1 = double.Parse(dataMatch.Groups["D1"].Value);
                                        double d2 = double.Parse(dataMatch.Groups["D2"].Value);
                                        double d3 = double.Parse(dataMatch.Groups["D3"].Value);
                                        double d4 = double.Parse(dataMatch.Groups["D4"].Value);
                                        double d5 = double.Parse(dataMatch.Groups["D5"].Value);

                                        FeMeshBeamElement beam = _model.MeshBeamElements[currentElementId];
                                        FeMeshNode node = _model.MeshBeamElements[currentElementId].GetNodeById(currentNodeId);
                                        FeMeshNode_SectionNode e = node.SectionNodes_AddNewOrGet(secNodeId);

                                            FeResultValue_SectionNodalStrain secNodeRes = new FeResultValue_SectionNodalStrain()
                                            {
                                            EPTT1 = d1,
                                            EPTT2 = d2,
                                            EPTT3 = d3,
                                            EPTTINT = d4,
                                            EPTTEQV = d5,
                                            };

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateSectionNodeLocation(_model, beam, node, e),
                                            inResultValue: secNodeRes));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new FeSolverException($"Could not parse file {inFullFileName} to read the Nodal Strain Results by OptimizationSection Point.", ex);
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.ElementNodal_BendingStrain:
                    {
                        if (fileName.Contains("_inode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        iEPELDIR = default(double),
                                        iEPELByT = default(double),
                                        iEPELByB = default(double),
                                        iEPELBzT = default(double),
                                        iEPELBzB = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.INode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node), 
                                            inResultValue: new FeResultValue_ElementNodalBendingStrain()
                                                {
                                                EPELDIR = r.iEPELDIR,
                                                EPELByT = r.iEPELByT,
                                                EPELByB = r.iEPELByB,
                                                EPELBzT = r.iEPELBzT,
                                                EPELBzB = r.iEPELBzB
                                                }));
                                    }
                                }
                            }
                        }
                        else if (fileName.Contains("_jnode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        jEPELDIR = default(double),
                                        jEPELByT = default(double),
                                        jEPELByB = default(double),
                                        jEPELBzT = default(double),
                                        jEPELBzB = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.JNode;

                                            _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalBendingStrain()
                                                {
                                                EPELDIR = r.jEPELDIR,
                                                EPELByT = r.jEPELByT,
                                                EPELByB = r.jEPELByB,
                                                EPELBzT = r.jEPELBzT,
                                                EPELBzB = r.jEPELBzB
                                                }));
                                    }
                                }
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.ElementNodal_Force:
                    {
                        if (fileName.Contains("_inode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        iFx = default(double),
                                        iMy = default(double),
                                        iMz = default(double),
                                        iTq = default(double),
                                        iSFz = default(double),
                                        iSFy = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.INode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalForces()
                                                {
                                                Fx = r.iFx,
                                                My = r.iMy,
                                                Mz = r.iMz,
                                                Tq = r.iTq,
                                                SFy = r.iSFy,
                                                SFz = r.iSFz
                                                }));
                                    }
                                }
                            }
                        }
                        else if (fileName.Contains("_jnode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        jFx = default(double),
                                        jMy = default(double),
                                        jMz = default(double),
                                        jTq = default(double),
                                        jSFz = default(double),
                                        jSFy = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.JNode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalForces()
                                                {
                                                Fx = r.jFx,
                                                My = r.jMy,
                                                Mz = r.jMz,
                                                Tq = r.jTq,
                                                SFy = r.jSFy,
                                                SFz = r.jSFz
                                                }));
                                    }
                                }
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.ElementNodal_Strain:
                    {
                        if (fileName.Contains("_inode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        iEx = default(double),
                                        iKy = default(double),
                                        iKz = default(double),
                                        iSEz = default(double),
                                        iSEy = default(double),
                                        iTe = default(double),
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.INode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalStrain()
                                                {
                                                Ex = r.iEx,
                                                Ky = r.iKy,
                                                Kz = r.iKz,
                                                SEy = r.iSEy,
                                                SEz = r.iSEz,
                                                Te = r.iTe,
                                                }));
                                    }
                                }
                            }
                        }
                        else if (fileName.Contains("_jnode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        jEx = default(double),
                                        jKy = default(double),
                                        jKz = default(double),
                                        jSEz = default(double),
                                        jSEy = default(double),
                                        jTe = default(double),
                                    };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.JNode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalStrain()
                                                {
                                                Ex = r.jEx,
                                                Ky = r.jKy,
                                                Kz = r.jKz,
                                                SEy = r.jSEy,
                                                SEz = r.jSEz,
                                                Te = r.jTe,
                                                }));
                                    }
                                }
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.ElementNodal_Stress:
                    {
                        if (fileName.Contains("_inode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        iSDIR = default(double),
                                        iSByT = default(double),
                                        iSByB = default(double),
                                        iSBzT = default(double),
                                        iSBzB = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.INode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalStress()
                                                {
                                                SDIR = r.iSDIR,
                                                SByB = r.iSByB,
                                                SByT = r.iSByT,
                                                SBzB = r.iSBzB,
                                                SBzT = r.iSBzT
                                                }));
                                    }
                                }
                            }
                        }
                        else if (fileName.Contains("_jnode"))
                        {
                            using (StreamReader reader = new StreamReader(inFullFileName))
                            {
                                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                                    var anonymousTypeDef = new
                                        {
                                        ELEMENT = default(string),
                                        jSDIR = default(double),
                                        jSByT = default(double),
                                        jSByB = default(double),
                                        jSBzT = default(double),
                                        jSBzB = default(double)
                                        };
                                    var records = csv.GetRecords(anonymousTypeDef);

                                    foreach (var r in records)
                                    {
                                        FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                        FeMeshNode node = beam.JNode;

                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                            inResultValue: new FeResultValue_ElementNodalStress()
                                                {
                                                SDIR = r.jSDIR,
                                                SByB = r.jSByB,
                                                SByT = r.jSByT,
                                                SBzB = r.jSBzB,
                                                SBzT = r.jSBzT
                                                }));
                                    }
                                }
                            }
                        }
                    }
                        break;

                    case FeResultFamilyEnum.Others:
                    {
                        switch (feResult.ResultType)
                        {
                            case FeResultTypeEnum.ElementNodal_CodeCheck:
                            {
                                {
                                    if (fileName.Contains("_inode"))
                                    {
                                        using (StreamReader reader = new StreamReader(inFullFileName))
                                        {
                                            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                            {
                                                csv.Configuration.TrimOptions = TrimOptions.Trim;

                                                var anonymousTypeDef = new
                                                {
                                                    ELEMENT = default(string),
                                                    Pr = default(double),
                                                    MrMajor = default(double),
                                                    MrMinor = default(double),
                                                    VrMajor = default(double),
                                                    VrMinor = default(double),
                                                    Tr = default(double),
                                                    PRatio = default(double),
                                                    MMajRatio = default(double),
                                                    MMinRatio = default(double),
                                                    VMajRatio = default(double),
                                                    VMinRatio = default(double),
                                                    TorRatio = default(double),
                                                    PcComp = default(double),
                                                    PcTension = default(double),
                                                    McMajor = default(double),
                                                    McMinor = default(double),
                                                    TotalRatio = default(double),
                                
                                                };
                                                var records = csv.GetRecords(anonymousTypeDef);

                                                foreach (var r in records)
                                                {
                                                    FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                                    FeMeshNode node = beam.INode;

                                                    _model.Results.Add(new FeResultItem(
                                                        inResultClass: feResult,
                                                        inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                                        inResultValue: new FeResultValue_ElementNodalCodeCheck()
                                                        {
                                                            Pr = r.Pr,
                                                            MrMajor = r.MrMajor,
                                                            MrMinor = r.MrMinor,
                                                            VrMajor = r.VrMajor,
                                                            VrMinor = r.VrMinor,
                                                            Tr = r.Tr,
                                                            PRatio = r.PRatio,
                                                            MMajRatio = r.MMajRatio,
                                                            MMinRatio = r.MMinRatio,
                                                            VMajRatio = r.VMajRatio,
                                                            VMinRatio = r.VMinRatio,
                                                            TorRatio = r.TorRatio,
                                                            PcComp = r.PcComp,
                                                            PcTension = r.PcTension,
                                                            McMajor = r.McMajor,
                                                            McMinor = r.McMinor,
                                                            TotalRatio = r.TotalRatio,
                                                        }));
                                                }
                                            }
                                        }
                                    }
                                    else if (fileName.Contains("_jnode"))
                                    {
                                        using (StreamReader reader = new StreamReader(inFullFileName))
                                        {
                                            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                            {
                                                csv.Configuration.TrimOptions = TrimOptions.Trim;

                                                var anonymousTypeDef = new
                                                {
                                                    ELEMENT = default(string),
                                                Pr = default(double),
                                                MrMajor = default(double),
                                                MrMinor = default(double),
                                                VrMajor = default(double),
                                                VrMinor = default(double),
                                                Tr = default(double),
                                                PRatio = default(double),
                                                MMajRatio = default(double),
                                                MMinRatio = default(double),
                                                VMajRatio = default(double),
                                                VMinRatio = default(double),
                                                TorRatio = default(double),
                                                PcComp = default(double),
                                                PcTension = default(double),
                                                McMajor = default(double),
                                                McMinor = default(double),
                                                TotalRatio = default(double),
                                                };
                                                var records = csv.GetRecords(anonymousTypeDef);

                                                foreach (var r in records)
                                                {
                                                    FeMeshBeamElement beam = _model.MeshBeamElements[r.ELEMENT];
                                                    FeMeshNode node = beam.JNode;

                                                    _model.Results.Add(new FeResultItem(
                                                        inResultClass: feResult,
                                                        inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                                        inResultValue: new FeResultValue_ElementNodalCodeCheck()
                                                            {
                                                            Pr = r.Pr,
                                                            MrMajor = r.MrMajor,
                                                            MrMinor = r.MrMinor,
                                                            VrMajor = r.VrMajor,
                                                            VrMinor = r.VrMinor,
                                                            Tr = r.Tr,
                                                            PRatio = r.PRatio,
                                                            MMajRatio = r.MMajRatio,
                                                            MMinRatio = r.MMinRatio,
                                                            VMajRatio = r.VMajRatio,
                                                            VMinRatio = r.VMinRatio,
                                                            TorRatio = r.TorRatio,
                                                            PcComp = r.PcComp,
                                                            PcTension = r.PcTension,
                                                            McMajor = r.McMajor,
                                                            McMinor = r.McMinor,
                                                            TotalRatio = r.TotalRatio,
                                                        }));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                                break;

                            case FeResultTypeEnum.Element_StrainEnergy:
                            {
                                using (StreamReader reader = new StreamReader(inFullFileName))
                                {
                                    using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                    {
                                        csv.Configuration.TrimOptions = TrimOptions.Trim;

                                        var anonymousTypeDef = new
                                            {
                                            ELEMENT = default(string),
                                            e_StrEn = default(double),
                                            };
                                        var records = csv.GetRecords(anonymousTypeDef);

                                        foreach (var r in records)
                                        {
                                            _model.Results.Add(new FeResultItem(
                                                inResultClass: feResult,
                                                inFeLocation: FeResultLocation.CreateElementLocation(_model, _model.MeshBeamElements[r.ELEMENT]), 
                                                inResultValue: new FeResultValue_ElementStrainEnergy(r.e_StrEn)));
                                        }
                                    }
                                }
                            }
                                break;

                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            {
                                Regex dataRegex = new Regex(@"^\s*(?<mode>\d+)\s*(?<mult>[\-\+\.\deE]+)");

                                using (StreamReader reader = new StreamReader(inFullFileName))
                                {
                                    try
                                    {
                                        string line = null;

                                        FeResultValue_EigenvalueBucklingSummary eigenvalue = new FeResultValue_EigenvalueBucklingSummary();

                                        while ((line = reader.ReadLine()) != null)
                                        {
                                            Match dataMatch = dataRegex.Match(line);
                                            if (dataMatch.Success)
                                            {
                                                int mode = int.Parse(dataMatch.Groups["mode"].Value);
                                                double mult = double.Parse(dataMatch.Groups["mult"].Value);

                                                eigenvalue.EigenvalueBucklingMultipliers.Add(mode,mult);
                                                    
                                            }
                                        }

                                        if (eigenvalue.EigenvalueBucklingMultipliers.Count == 0) throw new FeSolverException($"The eigenvalue summary came out empty.");
  
                                        // adds to the result list
                                        _model.Results.Add(new FeResultItem(
                                            inResultClass: feResult,
                                            inFeLocation: FeResultLocation.CreateModelLocation(_model),
                                            inResultValue: eigenvalue));
                                    }
                                    catch (Exception e)
                                    {
                                        throw new FeSolverException($"Could not parse file {inFullFileName} to read the Eigenvalue Buckling Table.", e);
                                    }
                                }
                            }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                    }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return true;
            }

            // The file wasn't a result
            return false;
        }
        private bool ReadHelper_ReadScreenShotFile(string inFullFileName)
        {
            if (EmasaWPFLibraryStaticMethods.IsFileLocked(new FileInfo(inFullFileName))) return false;
            string fileName = Path.GetFileNameWithoutExtension(inFullFileName);

            // Checks for the special screenshots
            if (fileName.Contains(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance.ScreenShotFileName))
            {
                string dirString = fileName.Remove(0, AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance.ScreenShotFileName.Length + 1);

                // Parses the Direction
                ImageCaptureViewDirectionEnum dir = (ImageCaptureViewDirectionEnum)Enum.Parse(typeof(ImageCaptureViewDirectionEnum), dirString);

                // Saves the screen shot
                using (FileStream fs = new FileStream(inFullFileName, FileMode.Open))
                {
                    _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance, dir, Image.FromStream(fs)));
                }

                return true;
            }
            // Checks for the special screenshots
            if (fileName.Contains(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotLinesInstance.ScreenShotFileName))
            {
                string dirString = fileName.Remove(0, AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotLinesInstance.ScreenShotFileName.Length + 1);

                // Parses the Direction
                ImageCaptureViewDirectionEnum dir = (ImageCaptureViewDirectionEnum)Enum.Parse(typeof(ImageCaptureViewDirectionEnum), dirString);

                // Saves the screen shot
                using (FileStream fs = new FileStream(inFullFileName, FileMode.Open))
                {
                    _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotLinesInstance, dir, Image.FromStream(fs)));
                }

                return true;
            }


            // Checks the Selected Results
            foreach (FeResultClassification feResult in AppSS.I.FeOpt.Wpf_SelectedFiniteElementResultsForOutput.OfType<FeResultClassification>())
            {
                if (feResult.ResultType == FeResultTypeEnum.Element_Weight) continue;

                // The image file name does match this selected result - skip
                if (!fileName.Contains(feResult.ScreenShotFileName)) continue;

                string dirString = fileName.Remove(0, feResult.ScreenShotFileName.Length + 1);

                // Parses the Direction
                ImageCaptureViewDirectionEnum dir = (ImageCaptureViewDirectionEnum)Enum.Parse(typeof(ImageCaptureViewDirectionEnum), dirString);

                // Saves the screen shot
                using (FileStream fs = new FileStream(inFullFileName, FileMode.Open))
                {
                    _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(feResult, dir, Image.FromStream(fs)));
                }

                return true;
            }

            // The file wasn't a screen shot
            return false;
        }
        #endregion

        #region Syntactic sugar to deal with the input file StringBuilder
        private void File_AppendSpacingLines(int inCount = 3)
        {
            for (int i = 0; i < inCount; i++)
            {
                Sb.AppendLine();
            }
        }

        private void File_AppendHeader(string inHeader)
        {
            File_AppendSpacingLines(2);
            Sb.AppendLine("!***************************************************************************************************************************");
            Sb.Append("!*   ");
            Sb.AppendLine(inHeader);
            Sb.AppendLine("!***************************************************************************************************************************");
        }
        private void File_AppendHeader(IEnumerable<string> inHeaderLines)
        {
            Sb.AppendLine("!┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
            foreach (string item in inHeaderLines)
            {
                Sb.Append("!│   ");
                Sb.AppendLine(item);
            }
            Sb.AppendLine("!└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
        }
        private void File_AppendHeader(params string[] inHeaderLines)
        {
            File_AppendHeader((IEnumerable<string>)inHeaderLines);
        }

        private void File_StartSection(string inSectionName, int inSpacingLines = 2)
        {
            if (inSpacingLines > 0) File_AppendSpacingLines(inSpacingLines);
            Sb.Append("!\t\t|   ");
            Sb.AppendLine(inSectionName);
            Sb.AppendLine("!└─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
        }
        private void File_EndSection()
        {
            Sb.AppendLine("!──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
        }

        private void File_AppendCommentLine(string inLine)
        {
            Sb.Append("!-- ");
            Sb.AppendLine(inLine);
        }
        private void File_AppendCommentLines(IEnumerable<string> inLines)
        {
            foreach (string line in inLines)
            {
                Sb.Append("!-- ");
                Sb.AppendLine(line);
            }
        }

        private void File_AppendCommandWithComment(string inCommand, string inComment)
        {
            Sb.Append(inCommand);
            Sb.Append("\t\t!-- ");
            Sb.AppendLine(inComment);
        }
        #endregion
    }
}