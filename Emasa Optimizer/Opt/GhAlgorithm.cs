extern alias r3dm;
using r3dm::Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using MathNet.Numerics.Random;
using Prism.Mvvm;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Opt
{
    public class GhAlgorithm : BindableBase
    {
        public GhAlgorithm()
        {
            // In the constructor, we will build the input files
            RhinoModel.Initialize();

            // Gets the current GH file. Will throw if not available
            GrasshopperFullFileName = RhinoModel.RM.GrasshopperFullFileName;
            WpfGrasshopperFileDescription = RhinoModel.RM.GrasshopperDescription;

            #region Gets the Grasshopper Input List
            GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm inputDefs = RhinoModel.RM.Grasshopper_GetAllEmasaInputDefs();

            // Integers
            foreach (string integerInput in inputDefs.IntegerInputs)
            {
                Integer_GhConfig_ParamDef intParam = new Integer_GhConfig_ParamDef(inName: integerInput);
                ConfigDefs.Add(intParam);
            }

            // Doubles
            foreach (var doubleInput in inputDefs.DoubleInputs)
            {
                Double_Input_ParamDef bdlInputParam = new Double_Input_ParamDef(inName: doubleInput.Key, inRange: new DoubleValueRange(doubleInput.Value.Item2, doubleInput.Value.Item3));
                bdlInputParam.Start = doubleInput.Value.Item1;

                AddParameterToInputs(bdlInputParam);
            }

            // Points
            foreach (var pointInputs in inputDefs.PointInputs)
            {
                Point_Input_ParamDef pntParam = new Point_Input_ParamDef(inName: pointInputs.Key, inRange: new PointValueRange(pointInputs.Value.Item2, pointInputs.Value.Item3));
                pntParam.Start = pointInputs.Value.Item1;

                AddParameterToInputs(pntParam);
                break;
            }
            #endregion

            #region Gets the Grasshopper Output List
            GrasshopperAllEmasaOutputWrapper_AsRhino3dm outputDefs = RhinoModel.RM.Grasshopper_GetAllEmasaOutputs();

            // Line Lists
            foreach (var lineList in outputDefs.LineLists)
            {
                GeometryDefs.Add(new LineList_GhGeom_ParamDef(lineList.Key));
            }

            // Double Lists
            foreach (var doubleList in outputDefs.DoubleLists)
            {
                DoubleList_GhGeom_ParamDef dlParam = new DoubleList_GhGeom_ParamDef(doubleList.Key);
                GeometryDefs.Add(dlParam);
            }

            // Point Lists
            foreach (var pointList in outputDefs.PointLists)
            {
                GeometryDefs.Add(new PointList_GhGeom_ParamDef(pointList.Key));
            }
            #endregion

            #region Setting the WPF collection views

            InputDefs_View = CollectionViewSource.GetDefaultView(InputDefs);
            InputDefs_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            InputDefs_View.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));

            
            ConfigDefs_View = CollectionViewSource.GetDefaultView(ConfigDefs);
            ConfigDefs_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));


            CollectionViewSource configDefs_Integer_Cvs = new CollectionViewSource() {Source = ConfigDefs};
            ConfigDefs_Integer_View = configDefs_Integer_Cvs.View;
            ConfigDefs_Integer_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            ConfigDefs_Integer_View.Filter += inO => inO is Integer_GhConfig_ParamDef;
            HasConfigDefs_Integer = ConfigDefs_Integer_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;


            CollectionViewSource geometryDefs_PointList_Cvs = new CollectionViewSource() {Source = GeometryDefs};
            GeometryDefs_PointList_View = geometryDefs_PointList_Cvs.View;
            GeometryDefs_PointList_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            //GeometryDefs_PointList_View.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_PointList_View.Filter += inO => inO is PointList_GhGeom_ParamDef;
            HasGeometryDef_PointList = GeometryDefs_PointList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;


            GeometryDefs_LineList_View = (new CollectionViewSource() { Source = GeometryDefs }).View;
            GeometryDefs_LineList_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            //GeometryDefs_LineList_View.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_LineList_View.Filter += inO => inO is LineList_GhGeom_ParamDef;
            HasGeometryDef_LineList = GeometryDefs_LineList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;


            GeometryDefs_DoubleList_View = (new CollectionViewSource() { Source = GeometryDefs }).View;
            GeometryDefs_DoubleList_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            //GeometryDefs_DoubleList_View.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_DoubleList_View.Filter += inO => inO is DoubleList_GhGeom_ParamDef;
            HasGeometryDef_DoubleList = GeometryDefs_DoubleList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;


            GeometryDefs_PointLineListBundle_View = (new CollectionViewSource() { Source = GeometryDefs }).View;
            GeometryDefs_PointLineListBundle_View.SortDescriptions.Add(new SortDescription("TypeName", ListSortDirection.Ascending));
            GeometryDefs_PointLineListBundle_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            GeometryDefs_PointLineListBundle_View.Filter += inO => inO is LineList_GhGeom_ParamDef || inO is PointList_GhGeom_ParamDef;
            #endregion
        }

        public string GrasshopperFullFileName { get; private set; }
        public string WpfGrasshopperFileDescription { get; private set; }
        public string GhDataDirPath => GrasshopperFullFileName + "_data";
        public string GhInputDirPath => Path.Combine(GhDataDirPath, "Input");
        public string GhGeometryDirPath => Path.Combine(GhDataDirPath, "Output");

        #region Parameters Defined in Grasshopper
        public FastObservableCollection<Input_ParamDefBase> InputDefs { get; } = new FastObservableCollection<Input_ParamDefBase>();
        public void AddParameterToInputs(Input_ParamDefBase inParam)
        {
            inParam.IndexInDoubleArray = InputDefs.Sum(a => a.VarCount);
            InputDefs.Add(inParam);
        }

        public string GetInputParameterNameByIndex(int inIndex)
        {
            if (inIndex < 0 || inIndex > InputDefs_VarCount) throw new ArgumentOutOfRangeException(nameof(inIndex), inIndex, $"Index must be between 0 and number of variables {InputDefs_VarCount}.");

            int pos = 0;

            foreach (Input_ParamDefBase inParam in InputDefs)
            {
                switch (inParam)
                {
                    case Double_Input_ParamDef double_Input_ParamDef:
                        if (pos == inIndex) return double_Input_ParamDef.Name;
                        pos++;
                        break;

                    case Point_Input_ParamDef point_Input_ParamDef:
                        if (pos == inIndex) return point_Input_ParamDef.Name + " - X";
                        pos++;
                        if (pos == inIndex) return point_Input_ParamDef.Name + " - Y";
                        pos++;
                        if (pos == inIndex) return point_Input_ParamDef.Name + " - Z";
                        pos++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inParam));
                }
            }

            throw new Exception($"Could not get input parameter at index {inIndex}.");
        }
        
        private int? _inputDefs_VarCount;
        public int InputDefs_VarCount
        {
            get
            {
                if (_inputDefs_VarCount.HasValue) return _inputDefs_VarCount.Value;
                _inputDefs_VarCount = InputDefs.Sum(a => a.VarCount);
                return _inputDefs_VarCount.Value;
            }
        }

        public double[] InputDefs_LowerBounds
        {
            get
            {
                List<double> lower = new List<double>();
                foreach (Input_ParamDefBase input in InputDefs)
                {
                    switch (input)
                    {
                        case Double_Input_ParamDef dp:
                            lower.Add(dp.SearchRange.Range.Min);
                            break;

                        case Point_Input_ParamDef pp:
                            lower.Add(pp.SearchRange.RangeX.Min);
                            lower.Add(pp.SearchRange.RangeY.Min);
                            lower.Add(pp.SearchRange.RangeZ.Min);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (lower.Count > 0) return lower.ToArray();

                return null;
            }
        }
        public double[] InputDefs_UpperBounds
        {
            get
            {
                List<double> upper = new List<double>();
                foreach (Input_ParamDefBase input in InputDefs)
                {
                    switch (input)
                    {
                        case Double_Input_ParamDef dp:
                            upper.Add(dp.SearchRange.Range.Max);
                            break;

                        case Point_Input_ParamDef pp:
                            upper.Add(pp.SearchRange.RangeX.Max);
                            upper.Add(pp.SearchRange.RangeY.Max);
                            upper.Add(pp.SearchRange.RangeZ.Max);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (upper.Count > 0) return upper.ToArray();

                return null;
            }
        }

        public double[] GetInputStartPosition()
        {
            // Gets the required variables for each type
            double[] lower = InputDefs_LowerBounds;
            double[] upper = InputDefs_UpperBounds;

            // Gets the center of the ranges
            double[] center = new double[lower.Length];
            for (int i = 0; i < center.Length; i++)
            {
                center[i] = ((upper[i] - lower[i]) / 2d) + lower[i];
            }

            // Gets the user values
            List<double> userValList = new List<double>();
            foreach (Input_ParamDefBase inputDef in InputDefs)
            {
                switch (inputDef)
                {
                    case Double_Input_ParamDef doubleInputParamDef:
                        userValList.Add(doubleInputParamDef.Start);
                        break;

                    case Point_Input_ParamDef pointInputParamDef:
                        Point3d pnt = pointInputParamDef.Start;
                        userValList.Add(pnt.X);
                        userValList.Add(pnt.Y);
                        userValList.Add(pnt.Z);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputDef));
                }
            }
            double[] userValues = userValList.ToArray();

            // The user vector to return
            double[] startVector = new double[lower.Length];

            // Start type is input based
            Random randGen = new Random();

            // Local function that gets the value at the position
            void lf_SetValue(Input_ParamDefBase inInputDef, int inPos)
            {
                switch (inInputDef.StartPositionType)
                {
                    case StartPositionTypeEnum.Given:
                        {
                            startVector[inPos] = userValList[inPos];
                            break;
                        }

                    case StartPositionTypeEnum.PercentRandomFromCenter:
                        {
                            double c = center[inPos];

                            double r = randGen.NextDouble();

                            startVector[inPos] = r * (inInputDef.StartPositionPercent - (-inInputDef.StartPositionPercent)) + (-inInputDef.StartPositionPercent);
                            startVector[inPos] = center[inPos] + (startVector[inPos] * center[inPos]);

                            if (startVector[inPos] > upper[inPos]) startVector[inPos] = upper[inPos];
                            if (startVector[inPos] < lower[inPos]) startVector[inPos] = lower[inPos];
                            break;
                        }

                    case StartPositionTypeEnum.CenterOfRange:
                        startVector[inPos] = center[inPos];
                        break;

                    case StartPositionTypeEnum.Random:
                        {
                            double r = randGen.NextDouble();

                            startVector[inPos] = r * (upper[inPos] - lower[inPos]) + lower[inPos];

                            if (startVector[inPos] > upper[inPos]) startVector[inPos] = upper[inPos];
                            if (startVector[inPos] < lower[inPos]) startVector[inPos] = lower[inPos];

                            break;
                        }

                    case StartPositionTypeEnum.PercentRandomFromGiven:
                        {
                            double r = randGen.NextDouble();

                            startVector[inPos] = r * (inInputDef.StartPositionPercent - (-inInputDef.StartPositionPercent)) + (-inInputDef.StartPositionPercent);
                            startVector[inPos] = userValues[inPos] + (startVector[inPos] * userValues[inPos]);


                            if (startVector[inPos] > upper[inPos]) startVector[inPos] = upper[inPos];
                            if (startVector[inPos] < lower[inPos]) startVector[inPos] = lower[inPos];

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inInputDef.StartPositionType), inInputDef.StartPositionType, null);
                }
            };

            foreach (Input_ParamDefBase inputDef in InputDefs)
            {
                int pos = inputDef.IndexInDoubleArray;
                switch (inputDef)
                {
                    case Double_Input_ParamDef double_Input_ParamDef:
                        lf_SetValue(inputDef, pos);
                        break;

                    case Point_Input_ParamDef point_Input_ParamDef:
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            lf_SetValue(inputDef, pos + k);
                        }
                    }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputDef));
                }
            }

            return startVector;
        }
        
        public FastObservableCollection<GhGeom_ParamDefBase> GeometryDefs { get; } = new FastObservableCollection<GhGeom_ParamDefBase>();

        public FastObservableCollection<GhConfig_ParamDefBase> ConfigDefs { get; } = new FastObservableCollection<GhConfig_ParamDefBase>();
        #endregion

        #region HELPER Grasshopper Send, Run, Read
        public void UpdateGrasshopperGeometry([NotNull] NlOpt_Point inSolPoint)
        {
            if (inSolPoint == null) throw new ArgumentNullException(nameof(inSolPoint));

            // Data that will be sent to Grasshopper
            Dictionary<string, object> inValuePairs = new Dictionary<string, object>();

            // Writes the data to the config files
            foreach (Integer_GhConfig_ParamDef intGhConfig in ConfigDefs.OfType<Integer_GhConfig_ParamDef>())
            {
                inValuePairs.Add(intGhConfig.Name, inSolPoint.Owner.GetGhIntegerConfig(intGhConfig));
                //string inputVarFilePath = Path.Combine(GhInputDirPath, $"{intGhConfig.Name}.{intGhConfig.TypeName}");

                //try
                //{
                //    // Finds the configuration value

                //    //object configValue = AppSS.I.SolveMgr.CurrentCalculatingProblemConfig.GetGhIntegerConfig(intGhConfig);
                //    object configValue = inSolPoint.Owner.GetGhIntegerConfig(intGhConfig);
                //    File.WriteAllText(inputVarFilePath, configValue.ToString());
                //}
                //catch (Exception e)
                //{
                //    throw new Exception($"Could not write the Grasshopper configuration input {intGhConfig.Name} to {inputVarFilePath}.", e);
                //}
            }

            // Writes the data to the input files
            foreach (Input_ParamDefBase input_ParamDefBase in InputDefs)
            {
                inValuePairs.Add(input_ParamDefBase.Name, inSolPoint.GhInput_Values[input_ParamDefBase]);
                //string inputVarFilePath = Path.Combine(GhInputDirPath, $"{input_ParamDefBase.Name}.{input_ParamDefBase.TypeName}");
                //try
                //{
                //    object solPointValue = inSolPoint.GhInput_Values[input_ParamDefBase];
                //    File.WriteAllText(inputVarFilePath, solPointValue.ToString());
                //}
                //catch (Exception e)
                //{
                //    throw new Exception($"Could not write the Grasshopper input {input_ParamDefBase.Name} to {inputVarFilePath}.", e);
                //}
            }

            try
            {
                // Updates the input and Runs Grasshopper
                //RhinoModel.RM.SolveGrasshopper();
                RhinoModel.RM.Grasshopper_UpdateEmasaInputs(inValuePairs, true);
            }
            catch (Exception e)
            {
                throw new COMException("Could not update the input parameters and solve the Grasshopper Algorithm. Error in the COM interface.", e);
            }

            // Checks if the Grasshopper result has anything to say
            string[,] ghMessages = RhinoModel.RM.Grasshopper_GetDocumentMessages();
            if (ghMessages != null)
            {
                string errors = string.Empty;

                // Adds the message to the buffer
                for (int i = 0; i < ghMessages.GetLength(0); i++)
                {
                    switch (ghMessages[i, 0])
                    {
                        case "Error":
                            errors += $"{ghMessages[i, 1]}{Environment.NewLine}";
                            break;

                        case "Warning":
                            inSolPoint.RuntimeWarningMessages.Add(new NlOpt_Point_Message(ghMessages[i, 1], NlOpt_Point_MessageSourceEnum.Grasshopper, NlOpt_Point_MessageLevelEnum.Warning));
                            break;

                        case "Remark":
                            inSolPoint.RuntimeWarningMessages.Add(new NlOpt_Point_Message(ghMessages[i, 1], NlOpt_Point_MessageSourceEnum.Grasshopper, NlOpt_Point_MessageLevelEnum.Remark));
                            break;

                        default:
                            errors += $"A grasshopper message type was unexpected: {ghMessages[i, 0]}. Message: {ghMessages[i, 1]}{Environment.NewLine}";
                            break;
                    }
                }

                // There was an error, so we must throw
                if (!string.IsNullOrWhiteSpace(errors)) throw new Exception($"Grasshopper errors. {Environment.NewLine}{errors}");
            }
        }
        public void UpdateGrasshopperGeometryAndGetResults([NotNull] NlOpt_Point inSolPoint)
        {
            if (inSolPoint == null) throw new ArgumentNullException(nameof(inSolPoint));

            Stopwatch sw = Stopwatch.StartNew();

            // Writes the data in the files and updated the Grasshopper Geometry
            UpdateGrasshopperGeometry(inSolPoint);

            // Reads the geometry into the solution point
            GrasshopperAllEmasaOutputWrapper_AsRhino3dm outputDefs = RhinoModel.RM.Grasshopper_GetAllEmasaOutputs();
            foreach (GhGeom_ParamDefBase output_ParamDefBase in GeometryDefs)
            {
                try
                {
                    switch (output_ParamDefBase)
                    {
                        case DoubleList_GhGeom_ParamDef doubleList_Output_ParamDef:
                            List<double> doubles = outputDefs.DoubleLists[doubleList_Output_ParamDef.Name];
                            inSolPoint.GhGeom_Values[output_ParamDefBase] = doubles;
                            break;

                        case LineList_GhGeom_ParamDef lineList_Output_ParamDef:
                            List<Line> lines = outputDefs.LineLists[lineList_Output_ParamDef.Name];
                            inSolPoint.GhGeom_Values[output_ParamDefBase] = lines;
                            break;

                        case PointList_GhGeom_ParamDef pointList_Output_ParamDef:
                            List<Point3d> points = outputDefs.PointLists[pointList_Output_ParamDef.Name];
                            inSolPoint.GhGeom_Values[output_ParamDefBase] = points;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(output_ParamDefBase));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not read the Grasshopper geometry {output_ParamDefBase.Name}.", e);
                }
            }

            /*
            foreach (GhGeom_ParamDefBase output_ParamDefBase in GeometryDefs)
            {
                string geometryVarFilePath = Path.Combine(GhGeometryDirPath, $"{output_ParamDefBase.Name}.{output_ParamDefBase.TypeName}");

                try
                {
                    string[] fileLines = File.ReadAllLines(geometryVarFilePath);
                    switch (output_ParamDefBase)
                    {
                        case DoubleList_GhGeom_ParamDef doubleList_Output_ParamDef:
                            List<double> doubles = fileLines.Select(Convert.ToDouble).ToList();

                            inSolPoint.GhGeom_Values[output_ParamDefBase] = doubles;
                            break;

                        case LineList_GhGeom_ParamDef lineList_Output_ParamDef:
                            List<Line> lines = fileLines.Select(a =>
                            {
                                if (!RhinoStaticMethods.TryParseLine(a, out Line l)) throw new InvalidCastException($"Could not convert {a} to a Line.");
                                return l;
                            }).ToList();

                            inSolPoint.GhGeom_Values[output_ParamDefBase] = lines;
                            break;

                        case PointList_GhGeom_ParamDef pointList_Output_ParamDef:
                            List<Point3d> points = fileLines.Select(a =>
                            {
                                if (!RhinoStaticMethods.TryParsePoint3d(a, out Point3d p)) throw new InvalidCastException($"Could not convert {a} to a Point3d.");
                                return p;
                            }).ToList();

                            inSolPoint.GhGeom_Values[output_ParamDefBase] = points;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(output_ParamDefBase));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not read the Grasshopper geometry {output_ParamDefBase.Name}.", e);
                }
            }
            */

            // Obtains the Rhino Screenshots
            try
            {
                List<(string dir, Image image)> rhinoScreenshots = RhinoModel.RM.GetScreenshots(AppSS.I.ScreenShotOpt.ImageCapture_ViewDirectionsEnumerable.Select(inEnum => inEnum.ToString()).ToArray());
                foreach ((string dir, Image image) rhinoScreenshot in rhinoScreenshots)
                {
                    if (!Enum.TryParse(rhinoScreenshot.dir, out ImageCaptureViewDirectionEnum dirEnum)) throw new Exception($"Could not get back the direction enumerate value from its string representation. {rhinoScreenshot.dir}.");
                    
                    inSolPoint.ScreenShots.Add(new NlOpt_Point_ScreenShot(
                        AppSS.I.ScreenShotOpt.SpecialRhinoDisplayScreenshotInstance,
                        dirEnum, 
                        rhinoScreenshot.image));
                }
            }
            catch (Exception e)
            {
                throw new COMException("Could not get the RhinoScreenshots.", e);
            }

            sw.Stop();
            inSolPoint.GhUpdateTimeSpan = sw.Elapsed;
        }
        #endregion

        #region HELPER File Handling Functions
        private double ReadDouble(string inFileName)
        {
            try
            {
                return Convert.ToDouble(File.ReadAllText(inFileName));
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }
        private List<double> ReadDoubles(string inFileName)
        {
            try
            {
                string[] fileLines = File.ReadAllLines(inFileName);
                return fileLines.Select(Convert.ToDouble).ToList();
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }
        private DoubleValueRange ReadDoubleValueRange(string inFileName)
        {
            try
            {
                string[] fileLines = File.ReadAllLines(inFileName);
                if (fileLines.Length != 2) throw new Exception();
                return new DoubleValueRange(Convert.ToDouble(fileLines[0]), Convert.ToDouble(fileLines[1]));
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }

        private int ReadInteger(string inFileName)
        {
            try
            {
                return Convert.ToInt32(File.ReadAllText(inFileName));
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }

        private Point3d ReadPoint(string inFileName)
        {
            try
            {
                if (!RhinoStaticMethods.TryParsePoint3d(File.ReadAllText(inFileName), out Point3d p)) throw new Exception();
                return p;
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }
        private List<Point3d> ReadPoints(string inFileName)
        {
            try
            {
                string[] fileLines = File.ReadAllLines(inFileName);
                return fileLines.Select(a =>
                {
                    if (!RhinoStaticMethods.TryParsePoint3d(a, out Point3d p)) throw new Exception();
                    return p;
                }).ToList();
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }
        private PointValueRange ReadPointValueRange(string inFileName)
        {
            try
            {
                string[] fileLines = File.ReadAllLines(inFileName);
                if (fileLines.Length != 2) throw new Exception();

                if (!RhinoStaticMethods.TryParsePoint3d(fileLines[0], out Point3d min)) throw new Exception();
                if (!RhinoStaticMethods.TryParsePoint3d(fileLines[1], out Point3d max)) throw new Exception();

                return new PointValueRange(min, max);
            }
            catch (Exception e)
            {
                throw new IOException($"{MethodBase.GetCurrentMethod().Name}: Could not read from file {inFileName}.", e);
            }
        }
        #endregion


        #region Wpf
        public string WpfGrasshopperName => Path.GetFileNameWithoutExtension(GrasshopperFullFileName); 

        public ICollectionView InputDefs_View { get; }

        public ICollectionView GeometryDefs_PointList_View { get; private set; }
        public Visibility HasGeometryDef_PointList { get; private set; }

        public ICollectionView GeometryDefs_LineList_View { get; private set; }
        public Visibility HasGeometryDef_LineList { get; private set; }

        public ICollectionView GeometryDefs_DoubleList_View { get; private set; }
        public Visibility HasGeometryDef_DoubleList { get; private set; }

        public ICollectionView GeometryDefs_PointLineListBundle_View { get; private set; }

        public ICollectionView ConfigDefs_View { get; }

        public ICollectionView ConfigDefs_Integer_View { get; }
        public Visibility HasConfigDefs_Integer { get; private set; }
        #endregion
    }
}
