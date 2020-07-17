extern alias r3dm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.Opt.ParamDefinitions;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace AccordHelper.Opt
{
    [Serializable]
    public class PossibleSolution : BindableBase
    {
        private int _functionHitCount = -1;
        public int FunctionHitCount
        {
            get => _functionHitCount;
            set => SetProperty(ref _functionHitCount, value);
        }
        private int _gradientHitCount = -1;
        public int GradientHitCount
        {
            get => _gradientHitCount;
            set => SetProperty(ref _gradientHitCount, value);
        }

        private readonly ObjectiveFunctionBase _objectiveFunction;
        private ObjectiveFunctionBase ObjectiveFunction
        {
            get => _objectiveFunction;
        }
        private ProblemBase Problem
        {
            get => _objectiveFunction.Problem;
        }
        public PossibleSolution(double[] inInputValuesAsDouble, ObjectiveFunctionBase inObjectiveFunction, FunctionOrGradientEval inFunctionOrGradientEval)
        {
            _objectiveFunction = inObjectiveFunction;

            // Fills the dictionaries
            foreach (Input_ParamDefBase inputParamDef in Problem.ObjectiveFunction.InputDefs)
            {
                InputValues.Add(inputParamDef, null);
            }
            foreach (Output_ParamDefBase intermediateParamDef in Problem.ObjectiveFunction.IntermediateDefs)
            {
                IntermediateValues.Add(intermediateParamDef, null);
            }
            foreach (Output_ParamDefBase outputParamDef in Problem.ObjectiveFunction.FinalDefs)
            {
                FinalValues.Add(outputParamDef, null);
            }

            InputValuesAsDouble = inInputValuesAsDouble; // The setter also fills the InputValues dictionary

            EvalType = inFunctionOrGradientEval;

            switch (EvalType)
            {
                case FunctionOrGradientEval.Function:
                    FunctionHitCount = inObjectiveFunction.FunctionHitCount;
                    break;

                case FunctionOrGradientEval.Gradient:
                    GradientHitCount = inObjectiveFunction.GradientHitCount;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inFunctionOrGradientEval), inFunctionOrGradientEval, null);
            }
        }

        private FunctionOrGradientEval _evalType;
        public FunctionOrGradientEval EvalType
        {
            get => _evalType;
            set => SetProperty(ref _evalType, value);
        }

        private double[] _inputValuesAsDouble;
        public double[] InputValuesAsDouble
        {
            get => _inputValuesAsDouble;
            set
            {
                _inputValuesAsDouble = value;

                // Updates the Input Dictionary
                int position = 0;
                foreach (Input_ParamDefBase inputParamDef in Problem.ObjectiveFunction.InputDefs)
                {
                    switch (inputParamDef)
                    {
                        case Double_Input_ParamDef doubleInputParamDef:
                            InputValues[inputParamDef] = _inputValuesAsDouble[position];
                            break;

                        case Integer_Input_ParamDef integerInputParamDef:
                            InputValues[inputParamDef] = (int)_inputValuesAsDouble[position];
                            break;

                        case Point_Input_ParamDef pointInputParamDef:
                            InputValues[inputParamDef] = new Point3d(_inputValuesAsDouble[position], _inputValuesAsDouble[position + 1], _inputValuesAsDouble[position + 2]);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(inputParamDef));
                    }
                    position += inputParamDef.VarCount;
                }
            }
        }

        private Dictionary<Input_ParamDefBase, object> _inputValues = new Dictionary<Input_ParamDefBase, object>();
        public Dictionary<Input_ParamDefBase, object> InputValues
        {
            get => _inputValues;
        }

        private Dictionary<Output_ParamDefBase, object> _intermediateValues = new Dictionary<Output_ParamDefBase, object>();
        public Dictionary<Output_ParamDefBase, object> IntermediateValues
        {
            get => _intermediateValues;
        }

        private Dictionary<Output_ParamDefBase, object> _finalValues = new Dictionary<Output_ParamDefBase, object>();
        public Dictionary<Output_ParamDefBase, object> FinalValues
        {
            get => _finalValues;
        }

        private double _eval;
        public double Eval
        {
            get => _eval;
            set => SetProperty(ref _eval, value);
        }

        public void WritePointToGrasshopper(string inInputFolder)
        {
            //// First, clears the folder
            //foreach (string file in Directory.GetFiles(inInputFolder))
            //{
            //    try
            //    {
            //        File.Delete(file);
            //    }
            //    catch (Exception e)
            //    {
            //        throw new Exception($"Could not delete the Grasshopper input file {Path.GetFileName(file)} in the directory {Path.GetDirectoryName(file)}", e);
            //    }
            //}

            // Writes the data to the files
            foreach (KeyValuePair<Input_ParamDefBase, object> inputValue in InputValues)
            {
                string fullFilePath = Path.Combine(inInputFolder, $"{inputValue.Key.Name}.{inputValue.Key.TypeName}");

                try
                {
                    File.WriteAllText(fullFilePath, inputValue.Value.ToString());
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not write the Grasshopper input file {Path.GetFileName(fullFilePath)} in the directory {Path.GetDirectoryName(fullFilePath)}", e);
                }
            }
        }
        public void ReadResultsFromGrasshopper(string inOutputFolder)
        {
            for (int i = 0; i < IntermediateValues.Count; i++)
            {
                Output_ParamDefBase key = IntermediateValues.ElementAt(i).Key;

                string fullFilePath = Path.Combine(inOutputFolder, $"{key.Name}.{key.TypeName}");

                string[] fileLines = null;
                try
                {
                    fileLines = File.ReadAllLines(fullFilePath);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not read the Grasshopper output file {Path.GetFileName(fullFilePath)} in the directory {Path.GetDirectoryName(fullFilePath)}", e);
                }

                switch (key)
                {
                    case DoubleList_Output_ParamDef doubleListOutputParamDef:
                        List<double> doubles = new List<double>();

                        foreach (string fileLine in fileLines)
                        {
                            if (!double.TryParse(fileLine, out double d1)) throw new Exception($"Could not parse {fileLine} to a Double.");
                            else doubles.Add(d1);
                        }

                        IntermediateValues[key] = doubles;
                        break;

                    case Double_Output_ParamDef doubleOutputParamDef:
                        
                        if (fileLines.Length > 1) throw new Exception($"Double output parameter supports only one double. Please use DoubleList if you intend to have a list of doubles.");
                        if (!double.TryParse(fileLines[0], out double d)) throw new Exception($"Could not parse {fileLines[0]} to a double.");

                        IntermediateValues[key] = d;


                        break;

                    case LineList_Output_ParamDef lineListOutputParamDef:
                        List<Line> lines = new List<Line>();

                        foreach (string fileLine in fileLines)
                        {
                            if (!RhinoStaticMethods.TryParseLine(fileLine, out Line line)) throw new Exception($"Could not parse {fileLine} to a Line.");
                            else lines.Add(line);
                        }

                        IntermediateValues[key] = lines;
                        break;

                    case PointList_Output_ParamDef pointListOutputParamDef:
                        List<Point3d> points = new List<Point3d>();

                        foreach (string fileLine in fileLines)
                        {
                            if (!RhinoStaticMethods.TryParsePoint3d(fileLine, out Point3d pnt)) throw new Exception($"Could not parse {fileLine} to a Point3d.");
                            else points.Add(pnt);
                        }

                        IntermediateValues[key] = points;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(key));
                }
            }
        }

        public T GetInputValueByName<T>(string inParamName)
        {
            object obj = InputValues.FirstOrDefault(a => a.Key.Name == inParamName).Value;
            if (obj == null) throw new Exception($"Could not find the input parameter called {inParamName}.");
            if (!(obj is T t)) throw new Exception($"Could not cast the input parameter called {inParamName} to type {typeof(T)}.");
            return t;
        }
        public T GetIntermediateValueByName<T>(string inParamName)
        {
            object obj = IntermediateValues.FirstOrDefault(a => a.Key.Name == inParamName).Value;
            if (obj == null) throw new Exception($"Could not find the output parameter called {inParamName}.");
            if (!(obj is T t)) throw new Exception($"Could not cast the output parameter called {inParamName} to type {typeof(T)}.");
            return t;
        }
        public T GetFinalValueByName<T>(string inParamName)
        {
            object obj = FinalValues.FirstOrDefault(a => a.Key.Name == inParamName).Value;
            if (obj == null) throw new Exception($"Could not find the solver result parameter called {inParamName}.");
            if (!(obj is T t)) throw new Exception($"Could not cast the solver result parameter called {inParamName} to type {typeof(T)}.");
            return t;
        }
        public (Output_ParamDefBase param, object val) SetFinalValueByName(string inParamName, object inValue)
        {
            Output_ParamDefBase param = FinalValues.Keys.FirstOrDefault(a => a.Name == inParamName);
            if (param == null) throw new Exception($"Could not find the solver result parameter called {inParamName}.");
            FinalValues[param] = inValue;
            return (param, inValue);
        }

        public double GetSquareSumOfList(IEnumerable<(Output_ParamDefBase param, object val)> inParamAndVals)
        {
            double SquareSum = 0d;

            foreach ((Output_ParamDefBase param, object val) paramVal in inParamAndVals)
            {
                if (paramVal.param is Double_Output_ParamDef param && paramVal.val is double val)
                {
                    double forSquareSum = param.GetValueForSquareSum(val);
                    SquareSum += forSquareSum * forSquareSum;
                }
                else throw new Exception($"The type of the parameter {paramVal.param} is not supported or the value {paramVal.val} is not of the expected type.");
            }

            return SquareSum;
        }
        public double GetFinalValueOfParameterToSquareSum(Output_ParamDefBase inParam)
        {
            if (inParam is Double_Output_ParamDef dOutParamDef)
            {
                return dOutParamDef.GetValueForSquareSum((double)FinalValues[inParam]);
            }

            throw new ArgumentOutOfRangeException();
        }
        public double GetFinalValueOfParameterToSquareSum(string inParamName)
        {
            KeyValuePair<Output_ParamDefBase, object> outparam_kvp;
            try
            {
                outparam_kvp = FinalValues.FirstOrDefault(a => a.Key.Name == "inParamName");
            }
            catch (Exception)
            {
                throw new Exception($"Could not find the Final parameter called {inParamName}.");
            }

            return GetFinalValueOfParameterToSquareSum(outparam_kvp.Key);
        }

        public string FriendlyName
        {
            get
            {
                return $"{FunctionHitCount,-8} : {Eval,-10:F4}";
            }
        }

        public string FriendlyReport
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{(char)218}---------------------------------------{(char)191}");
                sb.AppendLine($"|          GRASSHOPPER INPUTS           |");
                sb.AppendLine($"{(char)192}---------------------------------------{(char)217}");
                sb.AppendLine(FriendlyInputReport);
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine($"{(char)218}---------------------------------------{(char)191}");
                sb.AppendLine($"|          GRASSHOPPER OUTPUTS          |");
                sb.AppendLine($"{(char)192}---------------------------------------{(char)217}");
                sb.AppendLine(FriendlyGHOutputs);
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine($"{(char)218}---------------------------------------{(char)191}");
                sb.AppendLine($"|            SOLVER RESULTS             |");
                sb.AppendLine($"{(char)192}---------------------------------------{(char)217}");
                sb.AppendLine(FriendlySolverResults);
                return sb.ToString();
            }
        }
        public string FriendlyInputReport
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{"Variable Id",-20} {"Value",-20}");
                sb.AppendLine($"-------------------- --------------------");

                foreach (KeyValuePair<Input_ParamDefBase, object> kvpair in InputValues)
                { 
                    AddParamDefValueKVP(sb, new KeyValuePair<ParamDefBase, object>(kvpair.Key,kvpair.Value));
                }

                return sb.ToString();
            }
        }
        public string FriendlyGHOutputs
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{"Variable Id",-20} {"Value",-20}");
                sb.AppendLine($"-------------------- --------------------");

                foreach (KeyValuePair<Output_ParamDefBase, object> kvpair in IntermediateValues)
                {
                    AddParamDefValueKVP(sb, new KeyValuePair<ParamDefBase, object>(kvpair.Key, kvpair.Value));
                }

                return sb.ToString();
            }
        }
        public string FriendlySolverResults
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{"Variable Id",-20} {"Value",-20}");
                sb.AppendLine($"-------------------- --------------------");

                foreach (KeyValuePair<Output_ParamDefBase, object> kvpair in FinalValues)
                {
                    AddParamDefValueKVP(sb, new KeyValuePair<ParamDefBase, object>(kvpair.Key, kvpair.Value));
                }

                return sb.ToString();
            }
        }
        private static void AddParamDefValueKVP(StringBuilder inSB, KeyValuePair<ParamDefBase, object> inKVP)
        {
            switch (inKVP.Key)
            {
                case Double_Output_ParamDef doubleOutputParamDefBase:
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {(double)inKVP.Value,-20:E3}");
                    break;

                case Double_Input_ParamDef doubleInputParamDefBase:
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {(double)inKVP.Value,-20:E3}");
                    break;

                case Integer_Input_ParamDef integerInputParamDefBase:
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {(int)inKVP.Value,-20}");
                    break;

                case LineList_Output_ParamDef lineListOutputParamDefBase:
                    List<Line> lineList = (List<Line>)inKVP.Value;
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {LineToDisplayString(lineList[0], "E3"),-20}");
                    for (int i = 1; i < lineList.Count; i++)
                    {
                        inSB.AppendLine($"{"",-20} {LineToDisplayString(lineList[i], "E3"),-20}");
                    }
                    break;

                case Point_Input_ParamDef pointInputParamDefBase:
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {Point3dToDisplayString((Point3d)inKVP.Value, "E3"),-20}");
                    break;

                case PointList_Output_ParamDef pointListOutputParamDefBase:
                    List<Point3d> pointList = (List<Point3d>)inKVP.Value;
                    inSB.AppendLine($"{inKVP.Key.Name,-20} {Point3dToDisplayString(pointList[0], "E3"),-20}");
                    for (int i = 1; i < pointList.Count; i++)
                    {
                        inSB.AppendLine($"{"",-20} {Point3dToDisplayString(pointList[i], "E3"),-20}");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inKVP.Key));
            }
        }

        private static string Point3dToDisplayString(Point3d inPoint, string inNumberFormat)
        {
            return $"{inPoint.X.ToString(inNumberFormat)},{inPoint.Y.ToString(inNumberFormat)},{inPoint.Z.ToString(inNumberFormat)}";
        }
        private static string LineToDisplayString(Line inLine, string inNumberFormat)
        {
            return $"{inLine.FromX.ToString(inNumberFormat)},{inLine.FromY.ToString(inNumberFormat)},{inLine.FromZ.ToString(inNumberFormat)} {(char) 187} {inLine.ToX.ToString(inNumberFormat)},{inLine.ToY.ToString(inNumberFormat)},{inLine.ToZ.ToString(inNumberFormat)}";
        }
    }

    public enum FunctionOrGradientEval
    {
        Function,
        Gradient
    }
}
