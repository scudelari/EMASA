extern alias r3dm;
using r3dm::Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        [NotNull] private readonly SolveManager _owner;
        public GhAlgorithm([NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            // In the constructor, we will build the input files
            RhinoModel.Initialize();

            // Gets the current GH file. Will throw if not available
            GrasshopperFullFileName = RhinoModel.RM.GrasshopperFullFileName;
            WpfGrasshopperFileDescription = RhinoModel.RM.GrasshopperDescription;

            #region Read the Grasshopper input list
            string[] inputVarFiles = Directory.GetFiles(GhInputDirPath);

            foreach (string varDataFile in inputVarFiles)
            {
                string varExtension = Path.GetExtension(varDataFile);
                string varName = Path.GetFileNameWithoutExtension(varDataFile);

                if (varExtension.EndsWith("Range")) continue; // Ignores the ranges as they are accounted together with their variable

                string varRangeFile = varDataFile + "Range";
                if (!File.Exists(varRangeFile)) throw new IOException($"The range file for GH input variable {varDataFile} could not be found.");

                switch (varExtension)
                {
                    case ".Double":
                        AddParameterToInputs(new Double_Input_ParamDef(inName: varName, inRange: ReadDoubleValueRange(varRangeFile))
                        { Start = ReadDouble(varDataFile) });
                        break;
                    case ".Point":
                        AddParameterToInputs(new Point_Input_ParamDef(inName: varName, inRange: ReadPointValueRange(varRangeFile))
                        { Start = ReadPoint(varDataFile) });
                        break;
                    default:
                        throw new InvalidDataException($"The format given by extension {varExtension} is not supported as GH input.");
                }
            }
            #endregion
            
            #region Read the Grasshopper output list
            string[] ghGeometryFiles = Directory.GetFiles(GhGeometryDirPath);

            foreach (string varDataFile in ghGeometryFiles)
            {
                string varExtension = Path.GetExtension(varDataFile);
                string varName = Path.GetFileNameWithoutExtension(varDataFile);

                switch (varExtension)
                {
                    case ".LineList":
                        GeometryDefs.Add(new LineList_GhGeom_ParamDef(varName));
                        break;

                    case ".DoubleList":
                        GeometryDefs.Add(new DoubleList_GhGeom_ParamDef(varName));
                        break;

                    case ".PointList":
                        GeometryDefs.Add(new PointList_GhGeom_ParamDef(varName));
                        break;

                    default:
                        throw new InvalidDataException($"The format given by extension {varExtension} is not supported as GH input.");
                }
            }
            #endregion

            #region Setting the WPF collection views

            InputDefs_View = CollectionViewSource.GetDefaultView(InputDefs);
            InputDefs_View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            InputDefs_View.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));


            CollectionViewSource geometryDefs_PointList_Cvs = new CollectionViewSource() {Source = GeometryDefs};
            geometryDefs_PointList_Cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is PointList_GhGeom_ParamDef item) inArgs.Accepted = true;
                else inArgs.Accepted = false;
            };
            geometryDefs_PointList_Cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            geometryDefs_PointList_Cvs.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_PointList_View = geometryDefs_PointList_Cvs.View;
            HasGeometryDef_PointList = GeometryDefs_PointList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;

            CollectionViewSource geometryDefs_LineList_Cvs = new CollectionViewSource() { Source = GeometryDefs };
            geometryDefs_LineList_Cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is LineList_GhGeom_ParamDef item) inArgs.Accepted = true;
                else inArgs.Accepted = false;
            };
            geometryDefs_LineList_Cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            geometryDefs_LineList_Cvs.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_LineList_View = geometryDefs_LineList_Cvs.View;
            HasGeometryDef_LineList = GeometryDefs_LineList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;

            CollectionViewSource geometryDefs_DoubleList_Cvs = new CollectionViewSource() { Source = GeometryDefs };
            geometryDefs_DoubleList_Cvs.Filter += (inSender, inArgs) =>
            {
                if (inArgs.Item is DoubleList_GhGeom_ParamDef item) inArgs.Accepted = true;
                else inArgs.Accepted = false;
            };
            geometryDefs_DoubleList_Cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            geometryDefs_DoubleList_Cvs.GroupDescriptions.Add(new PropertyGroupDescription("TypeName"));
            GeometryDefs_DoubleList_View = geometryDefs_DoubleList_Cvs.View;
            HasGeometryDef_DoubleList = GeometryDefs_DoubleList_View.IsEmpty ? Visibility.Collapsed : Visibility.Visible;


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
                        case Integer_Input_ParamDef ip:
                            lower.Add((double)ip.SearchRange.Range.Min);
                            break;
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
                        case Integer_Input_ParamDef ip:
                            upper.Add((double)ip.SearchRange.Range.Max);
                            break;
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
                center[i] = ((upper[i] + lower[i]) / 2d) + lower[i];
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

                    case Integer_Input_ParamDef integerInputParamDef:
                        userValList.Add((double)integerInputParamDef.Start);
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
                    case Integer_Input_ParamDef integer_Input_ParamDef:
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
        #endregion

        #region HELPER Grasshopper Send, Run, Read
        public void UpdateGrasshopperGeometry([NotNull] SolutionPoint inSolPoint)
        {
            if (inSolPoint == null) throw new ArgumentNullException(nameof(inSolPoint));

            Stopwatch sw = Stopwatch.StartNew();

            // Writes the data to the input files
            foreach (Input_ParamDefBase input_ParamDefBase in InputDefs)
            {
                string inputVarFilePath = Path.Combine(GhInputDirPath, $"{input_ParamDefBase.Name}.{input_ParamDefBase.TypeName}");
                try
                {
                    object solPointValue = inSolPoint.GhInput_Values[input_ParamDefBase];
                    File.WriteAllText(inputVarFilePath, solPointValue.ToString());
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not write the Grasshopper input {input_ParamDefBase.Name} to {inputVarFilePath}.", e);
                }
            }

            try
            {
                // Runs Grasshopper
                RhinoModel.RM.SolveGrasshopper();
            }
            catch (Exception e)
            {
                throw new COMException("Could not solve the Grasshopper Algorithm.", e);
            }


            // Reads the geometry into the solution point
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
        #endregion
    }
}
