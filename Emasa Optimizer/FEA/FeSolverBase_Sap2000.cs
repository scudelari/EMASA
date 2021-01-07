extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Loads;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using r3dm::Rhino.Geometry;
using Sap2000Library;
using Sap2000Library.Other;

namespace Emasa_Optimizer.FEA
{
    public class FeSolverBase_Sap2000 : FeSolverBase
    {
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
                catch
                {
                }

                Thread.Sleep(50);
                count++;
                if (count > 100) throw new IOException($"Could not clean-up the folder to be used as SAP2000's buffer: {FeWorkFolder}");
            }
        }

        protected override void InitializeSoftware()
        {
            // Creates the working directory of the SAP2000 file
            if (!Directory.Exists(FeWorkFolder)) Directory.CreateDirectory(FeWorkFolder);

            // Blocks the user input
            S2KStaticMethods.BlockInput(true);

            // Starts a new instance of SAP2000
            S2KModel.InitSingleton_NewInstance(UnitsEnum.N_m_C, true);
        }

        public override void ResetSoftwareData()
        {
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Closing Current Model.", "");

            S2KModel.SM.NewModelBlank(false, UnitsEnum.N_m_C);


            AppSS.I.UpdateOverlayTopMessage("SAP2000: Clearing Old Solution Files (will keep trying for 2 seconds).", "");

            Stopwatch attemptStopWatch = Stopwatch.StartNew();

            // Must clean-up the data for the next iteration
            DirectoryInfo dInfo = new DirectoryInfo(FeWorkFolder);
            if (!dInfo.Exists) throw new FeSolverException("Could not reset the Sap2000 folder between iterations.", new IOException($"The model directory does not exist; something is really wrong."));

            while (true)
            {
                try
                {
                    // Deletes all files in the directory
                    foreach (FileInfo fileInfo in dInfo.GetFiles())
                    {
                        fileInfo.Delete();
                    }

                    // Perhaps some other files should also be deleted 
                    break;
                }
                catch (Exception e)
                {
                    // Stops to check the elapsed time
                    attemptStopWatch.Stop();
                    if (attemptStopWatch.Elapsed.TotalSeconds > 2d) throw new FeSolverException("Could not reset the Sap2000 folder between iterations. Retried for 2 seconds.", e);

                    // Resumes counting time
                    attemptStopWatch.Start();
                    Thread.Sleep(250);
                }
            }

        }

        protected override void CloseApplication()
        {
            // Closing SAP2000
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Closing Application.", "");

            S2KModel.CloseSingleton(false);

            S2KStaticMethods.BlockInput(false);
        }
        public override List<Process> GetAllRunningProcesses()
        {
            // Gets all Ansys processes
            List<Process> sap2000Procs = Process.GetProcesses().Where(a => a.ProcessName.Contains("SAP2000")).ToList();

            // Simply returns - no treatment required
            return sap2000Procs;
        }

        internal readonly StringBuilder Sb = new StringBuilder();
        private FeModel _model = null;
        private readonly HashSet<string> _errorLogLines = new HashSet<string>();
        private readonly HashSet<string> _expectedOutputNotConsumedList = new HashSet<string>();
        private readonly Dictionary<int,int> _bucklingModePositiveMatch = new Dictionary<int, int>();

        public override void RunAnalysisAndCollectResults(FeModel inModel)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));

            // Various tests to check if Sap is alive
            bool isAlive = true;
            if (GetAllRunningProcesses().Count == 0) isAlive = false;
            try
            {
                isAlive = S2KModel.SM.IsAlive;
            }
            catch (Exception e)
            {
                isAlive = false;
            }

            if (!isAlive)
            {
                Stopwatch sw = Stopwatch.StartNew();

                AppSS.I.UpdateOverlayTopMessage("Sap2000: Initializing Software.", "");

                // Starts the software
                CleanUpDirectory();
                InitializeSoftware();

                sw.Stop();
                InitializingSoftwareTimeSpan += sw.Elapsed;
            }

            // Hides SAP
            //S2KModel.SM.WindowVisible = false;

            // Clean-up previous analysis
            try
            {
                ResetSoftwareData();
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Error while resetting the SAP2000 software data. {e.Message}");
            }

            _bucklingModePositiveMatch.Clear();
            _errorLogLines.Clear();
            _model = null;
            _model = inModel;


            #region Input file Generation
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Generating Input File.", "");

            _expectedOutputNotConsumedList.Clear();
            Sb.Clear();

            // Builds the basic model definition
            WriteHelper_BasicModelTables();
            WriteHelper_LoadsAndCombos();
            WriteHelper_JointsAndFrames();
            WriteHelper_TableOutputNamedSets();
            Sb.AppendLine(S2KModel.GetS2KTextFileTerminator);

            // Saves the file into the disk
            try
            {
                // Writes the file
                File.WriteAllText(Path.Combine(FeWorkFolder, "perfect.s2k"), Sb.ToString());
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Could not write the SAP2000 input file to the disk. {e.Message}");
            }

            try
            {
                // Manipulates SAP2000 to open the file
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Opening the Input File.", "");
                //if (!S2KModel.SM.OpenFile(Path.Combine(FeWorkFolder, "perfect.s2k"))) throw new FeSolverException($"Sap2000 failed to open the {Path.Combine(FeWorkFolder, "perfect.s2k")} file.");
                if (!S2KModel.SM.InterAuto.FlaUI_Action_OpenFileAndCloseDialog(Path.Combine(FeWorkFolder, "perfect.s2k"))) throw new FeSolverException($"Sap2000 failed to open the {Path.Combine(FeWorkFolder, "perfect.s2k")} file.");
            }
            catch (Exception e)
            {
                throw new FeSolverException($"Error reading the SAP2000 input file. {e.Message}");
            }

            // Basic Screenshot setup
            S2KModel.SM.InterAuto.FlaUI_Prepare_For_Screenshots(810, 610);
            #endregion

            #region Perfect Shape
            // The file is OPEN - Configuring what to run
            S2KModel.SM.AnalysisMan.SetAllNotToRun();
            S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);

            // Should we also run the perfect-shape Eigenvalue Buckling?
            if (AppSS.I.FeOpt.IsPerfectShape_EigenvalueBucking_Required) S2KModel.SM.AnalysisMan.SetCaseRunFlag("EVBUCK", true);

            // Runs the PERFECT model
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Perfect Shape Model.", "");
            S2KModel.SM.AnalysisMan.RunAnalysis();

            // Runs the code check if requested
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Code-Check of the Perfect Shape Model.", "");
            if (AppSS.I.FeOpt.PerfectShapeRequestedResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.PerfectShape && a.ResultType == FeResultTypeEnum.ElementNodal_CodeCheck))
            {
                S2KModel.SM.SteelDesignMan.StartDesign(TimeSpan.FromSeconds(10));
            }

            // Writes the stress outputs - perfect output is *always* necessary as it contains the basic meshed model data
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Writing stress the output of the Perfect Shape Model.", "");
            S2KModel.SM.InterAuto.FlaUI_Action_OutputTableNamedSetToS2K("OS_PERFECT", Path.Combine(FeWorkFolder, "perfect_stress_output.s2k"));

            // Reads the output data of the perfect results
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the stress the output of the Perfect Shape Model.", "");
            DataSet perfectStressDataSet = S2KModel.ReadDataSetFromS2K(Path.Combine(FeWorkFolder, "perfect_stress_output.s2k"));

            #region Reading the Mesh JOINTS
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Parsing the Joint Mesh Data and Getting Screenshots.", "");
            DataTable meshJoints = perfectStressDataSet.Tables["OBJECTS AND ELEMENTS - JOINTS"];
            foreach (DataRow row in meshJoints.AsEnumerable())
            {
                string nodeId = row.Field<string>("JointElem");

                Point3d nodeCoords = new Point3d(row.Field<double>("GlobalX"), row.Field<double>("GlobalY"), row.Field<double>("GlobalZ"));
                nodeCoords = _model.RoundedPoint3d(nodeCoords);

                // Matches a model joint - in SAP the joint already comes in the table if it is given
                _model.MeshNodes.Add(nodeId, 
                    !row.IsNull("JointObject") ? 
                        new FeMeshNode(nodeId, nodeCoords, _model.Joints[row.Field<string>("JointObject")]) : 
                        new FeMeshNode(nodeId, nodeCoords, null));
            }

            // Reads the Special screenshot of the Joints
            foreach (KeyValuePair<Sap2000ViewDirection, Image> image in S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_JointNames(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection)))
            {
                ImageCaptureViewDirectionEnum dir = GetMatchingImageCaptureViewDirection(image.Key);
                _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance, dir, image.Value));
            }
            #endregion

            #region Reading the Mesh ELEMENTS
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Parsing the Frame Mesh Data and Getting Screenshots.", "");
            DataTable meshElements = perfectStressDataSet.Tables["OBJECTS AND ELEMENTS - FRAMES"];
            foreach (DataRow r in meshElements.AsEnumerable())
            {
                FeMeshBeamElement feBeamElement = new FeMeshBeamElement(r.Field<string>("FrameElem"), _model.MeshNodes[r.Field<string>("ElemJtI")], _model.MeshNodes[r.Field<string>("ElemJtJ")], null);

                _model.MeshBeamElements.Add(r.Field<string>("FrameElem"), feBeamElement);

                // Saves a CROSS reference to the Frame what owns this Mesh Element
                feBeamElement.OwnerFrame = _model.Frames[r.Field<string>("FrameObject")];
                _model.Frames[r.Field<string>("FrameObject")].MeshBeamElements.Add(feBeamElement);

                // Saves the reference of this Beam Element into its MeshNodes
                _model.MeshNodes[r.Field<string>("ElemJtI")].LinkedElements.Add(feBeamElement);
                _model.MeshNodes[r.Field<string>("ElemJtJ")].LinkedElements.Add(feBeamElement);
            }

            // Reads the Special screenshot of the Frames
            foreach (KeyValuePair<Sap2000ViewDirection, Image> image in S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_FrameNames(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection)))
            {
                ImageCaptureViewDirectionEnum dir = GetMatchingImageCaptureViewDirection(image.Key);
                _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(AppSS.I.ScreenShotOpt.SpecialFeModelOverviewKeypointsScreenshotLinesInstance, dir, image.Value));
            }
            #endregion

            #region Reads perfect shape result
            AppSS.I.UpdateOverlayTopMessage("SAP2000: Parsing the Perfect Shape Results.", "");
            ReadHelper_ReadResultsFromDataSet(AppSS.I.FeOpt.PerfectShapeRequestedResults, perfectStressDataSet);

            // Filtering SAP2000's Eigenvalue Buckling for Positive Only
            DataTable perfectEigenvalueBucklingFactors = null;
            if (AppSS.I.FeOpt.IsPerfectShape_EigenvalueBucking_Required)
            {
                perfectEigenvalueBucklingFactors = perfectStressDataSet.Tables["BUCKLING FACTORS"];

                int positiveMode = 0;
                foreach (DataRow posRow in perfectEigenvalueBucklingFactors.AsEnumerable().Where(a => a.Field<double>("ScaleFactor") > 0d))
                {
                    _bucklingModePositiveMatch.Add(positiveMode, (int)posRow.Field<double>("StepNum"));
                    positiveMode++;
                }
            }

            AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the Perfect Shape Screenshots.", "");
            ReadHelper_GetSap2000ScreenShots(AppSS.I.FeOpt.PerfectShapeRequestedResults);
            #endregion

            #endregion

            #region Imperfect Shape Analyses - CHANGING JOINT COORDINATES
            if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required || AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
            {
                // Must change the coordinates
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Changing the Model Coordinates for the Imperfect Shape.", "");
                S2KModel.SM.InterAuto.FlaUI_Action_ModifyUndeformedGeometry(
                    inScaleFactor: AppSS.I.FeOpt.Imperfect_MultiplierFromBoundingBox ? (_model.Joints_BoundingBox_MaxLength / 1000d) : AppSS.I.FeOpt.Imperfect_Multiplier,
                    inCase: "EVBUCK",
                    _bucklingModePositiveMatch[AppSS.I.FeOpt.Imperfect_EigenvalueBucklingMode]
                    );
            }
            #endregion

            #region Imperfect Shape - Full Stiffness
            if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required)
            {
                S2KModel.SM.AnalysisMan.ModelLocked = false;
                S2KModel.SM.AnalysisMan.SetAllNotToRun();
                S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);

                // Should we also run the Eigenvalue Buckling?
                if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_EigenvalueBuckling_Required) S2KModel.SM.AnalysisMan.SetCaseRunFlag("EVBUCK", true);

                // Runs the model
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Imperfect Shape - Full Stiffness Model.", "");
                S2KModel.SM.AnalysisMan.RunAnalysis();

                // Runs the code check if requested
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Code-Check of the Imperfect Shape - Full Stiffness Model.", "");
                if (AppSS.I.FeOpt.ImperfectShapeFullStiffnessRequestedResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_FullStiffness && a.ResultType == FeResultTypeEnum.ElementNodal_CodeCheck))
                {
                    S2KModel.SM.SteelDesignMan.StartDesign(TimeSpan.FromSeconds(10));
                }

                // Writes the stress outputs - perfect output is *always* necessary as it contains the basic meshed model data
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Writing stress the output of the Imperfect Shape - Full Stiffness Model.", "");
                S2KModel.SM.InterAuto.FlaUI_Action_OutputTableNamedSetToS2K("OS_IMPFULL", Path.Combine(FeWorkFolder, "imperfectfull_stress_output.s2k"));

                // Reads the output data of the perfect results
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the stress the output of the Imperfect Shape - Full Stiffness Model.", "");
                DataSet imperfectFullStressDataSet = S2KModel.ReadDataSetFromS2K(Path.Combine(FeWorkFolder, "imperfectfull_stress_output.s2k"));

                AppSS.I.UpdateOverlayTopMessage("SAP2000: Parsing the Imperfect Shape - Full Stiffness Results.", "");
                ReadHelper_ReadResultsFromDataSet(AppSS.I.FeOpt.ImperfectShapeFullStiffnessRequestedResults, imperfectFullStressDataSet);

                AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the Imperfect Shape - Full Stiffness Screenshots.", "");
                ReadHelper_GetSap2000ScreenShots(AppSS.I.FeOpt.ImperfectShapeFullStiffnessRequestedResults);

            }
            #endregion

            #region Imperfect Shape - Soft
            if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
            {
                // Unlocks the model
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Softening the Steel Materials for the Imperfect Shape - Softened Model.", "");
                S2KModel.SM.AnalysisMan.ModelLocked = false;

                // Softens all the materials
                foreach (string sapMaterials in S2KModel.SM.MaterialMan.GetMaterialList(MatTypeEnum.Steel))
                {
                    S2KModel.SM.MaterialMan.SoftenMaterial(sapMaterials, 0.8d);
                }

                S2KModel.SM.AnalysisMan.SetAllNotToRun();
                S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);

                // Should we also run the Eigenvalue Buckling?
                if (AppSS.I.FeOpt.IsImperfectShapeSoftened_EigenvalueBuckling_Required) S2KModel.SM.AnalysisMan.SetCaseRunFlag("EVBUCK", true);

                // Runs the PERFECT model
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Imperfect Shape - Softened Model.", "");
                S2KModel.SM.AnalysisMan.RunAnalysis();

                // Runs the code check if requested
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Running the Code-Check of the Imperfect Shape - Softened Model.", "");
                if (AppSS.I.FeOpt.ImperfectShapeSoftenedRequestedResults.Any(a => a.TargetShape == FeAnalysisShapeEnum.ImperfectShape_Softened && a.ResultType == FeResultTypeEnum.ElementNodal_CodeCheck))
                {
                    S2KModel.SM.SteelDesignMan.StartDesign(TimeSpan.FromSeconds(10));
                }

                // Writes the stress outputs - perfect output is *always* necessary as it contains the basic meshed model data
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Writing stress the output of the Imperfect Shape - Softened Model.", "");
                S2KModel.SM.InterAuto.FlaUI_Action_OutputTableNamedSetToS2K("OS_IMPSOFT", Path.Combine(FeWorkFolder, "imperfectsoft_stress_output.s2k"));

                // Reads the output data of the perfect results
                AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the stress the output of the Imperfect Shape - Softened Model.", "");
                DataSet imperfectSoftStressDataSet = S2KModel.ReadDataSetFromS2K(Path.Combine(FeWorkFolder, "imperfectsoft_stress_output.s2k"));

                AppSS.I.UpdateOverlayTopMessage("SAP2000: Parsing the Imperfect Shape - Softened Results.", "");
                ReadHelper_ReadResultsFromDataSet(AppSS.I.FeOpt.ImperfectShapeSoftenedRequestedResults, imperfectSoftStressDataSet);

                AppSS.I.UpdateOverlayTopMessage("SAP2000: Getting the Imperfect Shape - Softened Screenshots.", "");
                ReadHelper_GetSap2000ScreenShots(AppSS.I.FeOpt.ImperfectShapeSoftenedRequestedResults);

            }
            #endregion
        }
        public override void GeneratePointModel(FeModel inModel, string inSaveFolder, string inFileName)
        {
            try
            {
                // Blocks the user input
                S2KStaticMethods.BlockInput(true);

                // Starts a new instance of SAP2000
                S2KModel.InitSingleton_NewInstance(UnitsEnum.N_m_C, true);

                _bucklingModePositiveMatch.Clear();
                _errorLogLines.Clear();
                _model = null;
                _model = inModel;
                _expectedOutputNotConsumedList.Clear();
                Sb.Clear();

                // Builds the basic model definition
                WriteHelper_BasicModelTables();
                WriteHelper_LoadsAndCombos();
                WriteHelper_JointsAndFrames();
                WriteHelper_TableOutputNamedSets();
                Sb.AppendLine(S2KModel.GetS2KTextFileTerminator);


                // Saves the file into the disk
                try
                {
                    // Writes the file
                    File.WriteAllText(Path.Combine(inSaveFolder, $"{inFileName}.s2k"), Sb.ToString());
                }
                catch (Exception e)
                {
                    throw new FeSolverException($"Could not write the SAP2000 input file to the disk. {e.Message}");
                }

                try
                {
                    //if (!S2KModel.SM.OpenFile(Path.Combine(FeWorkFolder, "perfect.s2k"))) throw new FeSolverException($"Sap2000 failed to open the {Path.Combine(FeWorkFolder, "perfect.s2k")} file.");
                    if (!S2KModel.SM.InterAuto.FlaUI_Action_OpenFileAndCloseDialog(Path.Combine(inSaveFolder, $"{inFileName}.s2k"))) throw new FeSolverException($"Sap2000 failed to open the {Path.Combine(inSaveFolder, $"{inFileName}.s2k")} file.");
                }
                catch (Exception e)
                {
                    throw new FeSolverException($"Error reading the SAP2000 input file. {e.Message}");
                }


                // The file is OPEN - Configuring what to run
                S2KModel.SM.AnalysisMan.SetAllNotToRun();
                S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);
                S2KModel.SM.AnalysisMan.SetCaseRunFlag("EVBUCK", true);

                // Do we need to update the shape?
                if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required || AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
                {
                    S2KModel.SM.AnalysisMan.RunAnalysis();

                    // Writes the stress outputs - perfect output is *always* necessary as it contains the basic meshed model data
                    S2KModel.SM.InterAuto.FlaUI_Action_OutputTableNamedSetToS2K("PNTEXPEVB", Path.Combine(inSaveFolder, "point_export_eigenvalue_bucklings.s2k"));

                    // Reads the output data of the perfect results
                    DataSet perfectStressDataSet = S2KModel.ReadDataSetFromS2K(Path.Combine(inSaveFolder, "point_export_eigenvalue_bucklings.s2k"));

                    // Reading the Eigenvalue Buckling results to make the match between the positive ones
                    DataTable perfectEigenvalueBucklingFactors = null;
                    perfectEigenvalueBucklingFactors = perfectStressDataSet.Tables["BUCKLING FACTORS"];
                    int positiveMode = 0;
                    foreach (DataRow posRow in perfectEigenvalueBucklingFactors.AsEnumerable().Where(a => a.Field<double>("ScaleFactor") > 0d))
                    {
                        _bucklingModePositiveMatch.Add(positiveMode, (int)posRow.Field<double>("StepNum"));
                        positiveMode++;
                    }

                    // Must change the coordinates
                    S2KModel.SM.InterAuto.FlaUI_Action_ModifyUndeformedGeometry(
                        inScaleFactor: AppSS.I.FeOpt.Imperfect_MultiplierFromBoundingBox ? (_model.Joints_BoundingBox_MaxLength / 1000d) : AppSS.I.FeOpt.Imperfect_Multiplier,
                        inCase: "EVBUCK",
                        _bucklingModePositiveMatch[AppSS.I.FeOpt.Imperfect_EigenvalueBucklingMode]
                    );

                    S2KModel.SM.AnalysisMan.ModelLocked = false;

                    // Is the Soft Shape Requested?
                    if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
                    {
                        // Softens all the materials
                        foreach (string sapMaterials in S2KModel.SM.MaterialMan.GetMaterialList(MatTypeEnum.Steel))
                        {
                            S2KModel.SM.MaterialMan.SoftenMaterial(sapMaterials, 0.8d);
                        }
                    }
                }

                // Saves the SAP2000 Model
                S2KModel.SM.SaveFile(Path.Combine(inSaveFolder, $"{inFileName}.sdb"));
            }
            finally
            {
                // Releases the user input
                S2KStaticMethods.BlockInput(false);
            }
        }

        private void WriteHelper_BasicModelTables()
        {
            List<DataTable> tables = new List<DataTable>();

            // TableFormat_ProgramControl
            {
                DataTable t = S2KModel.TableFormat_ProgramControl;
                DataRow row = t.NewRow();
                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_PreferencesDimensional
            {
                DataTable t = S2KModel.TableFormat_PreferencesDimensional;
                DataRow row = t.NewRow();

                row["MinFont"] = 8d;

                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_ActiveDegreesOfFreedom
            {
                DataTable t = S2KModel.TableFormat_ActiveDegreesOfFreedom;
                DataRow row = t.NewRow();
                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_AnalysisOptions
            {
                DataTable t = S2KModel.TableFormat_AnalysisOptions;
                DataRow row = t.NewRow();
                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_PreferencesSteelDesignAisc360_16
            {
                DataTable t = S2KModel.TableFormat_PreferencesSteelDesignAisc360_16;
                DataRow row = t.NewRow();
                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_CoordinateSystems
            //{
            //    DataTable t = S2KModel.TableFormat_CoordinateSystems;
            //    DataRow row = t.NewRow();
            //    t.Rows.Add(row); // Uses all default values
            //    tables.Add(t);
            //}

            // TableFormat_MaterialProperties01General
            {
                DataTable t = S2KModel.TableFormat_MaterialProperties01General;

                foreach (FeMaterial mat in _model.Materials)
                {
                    DataRow row = t.NewRow();

                    row["Material"] = mat.Name;
                    row["Type"] = "Steel";
                    row["Grade"] = mat.Name;

                    t.Rows.Add(row); 
                }
                
                tables.Add(t);
            }

            // TableFormat_MaterialProperties02BasicMechanicalProperties
            {
                DataTable t = S2KModel.TableFormat_MaterialProperties02BasicMechanicalProperties;

                foreach (FeMaterial mat in _model.Materials)
                {
                    DataRow row = t.NewRow();

                    row["Material"] = mat.Name;
                    row["UnitWeight"] = mat.Density * 9.806650166;
                    row["UnitMass"] = mat.Density;
                    row["E1"] = mat.YoungModulus;
                    row["U12"] = mat.Poisson;
                    row["A1"] = mat.ThermalCoefficient;

                    t.Rows.Add(row);
                }

                tables.Add(t);
            }

            // TableFormat_MaterialProperties03ASteelData
            {
                DataTable t = S2KModel.TableFormat_MaterialProperties03ASteelData;

                foreach (FeMaterial mat in _model.Materials)
                {
                    DataRow row = t.NewRow();

                    row["Material"] = mat.Name;
                    row["Fy"] = mat.Fy;
                    row["Fu"] = mat.Fu;
                    row["EffFy"] = mat.Fy * 1.1d;
                    row["EffFu"] = mat.Fu * 1.1d;

                    row["SHard"] = 0.015d;
                    row["SMax"] = 0.11d;
                    row["SRup"] = 0.17d;
                    row["FinalSlope"] = -0.1d;

                    t.Rows.Add(row);
                }

                tables.Add(t);
            }

            // TableFormat_FrameSectionProperties01General
            {
                DataTable t = S2KModel.TableFormat_FrameSectionProperties01General;

                foreach (FeSection sec in _model.Sections)
                {
                    DataRow row = t.NewRow();

                    row["SectionName"] = sec.Name;
                    row["Material"] = sec.Material.Name;

                    switch (sec)
                    {
                        case FeSectionPipe feSectionPipe:
                            row["Shape"] = "Pipe";
                            row["t3"] = feSectionPipe.OuterDiameter; // Outer diameter 
                            row["tw"] = feSectionPipe.Thickness; // Thickness
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Sectuion type not supported: {nameof(sec)}");
                    }

                    t.Rows.Add(row);
                }

                tables.Add(t);
            }

            // Aggregates in a single chunk of text and returns
            foreach (DataTable dataTable in tables) Sb.Append(S2KModel.GetS2KTextFileFormat(dataTable));
        }
        private void WriteHelper_LoadsAndCombos()
        {
            List<DataTable> tables = new List<DataTable>();

            // TableFormat_LoadPatternDefinitions
            {
                DataTable t = S2KModel.TableFormat_LoadPatternDefinitions;
                DataRow row = t.NewRow();

                if (_model.Loads.OfType<FeLoad_Inertial>().Count() > 1) throw new FeSolverException("Only one FeLoad_Inertial is supported and it is taken as DEAD load.");

                if (_model.Loads.OfType<FeLoad_Inertial>().Count() == 0)
                {
                    // The DEAD load pattern
                    row["LoadPat"] = "DEAD";
                    row["DesignType"] = "Dead";
                    row["SelfWtMult"] = 0d;
                }
                else
                {
                    foreach (FeLoad_Inertial feLoad_Inertial in _model.Loads.OfType<FeLoad_Inertial>())
                    {
                        // The DEAD load pattern
                        row["LoadPat"] = "DEAD";
                        row["DesignType"] = "Dead";
                        row["SelfWtMult"] = feLoad_Inertial.Multiplier;
                    }
                }

                t.Rows.Add(row); // Uses all default values
                tables.Add(t);
            }

            // TableFormat_JointLoads_Force
            if (_model.Loads.OfType<FeLoad_Point>().Any())
            {
                DataTable t = S2KModel.TableFormat_JointLoads_Force;

                foreach (FeLoad_Point pointLoad in _model.Loads.OfType<FeLoad_Point>())
                {
                    foreach (FeJoint j in _model.Groups[pointLoad.GhGeom.FeGroupNameHelper].Joints)
                    {
                        DataRow row = t.NewRow();
                        row["Joint"] = j.Id;
                        row["LoadPat"] = "DEAD";

                        if (pointLoad.Nominal.X != 0d) row["F1"] = pointLoad.Nominal.X * pointLoad.Multiplier;
                        if (pointLoad.Nominal.Y != 0d) row["F2"] = pointLoad.Nominal.Y * pointLoad.Multiplier;
                        if (pointLoad.Nominal.Z != 0d) row["F3"] = pointLoad.Nominal.Z * pointLoad.Multiplier;

                        t.Rows.Add(row); // Uses all default values
                    }
                }

                tables.Add(t);
            }

            // TableFormat_LoadCaseDefinitions
            {
                DataTable t = S2KModel.TableFormat_LoadCaseDefinitions;

                // Adds DEAD Non-Linear Static
                DataRow row = t.NewRow();
                row["Case"] = "DEAD";
                row["Type"] = "NonStatic";
                row["InitialCond"] = "Zero";
                row["DesignType"] = "Dead";
                row["DesignAct"] = "Non-Composite";
                row["RunCase"] = false;
                t.Rows.Add(row);

                // Adds Eigenvalue Buckling
                row = t.NewRow();
                row["Case"] = "EVBUCK";
                row["Type"] = "LinBuckling";
                row["InitialCond"] = "Zero";
                row["DesignType"] = "Dead";
                row["DesignAct"] = "Other";
                row["RunCase"] = false;
                t.Rows.Add(row);

                tables.Add(t);
            }

            // TableFormat_CaseBuckling1General
            {
                DataTable t = S2KModel.TableFormat_CaseBuckling1General;
                DataRow row = t.NewRow();

                row["Case"] = "EVBUCK";
                row["NumBuckMode"] = 12d;

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_CaseBuckling2LoadAssignments
            {
                DataTable t = S2KModel.TableFormat_CaseBuckling2LoadAssignments;
                DataRow row = t.NewRow();

                row["Case"] = "EVBUCK";
                row["LoadName"] = "DEAD";
                row["LoadSF"] = 1d;

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_CaseStatic1LoadAssignments
            {
                DataTable t = S2KModel.TableFormat_CaseStatic1LoadAssignments;
                DataRow row = t.NewRow();

                row["Case"] = "DEAD";
                row["LoadName"] = "DEAD";
                row["LoadSF"] = 1d;

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_CaseStatic2NonLinearLoadApplication
            {
                DataTable t = S2KModel.TableFormat_CaseStatic2NonLinearLoadApplication;
                DataRow row = t.NewRow();

                row["Case"] = "DEAD";
                row["LoadApp"] = "Full Load";

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_CaseStatic4NonLinearParameters
            {
                DataTable t = S2KModel.TableFormat_CaseStatic4NonLinearParameters;
                DataRow row = t.NewRow();

                row["Case"] = "DEAD";
                // The rest is default

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_CombinationDefinitions
            {
                DataTable t = S2KModel.TableFormat_CombinationDefinitions;
                DataRow row = t.NewRow();

                row["ComboName"] = "DEADCOMB";
                row["ComboType"] = "Linear Add";
                row["AutoDesign"] = false;
                row["CaseName"] = "DEAD";
                row["ScaleFactor"] = 1d;

                t.Rows.Add(row);
                tables.Add(t);
            }

            // TableFormat_AutoCombinationOptionData01General
            {
                DataTable t = S2KModel.TableFormat_AutoCombinationOptionData01General;
                DataRow row = t.NewRow();

                row["DesignType"] = "Steel";

                t.Rows.Add(row);
                tables.Add(t);
            }

            // Aggregates in a single chunk of text and returns
            foreach (DataTable dataTable in tables) Sb.Append(S2KModel.GetS2KTextFileFormat(dataTable));
        }
        private void WriteHelper_JointsAndFrames(DataTable inDeltas_JointDisplacementFormattedTable = null)
        {
            List<DataTable> tables = new List<DataTable>();

            // TableFormat_JointCoordinates && TableFormat_JointRestraintAssignments
            {
                DataTable jc = S2KModel.TableFormat_JointCoordinates;
                DataTable jrest = S2KModel.TableFormat_JointRestraintAssignments;

                foreach (KeyValuePair<string, FeJoint> feJoint in _model.Joints.OrderBy(a => a.Key))
                {
                    DataRow jc_row = jc.NewRow();

                    // Do we have a delta to incorporate?
                    (double x, double y, double z)? jointDelta = null;

                    // Gets the row containing the delta to apply to this displacement
                    DataRow r = inDeltas_JointDisplacementFormattedTable?.AsEnumerable().FirstOrDefault(a => a.Field<string>("Joint") == feJoint.Value.Id.ToString());
                    if (r != null) jointDelta = (r.Field<double>("U1"), r.Field<double>("U2"), r.Field<double>("U3"));

                    jc_row["Joint"] = feJoint.Value.Id.ToString();
                    jc_row["XorR"] = feJoint.Value.Point.X + (jointDelta?.x ?? 0d);
                    jc_row["Y"] = feJoint.Value.Point.Y + (jointDelta?.y ?? 0d);
                    jc_row["Z"] = feJoint.Value.Point.Z + (jointDelta?.z ?? 0d);
                    jc_row["SpecialJt"] = true;

                    jc.Rows.Add(jc_row);

                    // Also has restraints?
                    if (feJoint.Value.Restraint != null && feJoint.Value.Restraint.ExistAny)
                    {
                        DataRow jrest_row = jrest.NewRow();

                        jrest_row["Joint"] = feJoint.Value.Id.ToString();
                        jrest_row["U1"] = feJoint.Value.Restraint.U1;
                        jrest_row["U2"] = feJoint.Value.Restraint.U2;
                        jrest_row["U3"] = feJoint.Value.Restraint.U3;
                        jrest_row["R1"] = feJoint.Value.Restraint.R1;
                        jrest_row["R2"] = feJoint.Value.Restraint.R2;
                        jrest_row["R3"] = feJoint.Value.Restraint.R3;

                        jrest.Rows.Add(jrest_row);
                    }
                }

                tables.Add(jc);
                tables.Add(jrest);
            }

            // FRAME Tables:
            // TableFormat_ConnectivityFrame
            // TableFormat_FrameSectionAssignments
            // TableFormat_FrameReleaseAssignments1General - NOT USED
            // TableFormat_FrameOutputStationAssignments
            // TableFormat_FrameAutoMeshAssignments
            {
                DataTable f_conn = S2KModel.TableFormat_ConnectivityFrame;
                DataTable f_secAssign = S2KModel.TableFormat_FrameSectionAssignments;
                //DataTable j_rel = S2KModel.TableFormat_FrameReleaseAssignments1General;
                DataTable f_output = S2KModel.TableFormat_FrameOutputStationAssignments;
                DataTable f_mesh = S2KModel.TableFormat_FrameAutoMeshAssignments;
                DataTable f_designOverrides = S2KModel.TableFormat_OverwritesSteelDesignAisc360_16;

                foreach (var feFrame in _model.Frames.OrderBy(a => a.Key))
                {
                    // Conn
                    DataRow f_conn_row = f_conn.NewRow();
                    f_conn_row["Frame"] = feFrame.Value.Id.ToString();
                    f_conn_row["JointI"] = feFrame.Value.IJoint.Id.ToString();
                    f_conn_row["JointJ"] = feFrame.Value.JJoint.Id.ToString();
                    f_conn.Rows.Add(f_conn_row);



                    // Frame assign
                    DataRow f_secAssign_row = f_secAssign.NewRow();
                    f_secAssign_row["Frame"] = feFrame.Value.Id.ToString();
                    f_secAssign_row["AnalSect"] = feFrame.Value.Section.Name;
                    f_secAssign.Rows.Add(f_secAssign_row);



                    // Output Stations
                    DataRow f_output_row = f_output.NewRow();
                    f_output_row["Frame"] = feFrame.Value.Id.ToString();
                    f_output_row["MinNumSta"] = 1;
                    f_output_row["AddAtElmInt"] = true;
                    f_output.Rows.Add(f_output_row);

                    // Mesh
                    DataRow f_mesh_row = f_mesh.NewRow();
                    f_mesh_row["Frame"] = feFrame.Value.Id.ToString();
                    f_mesh.Rows.Add(f_mesh_row);

                    // Design Overrides
                    DataRow f_designOverrides_row = f_designOverrides.NewRow();
                    f_designOverrides_row["Frame"] = feFrame.Value.Id.ToString();
                    f_designOverrides_row["XLMajor"] = 1d;
                    f_designOverrides_row["XLMinor"] = 1d;
                    f_designOverrides_row["XLLTB"] = 1d;
                    f_designOverrides.Rows.Add(f_designOverrides_row);
                }

                tables.Add(f_conn);
                tables.Add(f_secAssign);
                tables.Add(f_output);
                tables.Add(f_mesh);

            }

            // Aggregates in a single chunk of text and returns
            foreach (DataTable dataTable in tables) Sb.Append(S2KModel.GetS2KTextFileFormat(dataTable));
        }
        private void WriteHelper_TableOutputNamedSets()
        {
            List<DataTable> tables = new List<DataTable>();
            HashSet<(string shape, FeResultTypeEnum type)> writeResultTouchedTypes = new HashSet<(string shape, FeResultTypeEnum type)>();
            

            // TableFormat_NamedSetsDatabaseTables1General
            DataTable t1 = S2KModel.TableFormat_NamedSetsDatabaseTables1General;
            DataRow row1;

            // !!!!!!!!!!!!    NOTE - HERE THE SIDE OPTIONS ARE ALSO SET    !!!!!!!!!!!!

            // The tables for selected output
            row1 = t1.NewRow();
            row1["DBNamedSet"] = "OS_PERFECT";
            t1.Rows.Add(row1);

            if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required)
            {
                // The tables for selected output
                row1 = t1.NewRow();
                row1["DBNamedSet"] = "OS_IMPFULL";
                t1.Rows.Add(row1);
            }
                
            if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
            {
                // The tables for selected output
                row1 = t1.NewRow();
                row1["DBNamedSet"] = "OS_IMPSOFT";
                t1.Rows.Add(row1);
            }

            if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
            {
                // The tables for selected output
                row1 = t1.NewRow();
                row1["DBNamedSet"] = "PNTEXPEVB";
                t1.Rows.Add(row1);
            }

            //row = t.NewRow();
            //row["DBNamedSet"] = $"BLKMODES";
            //row["ModeStart"] = "1"; // SAP has a BUG that does not allow you to set the mode...
            //row["ModeEnd"] = "All";
            //t.Rows.Add(row);

            tables.Add(t1);

            // TableFormat_NamedSetsDatabaseTables2Selections
            DataTable t = S2KModel.TableFormat_NamedSetsDatabaseTables2Selections;
            DataRow row;

            void lf_addList(string inShape, IEnumerable<FeResultClassification> inRequestedResults)
            {
                // Treating the *requested* outputs
                foreach (FeResultClassification feResultClassification in inRequestedResults)
                {
                    // Jumps if the result output has already been requested
                    if (writeResultTouchedTypes.Contains((inShape, feResultClassification.ResultType))) continue;

                    switch (feResultClassification.ResultFamily)
                    {
                        case FeResultFamilyEnum.Nodal_Reaction:

                            row = t.NewRow();
                            row["DBNamedSet"] = inShape;
                            row["SelectType"] = "Table";
                            row["Selection"] = "Joint Reactions";
                            t.Rows.Add(row);

                            // Adds to the treated hashset
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_Fx));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_Fy));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_Fz));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_Mx));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_My));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Reaction_Mz));
                            break;

                        case FeResultFamilyEnum.Nodal_Displacement:

                            row = t.NewRow();
                            row["DBNamedSet"] = inShape;
                            row["SelectType"] = "Table";
                            row["Selection"] = "Joint Displacements";
                            t.Rows.Add(row);

                            // Adds to the treated hashset
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Ux));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Uy));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Uz));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Rx));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Ry));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_Rz));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Nodal_Displacement_UTotal));
                            break;

                        case FeResultFamilyEnum.SectionNode_Stress:

                            row = t.NewRow();
                            row["DBNamedSet"] = inShape;
                            row["SelectType"] = "Table";
                            row["Selection"] = "Element Stresses - Frames";
                            t.Rows.Add(row);

                            // Adds to the treated hashset
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.SectionNode_Stress_S1));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.SectionNode_Stress_S2));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.SectionNode_Stress_S3));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.SectionNode_Stress_SInt));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.SectionNode_Stress_SEqv));
                            break;



                        case FeResultFamilyEnum.ElementNodal_Force:

                            row = t.NewRow();
                            row["DBNamedSet"] = inShape;
                            row["SelectType"] = "Table";
                            row["Selection"] = "Element Forces - Frames";
                            t.Rows.Add(row);

                            // Adds to the treated hashset
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_Fx));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_SFy));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_SFz));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_Tq));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_My));
                            writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_Force_Mz));
                            break;

                        case FeResultFamilyEnum.ElementNodal_Strain:
                        case FeResultFamilyEnum.ElementNodal_Stress:
                        case FeResultFamilyEnum.SectionNode_Strain:
                        case FeResultFamilyEnum.ElementNodal_BendingStrain:
                            throw new FeSolverException($"Results of family {feResultClassification.ResultFamily} are not supported by SAP2000.");

                        case FeResultFamilyEnum.Others:
                            switch (feResultClassification.ResultType)
                            {
                                case FeResultTypeEnum.ElementNodal_CodeCheck:

                                    row = t.NewRow();
                                    row["DBNamedSet"] = inShape;
                                    row["SelectType"] = "Table";
                                    row["Selection"] = "STEEL DESIGN 2 - PMM DETAILS - AISC 360-16";
                                    t.Rows.Add(row);

                                    writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.ElementNodal_CodeCheck));
                                    break;

                                case FeResultTypeEnum.Element_StrainEnergy:
                                    throw new FeSolverException($"Results of family {feResultClassification.ResultFamily} are not supported by SAP2000.");

                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                                case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:

                                    row = t.NewRow();
                                    row["DBNamedSet"] = inShape;
                                    row["SelectType"] = "Table";
                                    row["Selection"] = "Buckling Factors";
                                    t.Rows.Add(row);

                                    writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor));
                                    writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor));
                                    writeResultTouchedTypes.Add((inShape, FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor));
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException(nameof(feResultClassification.ResultType), feResultClassification.ResultType, "The family other does not contain the given Result Type.");
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            #region Perfect Shape
            // Analysis Messages
            row = t.NewRow();
            row["DBNamedSet"] = "OS_PERFECT";
            row["SelectType"] = "Table";
            row["Selection"] = "Analysis Messages";
            t.Rows.Add(row);

            // Positions and links of the mesh elements and joints
            row = t.NewRow();
            row["DBNamedSet"] = "OS_PERFECT";
            row["SelectType"] = "Table";
            row["Selection"] = "Objects And Elements - Joints";
            t.Rows.Add(row);

            // Positions and links of the mesh elements and joints
            row = t.NewRow();
            row["DBNamedSet"] = "OS_PERFECT";
            row["SelectType"] = "Table";
            row["Selection"] = "Objects And Elements - Frames";
            t.Rows.Add(row);

            if (AppSS.I.FeOpt.IsPerfectShape_EigenvalueBucking_Required)
            {
                row = t.NewRow();
                row["DBNamedSet"] = "OS_PERFECT";
                row["SelectType"] = "Table";
                row["Selection"] = "Buckling Factors";
                t.Rows.Add(row);

                writeResultTouchedTypes.Add(("OS_PERFECT", FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor));
                writeResultTouchedTypes.Add(("OS_PERFECT", FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor));
                writeResultTouchedTypes.Add(("OS_PERFECT", FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor));
            }

            lf_addList("OS_PERFECT", AppSS.I.FeOpt.PerfectShapeRequestedResults);

            // Adding the Pattern Selections for outputs
            row = t.NewRow();
            row["DBNamedSet"] = "OS_PERFECT";
            row["SelectType"] = "LoadPattern";
            row["Selection"] = "DEAD";
            t.Rows.Add(row);

            // Adding the Pattern Selections for outputs
            row = t.NewRow();
            row["DBNamedSet"] = "OS_PERFECT";
            row["SelectType"] = "LoadCase";
            row["Selection"] = "DEAD";
            t.Rows.Add(row);
            #endregion

            if (AppSS.I.FeOpt.IsImperfectShapeFullStiffness_StaticAnalysis_Required)
            {
                #region Imperfect FullStiffness

                // Analysis Messages
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPFULL";
                row["SelectType"] = "Table";
                row["Selection"] = "Analysis Messages";
                t.Rows.Add(row);

                lf_addList("OS_IMPFULL", AppSS.I.FeOpt.ImperfectShapeFullStiffnessRequestedResults);

                // Adding the Pattern Selections for outputs
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPFULL";
                row["SelectType"] = "LoadPattern";
                row["Selection"] = "DEAD";
                t.Rows.Add(row);

                // Adding the Pattern Selections for outputs
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPFULL";
                row["SelectType"] = "LoadCase";
                row["Selection"] = "DEAD";
                t.Rows.Add(row);

                #endregion
            }

            if (AppSS.I.FeOpt.IsImperfectShapeSoftened_StaticAnalysis_Required)
            {
                #region Imperfect Soft

                // Analysis Messages
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPSOFT";
                row["SelectType"] = "Table";
                row["Selection"] = "Analysis Messages";
                t.Rows.Add(row);

                lf_addList("OS_IMPSOFT", AppSS.I.FeOpt.ImperfectShapeSoftenedRequestedResults);

                // Adding the Pattern Selections for outputs
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPSOFT";
                row["SelectType"] = "LoadPattern";
                row["Selection"] = "DEAD";
                t.Rows.Add(row);

                // Adding the Pattern Selections for outputs
                row = t.NewRow();
                row["DBNamedSet"] = "OS_IMPSOFT";
                row["SelectType"] = "LoadCase";
                row["Selection"] = "DEAD";
                t.Rows.Add(row);

                #endregion
            }

            row = t.NewRow();
            row["DBNamedSet"] = "PNTEXPEVB";
            row["SelectType"] = "Table";
            row["Selection"] = "Buckling Factors";
            t.Rows.Add(row);

            //#region Buckling Modes
            //// Adding the Pattern Selections for outputs
            //row = t.NewRow();
            //row["DBNamedSet"] = $"BLKMODES";
            //row["SelectType"] = "LoadPattern";
            //row["Selection"] = "DEAD";
            //t.Rows.Add(row);

            //// Adding the Pattern Selections for outputs
            //row = t.NewRow();
            //row["DBNamedSet"] = $"BLKMODES";
            //row["SelectType"] = "LoadCase";
            //row["Selection"] = "EVBUCK";
            //t.Rows.Add(row);

            //// Adding the displacement table
            //row = t.NewRow();
            //row["DBNamedSet"] = $"BLKMODES";
            //row["SelectType"] = "Table";
            //row["Selection"] = "Joint Displacements";
            //t.Rows.Add(row);
            //#endregion


            tables.Add(t);
            

            // Aggregates in a single chunk of text and returns
            foreach (DataTable dataTable in tables) Sb.Append(S2KModel.GetS2KTextFileFormat(dataTable));
        }

        private Sap2000ViewDirection GetMatchingSap2000ViewDirection(ImageCaptureViewDirectionEnum inDir)
        {
            switch (inDir)
            {
                case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                    return Sap2000ViewDirection.Top_Towards_ZNeg;

                case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                    return Sap2000ViewDirection.Front_Towards_YPos;

                case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                    return Sap2000ViewDirection.Back_Towards_YNeg;

                case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                    return Sap2000ViewDirection.Right_Towards_XNeg;

                case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                    return Sap2000ViewDirection.Left_Towards_XPos;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                    return Sap2000ViewDirection.Perspective_Top_Front_Edge;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                    return Sap2000ViewDirection.Perspective_Top_Back_Edge;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                    return Sap2000ViewDirection.Perspective_Top_Right_Edge;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                    return Sap2000ViewDirection.Perspective_Top_Left_Edge;

                case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                    return Sap2000ViewDirection.Perspective_TFR_Corner;

                case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                    return Sap2000ViewDirection.Perspective_TFL_Corner;

                case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                    return Sap2000ViewDirection.Perspective_TBR_Corner;

                case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                    return Sap2000ViewDirection.Perspective_TBL_Corner;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inDir), inDir, null);
            }
        }
        private ImageCaptureViewDirectionEnum GetMatchingImageCaptureViewDirection(Sap2000ViewDirection inDir)
        {
            switch (inDir)
            {
                case Sap2000ViewDirection.Top_Towards_ZNeg:
                    return ImageCaptureViewDirectionEnum.Top_Towards_ZNeg;

                case Sap2000ViewDirection.Front_Towards_YPos:
                    return ImageCaptureViewDirectionEnum.Front_Towards_YPos;

                case Sap2000ViewDirection.Back_Towards_YNeg:
                    return ImageCaptureViewDirectionEnum.Back_Towards_YNeg;

                case Sap2000ViewDirection.Right_Towards_XNeg:
                    return ImageCaptureViewDirectionEnum.Right_Towards_XNeg;

                case Sap2000ViewDirection.Left_Towards_XPos:
                    return ImageCaptureViewDirectionEnum.Left_Towards_XPos;

                case Sap2000ViewDirection.Perspective_Top_Front_Edge:
                    return ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge;

                case Sap2000ViewDirection.Perspective_Top_Back_Edge:
                    return ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge;

                case Sap2000ViewDirection.Perspective_Top_Right_Edge:
                    return ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge;

                case Sap2000ViewDirection.Perspective_Top_Left_Edge:
                    return ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge;

                case Sap2000ViewDirection.Perspective_TFR_Corner:
                    return ImageCaptureViewDirectionEnum.Perspective_TFR_Corner;

                case Sap2000ViewDirection.Perspective_TFL_Corner:
                    return ImageCaptureViewDirectionEnum.Perspective_TFL_Corner;

                case Sap2000ViewDirection.Perspective_TBR_Corner:
                    return ImageCaptureViewDirectionEnum.Perspective_TBR_Corner;

                case Sap2000ViewDirection.Perspective_TBL_Corner:
                    return ImageCaptureViewDirectionEnum.Perspective_TBL_Corner;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inDir), inDir, null);
            }
        }

        private void ReadHelper_ReadResultsFromDataSet(IEnumerable<FeResultClassification> inResults, DataSet inDataSet)
        {
            HashSet<FeResultTypeEnum> readResultTouchedTypes = new HashSet<FeResultTypeEnum>();

            foreach (FeResultClassification feResult in inResults)
            {
                // Ignore as it has already been treated
                if (readResultTouchedTypes.Contains(feResult.ResultType)) continue;

                switch (feResult.ResultFamily)
                {
                    case FeResultFamilyEnum.Nodal_Reaction:
                    {
                        DataTable t = inDataSet.Tables["JOINT REACTIONS"];

                        foreach (DataRow r in t.AsEnumerable())
                        {
                            FeResultValue_NodalReactions react = new FeResultValue_NodalReactions()
                                {
                                FX = r.Field<double>("F1"),
                                FY = r.Field<double>("F2"),
                                FZ = r.Field<double>("F3"),
                                MX = r.Field<double>("M1"),
                                MY = r.Field<double>("M2"),
                                MZ = r.Field<double>("M3"),
                            };

                            _model.Results.Add(new FeResultItem(
                                inResultClass: feResult,
                                inFeLocation: FeResultLocation.CreateMeshNodeLocation(_model, _model.MeshNodes[r.Field<string>("Joint")]),
                                inResultValue: react));
                        }

                        // Marks the results as read
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fx);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fy);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Fz);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Mx);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_My);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Reaction_Mz);
                    }
                        break;

                    case FeResultFamilyEnum.Nodal_Displacement:
                    {
                        DataTable t = inDataSet.Tables["JOINT DISPLACEMENTS"];

                        foreach (var r in t.AsEnumerable())
                        {
                            _model.Results.Add(new FeResultItem(
                                inResultClass: feResult,
                                inFeLocation: FeResultLocation.CreateMeshNodeLocation(_model, _model.MeshNodes[r.Field<string>("Joint")]),
                                inResultValue: new FeResultValue_NodalDisplacements()
                                    {
                                    UX = r.Field<double>("U1"),
                                    UY = r.Field<double>("U2"),
                                    UZ = r.Field<double>("U3"),
                                    RX = r.Field<double>("R1"),
                                    RY = r.Field<double>("R2"),
                                    RZ = r.Field<double>("R3"),
                                }));
                        }

                        // Adds to the treated hashset
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Ux);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Uy);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Uz);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Rx);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Ry);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_Rz);
                        readResultTouchedTypes.Add(FeResultTypeEnum.Nodal_Displacement_UTotal);
                    }
                        break;

                    case FeResultFamilyEnum.SectionNode_Stress:
                    {
                        DataTable t = inDataSet.Tables["ELEMENT STRESSES - FRAMES"];

                        foreach (var r in t.AsEnumerable())
                        {
                            FeMeshBeamElement beam = _model.MeshBeamElements[r.Field<string>("FrameElem")];

                            // Depends on the station - if 0 equal to i joint
                            FeMeshNode node = r.Field<double>("ElemStation") < 0.0001d ? beam.INode : beam.JNode;

                            FeMeshNode_SectionNode e = node.SectionNodes_AddNewOrGet(int.Parse(r.Field<string>("Point")));

                            FeResultValue_SectionNodalStress secNodeRes = new FeResultValue_SectionNodalStress()
                            {
                                S1 = r.Field<double>("S11"),
                                S2 = r.Field<double>("S12"),
                                S3 = r.Field<double>("S13"),
                                SINT = 0d,
                                SEQV = r.Field<double>("SVM"),
                            };

                            _model.Results.Add(new FeResultItem(
                                inResultClass: feResult,
                                inFeLocation: FeResultLocation.CreateSectionNodeLocation(_model, beam, node, e),
                                inResultValue: secNodeRes));

                        }

                        // Adds to the treated hashset
                        readResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S1);
                        readResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S2);
                        readResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_S3);
                        readResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_SInt);
                        readResultTouchedTypes.Add(FeResultTypeEnum.SectionNode_Stress_SEqv);
                    }
                        break;


                    case FeResultFamilyEnum.ElementNodal_Force:
                        {
                            DataTable t = inDataSet.Tables["ELEMENT FORCES - FRAMES"];

                            foreach (var r in t.AsEnumerable())
                            {
                                FeMeshBeamElement beam = _model.MeshBeamElements[r.Field<string>("FrameElem")];
                                // Depends on the station - if 0 equal to i joint
                                FeMeshNode node = r.Field<double>("ElemStation") < 0.0001d ? beam.INode : beam.JNode;

                                _model.Results.Add(new FeResultItem(
                                    inResultClass: feResult,
                                    inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                    inResultValue: new FeResultValue_ElementNodalForces()
                                        {
                                        Fx = r.Field<double>("P"),
                                        My = r.Field<double>("M2"),
                                        Mz = r.Field<double>("M3"),
                                        Tq = r.Field<double>("T"),
                                        SFy = r.Field<double>("V2"),
                                        SFz = r.Field<double>("V3"),
                                    }));
                            }

                            // Adds to the treated hashset
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Fx);
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_SFy);
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_SFz);
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Tq);
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_My);
                            readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_Force_Mz);
                        }
                        break;

                    case FeResultFamilyEnum.ElementNodal_Strain:
                    case FeResultFamilyEnum.ElementNodal_Stress:
                    case FeResultFamilyEnum.SectionNode_Strain:
                    case FeResultFamilyEnum.ElementNodal_BendingStrain:
                        throw new FeSolverException($"Results of family {feResult.ResultFamily} are not supported by SAP2000.");


                    case FeResultFamilyEnum.Others:
                        switch (feResult.ResultType)
                        {
                            case FeResultTypeEnum.ElementNodal_CodeCheck:
                                {
                                    DataTable t = inDataSet.Tables["STEEL DESIGN 2 - PMM DETAILS - AISC 360-16"];

                                    foreach (var r in t.AsEnumerable())
                                    {
                                        // SAP2000 Outputs WORST per frame only - this will copy the values in all elements
                                        FeFrame frame = _model.Frames[r.Field<string>("Frame")];

                                        foreach (FeMeshBeamElement beam in frame.MeshBeamElements)
                                        {
                                            foreach (FeMeshNode node in beam.MeshNodes_NoK)
                                            {
                                                _model.Results.Add(new FeResultItem(
                                                    inResultClass: feResult,
                                                    inFeLocation: FeResultLocation.CreateElementNodeLocation(_model, beam, node),
                                                    inResultValue: new FeResultValue_ElementNodalCodeCheck()
                                                        {
                                                        Pr = r.Field<double>("Pr"),
                                                        MrMajor = r.Field<double>("MrMajor"),
                                                        MrMinor = r.Field<double>("MrMinor"),
                                                        VrMajor = r.Field<double>("VrMajor"),
                                                        VrMinor = r.Field<double>("VrMinor"),
                                                        Tr = r.Field<double>("Tr"),

                                                        PRatio = r.Field<double>("PRatio"),
                                                        MMajRatio = r.Field<double>("MMajRatio"),
                                                        MMinRatio = r.Field<double>("MMinRatio"),
                                                        VMajRatio = r.Field<double>("VMajRatio"),
                                                        VMinRatio = r.Field<double>("VMinRatio"),
                                                        TorRatio = r.Field<double>("TorRatio"),

                                                        PcComp = r.Field<double>("PcComp"),
                                                        PcTension = r.Field<double>("PcTension"),
                                                        MrMajorDsgn = r.Field<double>("MrMajorDsgn"),
                                                        McMajor = r.Field<double>("McMajor"),
                                                        MrMinorDsgn = r.Field<double>("MrMinorDsgn"),
                                                        McMinor = r.Field<double>("McMinor"),

                                                        TotalRatio = r.Field<double>("TotalRatio"),
                                                    }));
                                            }
                                        }

                                    }

                                    // Adds to the treated hashset
                                    readResultTouchedTypes.Add(FeResultTypeEnum.ElementNodal_CodeCheck);
                                }


                                break;

                            case FeResultTypeEnum.Element_StrainEnergy:
                                throw new FeSolverException($"Results of family {feResult.ResultFamily} are not supported by SAP2000.");

                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                            case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            {
                                DataTable t = inDataSet.Tables["BUCKLING FACTORS"];

                                FeResultValue_EigenvalueBucklingSummary eigenvalue = new FeResultValue_EigenvalueBucklingSummary();

                                foreach (DataRow r in t.AsEnumerable().OrderBy(a => (int)a.Field<double>("StepNum")))
                                {
                                    eigenvalue.EigenvalueBucklingMultipliers.Add((int)r.Field<double>("StepNum"), r.Field<double>("ScaleFactor"));
                                }

                                if (eigenvalue.EigenvalueBucklingMultipliers.Count == 0) throw new FeSolverException($"The eigenvalue summary came out empty.");

                                // adds to the result list
                                _model.Results.Add(new FeResultItem(
                                    inResultClass: feResult,
                                    inFeLocation: FeResultLocation.CreateModelLocation(_model),
                                    inResultValue: eigenvalue));

                                readResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor);
                                readResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor);
                                readResultTouchedTypes.Add(FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor);
                            }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(feResult.ResultType), feResult.ResultType, "The family other does not contain the given Result Type.");
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


        }
        private void ReadHelper_GetSap2000ScreenShots(IEnumerable<FeResultClassification> inResults)
        {
            // For each requested result
            foreach (FeResultClassification feResult in inResults)
            {
                Dictionary<Sap2000ViewDirection, Image> resultScreenshots;

                switch (feResult.ResultType)
                {
                    case FeResultTypeEnum.Nodal_Reaction_Fx:
                    case FeResultTypeEnum.Nodal_Reaction_Fy:
                    case FeResultTypeEnum.Nodal_Reaction_Fz:
                    case FeResultTypeEnum.Nodal_Reaction_Mx:
                    case FeResultTypeEnum.Nodal_Reaction_My:
                    case FeResultTypeEnum.Nodal_Reaction_Mz:
                        {
                            // Did we already get these results?
                            List<NlOpt_Point_ScreenShot> alreadyAcquiredScreenshots = _model.Owner.ScreenShots.Where(a =>
                            {
                                // Gets the screenshots that are a result Classification AND that are a Nodal Reaction
                                if (a.Result is FeResultClassification resClass && resClass.ResultFamily == feResult.ResultFamily) return true;
                                return false;
                            }).ToList();

                            // We have already gotten these screenshots - we must copy them
                            if (alreadyAcquiredScreenshots.Count > 0)
                            {
                                // Makes a temp list
                                resultScreenshots = new Dictionary<Sap2000ViewDirection, Image>();

                                foreach (ImageCaptureViewDirectionEnum requestedDirection in AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable)
                                {
                                    // Gets the first image in the list with this direction
                                    NlOpt_Point_ScreenShot first = alreadyAcquiredScreenshots.First(a => a.Direction == requestedDirection);
                                    resultScreenshots.Add(GetMatchingSap2000ViewDirection(requestedDirection), first.Image);
                                }
                            }
                            else // Acquire them from SAP
                            {
                                resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_JointReaction(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection), "DEAD");
                            }
                        }

                        break;


                    case FeResultTypeEnum.Nodal_Displacement_Ux:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection), 
                            inCase: "DEAD", 
                            inContour: "Ux",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow);
                        break;

                    case FeResultTypeEnum.Nodal_Displacement_Uy:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inContour: "Uy",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow);
                        break;

                    case FeResultTypeEnum.Nodal_Displacement_Uz:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inContour: "Uz",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow);
                        break;

                    case FeResultTypeEnum.Nodal_Displacement_UTotal:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inContour: "Resultant",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow);
                        break;

                    case FeResultTypeEnum.Nodal_Displacement_Rx:
                    case FeResultTypeEnum.Nodal_Displacement_Ry:
                    case FeResultTypeEnum.Nodal_Displacement_Rz:
                    {
                        // Did we already get these results?
                        List<NlOpt_Point_ScreenShot> alreadyAcquiredScreenshots = _model.Owner.ScreenShots.Where(a =>
                        {
                            // Gets the screenshots that are a result Classification AND that are a Nodal Reaction
                            if (a.Result is FeResultClassification resClass && 
                                (resClass.ResultType == FeResultTypeEnum.Nodal_Displacement_Rx ||
                                 resClass.ResultType == FeResultTypeEnum.Nodal_Displacement_Ry ||
                                 resClass.ResultType == FeResultTypeEnum.Nodal_Displacement_Rz) ) return true;
                            return false;
                        }).ToList();

                        // We have already gotten these screenshots - we must copy them
                        if (alreadyAcquiredScreenshots.Count > 0)
                        {
                            // Makes a temp list
                            resultScreenshots = new Dictionary<Sap2000ViewDirection, Image>();

                            foreach (ImageCaptureViewDirectionEnum requestedDirection in AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable)
                            {
                                // Gets the first image in the list with this direction
                                NlOpt_Point_ScreenShot first = alreadyAcquiredScreenshots.First(a => a.Direction == requestedDirection);
                                resultScreenshots.Add(GetMatchingSap2000ViewDirection(requestedDirection), first.Image);
                            }
                        }
                        else // Acquire them from SAP
                        {
                            resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                                inCase: "DEAD",
                                inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow);
                        }
                    }
                        break;



                    case FeResultTypeEnum.SectionNode_Stress_S1:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "S11");
                        break;

                    case FeResultTypeEnum.SectionNode_Stress_S2:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "S12");
                        break;

                    case FeResultTypeEnum.SectionNode_Stress_S3:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "S13");
                        break;

                    case FeResultTypeEnum.SectionNode_Stress_SEqv:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "SVM");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_Fx:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Axial Force");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_SFy:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Shear 2-2");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_SFz:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Shear 3-3");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_Tq:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Torsion");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_My:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Moment 2-2");
                        break;

                    case FeResultTypeEnum.ElementNodal_Force_Mz:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_ForceStress(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "DEAD",
                            inForceStressName: "Moment 3-3");
                        break;


                    case FeResultTypeEnum.ElementNodal_CodeCheck:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_CodeCheck(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection));
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "EVBUCK",
                            inContour: "Resultant",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow, 
                            inStepOrMode: _bucklingModePositiveMatch[0]);
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "EVBUCK",
                            inContour: "Resultant",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow,
                            inStepOrMode: _bucklingModePositiveMatch[1]);
                        break;

                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                        resultScreenshots = S2KModel.SM.InterAuto.FlaUI_GetSapScreenShot_DeformedShape(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(GetMatchingSap2000ViewDirection),
                            inCase: "EVBUCK",
                            inContour: "Resultant",
                            inShowUndeformedShadow: AppSS.I.ScreenShotOpt.ImageCapture_UndeformedShadow,
                            inStepOrMode: _bucklingModePositiveMatch[2]);
                        break;

                    case FeResultTypeEnum.SectionNode_Stress_SInt:
                    case FeResultTypeEnum.Element_StrainEnergy:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT1:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT2:
                    case FeResultTypeEnum.SectionNode_Strain_EPTT3:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTInt:
                    case FeResultTypeEnum.SectionNode_Strain_EPTTEqv:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELDIR:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByT:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELByB:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzT:
                    case FeResultTypeEnum.ElementNodal_BendingStrain_EPELBzB:
                    case FeResultTypeEnum.ElementNodal_Strain_Ex:
                    case FeResultTypeEnum.ElementNodal_Strain_Ky:
                    case FeResultTypeEnum.ElementNodal_Strain_Kz:
                    case FeResultTypeEnum.ElementNodal_Strain_SEz:
                    case FeResultTypeEnum.ElementNodal_Strain_SEy:
                    case FeResultTypeEnum.ElementNodal_Strain_Te:
                    case FeResultTypeEnum.ElementNodal_Stress_SDir:
                    case FeResultTypeEnum.ElementNodal_Stress_SByT:
                    case FeResultTypeEnum.ElementNodal_Stress_SByB:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzT:
                    case FeResultTypeEnum.ElementNodal_Stress_SBzB:
                    default:
                        throw new ArgumentOutOfRangeException($"{feResult.ResultType} is not supported by SAP2000.");
                }

                // Adds the result's direction images to the list of images
                foreach (KeyValuePair<Sap2000ViewDirection, Image> image in resultScreenshots)
                {
                    ImageCaptureViewDirectionEnum dir = GetMatchingImageCaptureViewDirection(image.Key);
                    _model.Owner.ScreenShots.Add(new NlOpt_Point_ScreenShot(feResult, dir, image.Value));
                }
            }

        }
    }

}
