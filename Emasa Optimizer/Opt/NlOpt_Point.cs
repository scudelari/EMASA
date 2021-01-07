extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BaseWPFLibrary;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;


namespace Emasa_Optimizer.Opt
{
    public class NlOpt_Point : BindableBase, IEquatable<NlOpt_Point>
    {
        [NotNull] private readonly ProblemConfig _owner;
        public ProblemConfig Owner => _owner;
        public NlOpt_Point([NotNull] ProblemConfig inOwner, double[] inInput, NlOpt_Point_CalcTypeEnum inNlOptPointCalcType)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            Phase = NlOpt_Point_PhaseEnum.Initializing;

            #region Grasshopper Inputs and Geometry
            // Sets and initializes the Gh related dictionaries
            GhInput_Values = new Dictionary<Input_ParamDefBase, object>();
            foreach (Input_ParamDefBase ghAlgInputDef in AppSS.I.Gh_Alg.InputDefs)
            {
                GhInput_Values.Add(ghAlgInputDef, null);
            }

            GhGeom_Values = new Dictionary<GhGeom_ParamDefBase, object>();
            foreach (GhGeom_ParamDefBase ghAlgGeometryDef in AppSS.I.Gh_Alg.GeometryDefs)
            {
                GhGeom_Values.Add(ghAlgGeometryDef, null);
            }
            #endregion
            
            InputValuesAsDoubleArray = (double[])inInput.Clone();
            NlOptPointCalcType = inNlOptPointCalcType;

            #region Wpf Communication
            AppSS.I.ScreenShotOpt.PropertyChanged += OtherElements_PropertyChanged;
            AppSS.I.FeOpt.PropertyChanged += OtherElements_PropertyChanged;
            #endregion
        }

        #region GH values in this solution point
        public Dictionary<Input_ParamDefBase, object> GhInput_Values { get; private set; }
        public Dictionary<GhGeom_ParamDefBase, object> GhGeom_Values { get; private set; }
        #endregion

        public NlOpt_Point_CalcTypeEnum NlOptPointCalcType { get; set; }

        private double[] _inputValuesAsDoubleArray;
        public double[] InputValuesAsDoubleArray
        {
            get => _inputValuesAsDoubleArray;
            private set
            {
                _inputValuesAsDoubleArray = value;

                // Updates the Input Dictionary
                int position = 0;

                List<Input_ParamDefBase> ghInput_Values_TmpKeys = new List<Input_ParamDefBase>(GhInput_Values.Keys);
                foreach (Input_ParamDefBase inputParamDef in ghInput_Values_TmpKeys)
                {
                    switch (inputParamDef)
                    {
                        case Double_Input_ParamDef doubleInputParamDef:
                            GhInput_Values[inputParamDef] = _inputValuesAsDoubleArray[position];
                            break;

                        case Point_Input_ParamDef pointInputParamDef:
                            GhInput_Values[inputParamDef] = new Point3d(_inputValuesAsDoubleArray[position], _inputValuesAsDoubleArray[position + 1], _inputValuesAsDoubleArray[position + 2]);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(inputParamDef));
                    }
                    position += inputParamDef.VarCount;
                }
            }
        }
        
        private int _pointIndex;
        public int PointIndex
        {
            get => _pointIndex;
            set => SetProperty(ref _pointIndex, value);
        }

        public readonly string InputTableName = $"Input Value Table";
        public DataView InputDataAsDataView
        {
            get
            {
                if (!OutputDataSet.Tables.Contains(InputTableName))
                {
                    // Creates the input list as a table
                    DataTable table = new DataTable(InputTableName);

                    table.Columns.Add("Index", typeof(int));
                    table.Columns.Add("Parameter", typeof(string));
                    table.Columns.Add("Value", typeof(double));

                    int position = 0;
                    foreach (KeyValuePair<Input_ParamDefBase, object> inputParam in GhInput_Values)
                    {
                        DataRow newRow = null;

                        switch (inputParam.Key)
                        {
                            case Double_Input_ParamDef doubleInputParamDef:
                                newRow = table.NewRow();
                                newRow["Index"] = position++;
                                newRow["Parameter"] = doubleInputParamDef.Name;
                                newRow["Value"] = (double)inputParam.Value;
                                table.Rows.Add(newRow);
                                break;

                            case Point_Input_ParamDef pointInputParamDef:
                                Point3d p = (Point3d)inputParam.Value;

                                newRow = table.NewRow();
                                newRow["Index"] = position++;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - X";
                                newRow["Value"] = p.X;
                                table.Rows.Add(newRow);

                                newRow = table.NewRow();
                                newRow["Index"] = position++;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - Y";
                                newRow["Value"] = p.Y;
                                table.Rows.Add(newRow);

                                newRow = table.NewRow();
                                newRow["Index"] = position++;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - Z";
                                newRow["Value"] = p.Z;
                                table.Rows.Add(newRow);

                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(inputParam.Key));
                        }
                        //position += inputParam.Key.VarCount;
                    }

                    // Saves in the OutputDataSet
                    OutputDataSet.Tables.Add(table);
                }

                // Returns the table as a DataView
                return new DataView(OutputDataSet.Tables[InputTableName]);
            }
        }
        
        private List<PointNlParameterInputData> _inputDataAsList;
        public List<PointNlParameterInputData> InputDataAsList
        {
            get
            {
                if (_inputDataAsList == null)
                {
                    List<PointNlParameterInputData> tmpList = new List<PointNlParameterInputData>();

                    int position = 0;
                    foreach (KeyValuePair<Input_ParamDefBase, object> inputParam in GhInput_Values)
                    {
                        DataRow newRow = null;

                        switch (inputParam.Key)
                        {
                            case Double_Input_ParamDef doubleInputParamDef:
                                tmpList.Add(new PointNlParameterInputData()
                                    {
                                    Index = position,
                                    ParameterName = doubleInputParamDef.Name,
                                    Value = (double)inputParam.Value,
                                    MinLimit = _owner.LowerBounds[position],
                                    MaxLimit = _owner.UpperBounds[position]
                                });
                                position++;
                                break;

                            case Point_Input_ParamDef pointInputParamDef:
                                Point3d p = (Point3d)inputParam.Value;

                                tmpList.Add(new PointNlParameterInputData()
                                    {
                                    Index = position,
                                    ParameterName = $"{pointInputParamDef.Name} - X",
                                    Value = p.X,
                                    MinLimit = _owner.LowerBounds[position],
                                    MaxLimit = _owner.UpperBounds[position]
                                    });
                                position++;
                                
                                tmpList.Add(new PointNlParameterInputData()
                                    {
                                    Index = position,
                                    ParameterName = $"{pointInputParamDef.Name} - Y",
                                    Value = p.Y,
                                    MinLimit = _owner.LowerBounds[position],
                                    MaxLimit = _owner.UpperBounds[position]
                                    });
                                position++;

                                tmpList.Add(new PointNlParameterInputData()
                                    {
                                    Index = position,
                                    ParameterName = $"{pointInputParamDef.Name} - Z",
                                    Value = p.Z,
                                    MinLimit = _owner.LowerBounds[position],
                                    MaxLimit = _owner.UpperBounds[position]
                                    });
                                position++;

                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(inputParam.Key));
                        }
                    }

                    _inputDataAsList = tmpList;
                }

                return _inputDataAsList;
            }
        }


        #region TimeSpans
        private TimeSpan _sinceNlOptStartTimeSpan;
        public TimeSpan SinceNlOptStartTimeSpan
        {
            get => _sinceNlOptStartTimeSpan;
            set => SetProperty(ref _sinceNlOptStartTimeSpan, value);
        }


        private TimeSpan _ghUpdateTimeSpan = TimeSpan.Zero;
        public TimeSpan GhUpdateTimeSpan
        {
            get => _ghUpdateTimeSpan;
            set => SetProperty(ref _ghUpdateTimeSpan, value);
        }

        private TimeSpan _calculateTimeSpan = TimeSpan.Zero;
        public TimeSpan CalculateTimeSpan
        {
            get => _calculateTimeSpan;
            set => SetProperty(ref _calculateTimeSpan, value);
        }

        private TimeSpan _feInputCalcOutputTimeSpan = TimeSpan.Zero;
        public TimeSpan FeInputCalcOutputTimeSpan
        {
            get => _feInputCalcOutputTimeSpan;
            set => SetProperty(ref _feInputCalcOutputTimeSpan, value);
        }

        private TimeSpan _totalGradientTimeSpan = TimeSpan.Zero;
        public TimeSpan TotalGradientTimeSpan
        {
            get => _totalGradientTimeSpan;
            set => SetProperty(ref _totalGradientTimeSpan, value);
        }

        private TimeSpan _totalIterationTimeSpan;
        public TimeSpan TotalIterationTimeSpan
        {
            get => _totalIterationTimeSpan;
            set => SetProperty(ref _totalIterationTimeSpan, value);
        }
        #endregion

        #region Finite Element Model
        private FeModel _feModel = null;
        public FeModel FeModel
        {
            get => _feModel;
            set => SetProperty(ref _feModel, value);
        }
        #endregion

        #region ScreenShots
        public readonly List<NlOpt_Point_ScreenShot> ScreenShots = new List<NlOpt_Point_ScreenShot>();
        #endregion



        #region Objective Function Calculation and Results
        private double _objectiveFunctionEval = double.NaN;
        public double ObjectiveFunctionEval
        {
            get => _objectiveFunctionEval;
            set => SetProperty(ref _objectiveFunctionEval, value);
        }

        private bool _hasGradient = false;
        public bool HasGradient
        {
            get => _hasGradient;
            set
            {
                SetProperty(ref _hasGradient, value);

                if (value)
                {
                    // Initializes the arrays that will have the solution points calculated for the Finite Difference's method.
                    ObjectiveFunctionGradient = new double[InputValuesAsDoubleArray.Length];
                    GradientSolutionPoints = new NlOpt_Point[InputValuesAsDoubleArray.Length];
                }
            }
        }

        private double[] _objectiveFunctionGradient;
        public double[] ObjectiveFunctionGradient
        {
            get => _objectiveFunctionGradient;
            set => SetProperty(ref _objectiveFunctionGradient, value); 
        }
        private NlOpt_Point[] _gradientSolutionPoints;
        public NlOpt_Point[] GradientSolutionPoints
        {
            get => _gradientSolutionPoints;
            set => SetProperty(ref _gradientSolutionPoints, value);
        }

        public void CalculateObjectiveFunctionResult()
        {
            Phase = NlOpt_Point_PhaseEnum.ObjectiveFunctionResult_Calculating;

            // The value has already been calculated
            if (!double.IsNaN(ObjectiveFunctionEval)) return;

            StringBuilder sb_errorMessages = new StringBuilder();

            #region Quantities related to the Objective Function
            // It will contain the sum of the problem quantities
            double eval = 0d;

            // Traverse all the output quantities that will be used to calculate the objective function
            foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ProblemQuantity_Output> quantity_Output in ProblemQuantityOutputs.Where(a => a.Key.IsObjectiveFunctionMinimize))
            {
                double quantity_aggregate_value = quantity_Output.Value.AggregatedValue ?? double.NaN;

                // Could not get the aggregate value
                if (double.IsNaN(quantity_aggregate_value))
                {
                    sb_errorMessages.AppendLine($"{quantity_Output.Key} => Failed to acquire the aggregate value to be used in the Objective Function.{Environment.NewLine}");
                    continue;
                }

                // The transformations to the concerned value list are already done within the NlOpt_Point_ProblemQuantity_Output.

                // THE OBJECTIVE IS TO MINIMIZE
                switch (quantity_Output.Key.FunctionObjective)
                {
                    case Quantity_FunctionObjectiveEnum.Target:
                        {
                            double tmp = quantity_aggregate_value - quantity_Output.Key.FunctionObjective_TargetValue;
                            switch (AppSS.I.NlOptOpt.ObjectiveFunctionSumType)
                            {
                                case ObjectiveFunctionSumTypeEnum.Simple:
                                    eval += tmp;
                                    break;

                                case ObjectiveFunctionSumTypeEnum.Squares:
                                    eval += (tmp * tmp);
                                    break;

                                // An unexpected value of the enum
                                default:
                                    sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {AppSS.I.NlOptOpt.ObjectiveFunctionSumType}");
                                    continue;
                            }
                        }
                        break;

                    case Quantity_FunctionObjectiveEnum.Minimize:
                        {
                            double tmp = quantity_aggregate_value;
                            switch (AppSS.I.NlOptOpt.ObjectiveFunctionSumType)
                            {
                                case ObjectiveFunctionSumTypeEnum.Simple:
                                    eval += tmp;
                                    break;

                                case ObjectiveFunctionSumTypeEnum.Squares:
                                    eval += (tmp * tmp);
                                    break;

                                // An unexpected value of the enum
                                default:
                                    sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {AppSS.I.NlOptOpt.ObjectiveFunctionSumType}");
                                    continue;
                            }

                            break;
                        }

                    case Quantity_FunctionObjectiveEnum.Maximize:
                        {
                            double tmp = quantity_aggregate_value;
                            switch (AppSS.I.NlOptOpt.ObjectiveFunctionSumType)
                            {
                                case ObjectiveFunctionSumTypeEnum.Simple:
                                    eval -= tmp;
                                    break;

                                case ObjectiveFunctionSumTypeEnum.Squares:
                                    eval -= (tmp * tmp);
                                    break;

                                // An unexpected value of the enum
                                default:
                                    sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {AppSS.I.NlOptOpt.ObjectiveFunctionSumType}");
                                    continue;
                            }

                            break;
                        }

                    default:
                        sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {quantity_Output.Key.FunctionObjective}");
                        continue;
                }
            }

            ObjectiveFunctionEval = eval;
            #endregion

            // There was an error in the compilation of values
            if (sb_errorMessages.Length != 0)
            {
                throw new Exception(sb_errorMessages.ToString());
            }
        }
        #endregion
        
        #region Previous Point And Stop Checks
        private NlOpt_Point _previousPoint;
        public NlOpt_Point PreviousPoint
        {
            get => _previousPoint;
            set => SetProperty(ref _previousPoint, value);
        }

        private List<StopCriteriaStatus> _stopCriteriaStatuses;
        public List<StopCriteriaStatus> StopCriteriaStatuses
        {
            get
            {
                // Creates the list
                if (_stopCriteriaStatuses == null)
                {
                    List<StopCriteriaStatus> list = new List<StopCriteriaStatus>();

                    // Maximum iterations
                    list.Add(new StopCriteriaStatus()
                        {
                        PreviousValue = PreviousPoint != null ? (double)PreviousPoint.PointIndex : double.NaN,
                        CurrentValue = (double)PointIndex,
                        CriteriaValue = (double)PointIndex,
                        IsActive = AppSS.I.NlOptOpt.IsOn_MaximumIterations,
                        Limit = (double)AppSS.I.NlOptOpt.MaximumIterations,
                        Name = ListDescSH.I.StopCriteriaTypeEnumDescriptions[StopCriteriaTypeEnum.Iterations],
                        StopCriteriaType = StopCriteriaTypeEnum.Iterations
                    });

                    // Maximum time
                    list.Add(new StopCriteriaStatus()
                        {
                        PreviousValue = PreviousPoint != null ? PreviousPoint.SinceNlOptStartTimeSpan.TotalSeconds : double.NaN,
                        CurrentValue = SinceNlOptStartTimeSpan.TotalSeconds,
                        CriteriaValue = SinceNlOptStartTimeSpan.TotalSeconds,
                        IsActive = AppSS.I.NlOptOpt.IsOn_MaximumRunTime,
                        Limit = AppSS.I.NlOptOpt.MaximumRunTime,
                        Name = ListDescSH.I.StopCriteriaTypeEnumDescriptions[StopCriteriaTypeEnum.Time],
                        StopCriteriaType = StopCriteriaTypeEnum.Time
                        });

                    // Function Stop Value
                    list.Add(new StopCriteriaStatus()
                        {
                        PreviousValue = PreviousPoint != null ? PreviousPoint.ObjectiveFunctionEval : double.NaN,
                        CurrentValue = ObjectiveFunctionEval,
                        CriteriaValue = ObjectiveFunctionEval,
                        IsActive = AppSS.I.NlOptOpt.IsOn_StopValueOnObjectiveFunction && PointIndex > 1,
                        Limit = AppSS.I.NlOptOpt.StopValueOnObjectiveFunction,
                        Name = ListDescSH.I.StopCriteriaTypeEnumDescriptions[StopCriteriaTypeEnum.FunctionValue],
                        StopCriteriaType = StopCriteriaTypeEnum.FunctionValue
                    });

                    // Function Absolute Change
                    list.Add(new StopCriteriaStatus()
                        {
                        PreviousValue = PreviousPoint != null ? PreviousPoint.ObjectiveFunctionEval : double.NaN,
                        CurrentValue = ObjectiveFunctionEval,
                        CriteriaValue = PreviousPoint != null ? Math.Abs(ObjectiveFunctionEval - PreviousPoint.ObjectiveFunctionEval) : Math.Abs(ObjectiveFunctionEval),
                        IsActive = AppSS.I.NlOptOpt.IsOn_AbsoluteToleranceOnFunctionValue && PointIndex > 1,
                        Limit = AppSS.I.NlOptOpt.AbsoluteToleranceOnFunctionValue,
                        Name = ListDescSH.I.StopCriteriaTypeEnumDescriptions[StopCriteriaTypeEnum.FunctionAbsoluteChange],
                        StopCriteriaType = StopCriteriaTypeEnum.FunctionAbsoluteChange
                    });

                    // Function Relative Change
                    list.Add(new StopCriteriaStatus()
                        {
                        PreviousValue = PreviousPoint != null ? PreviousPoint.ObjectiveFunctionEval : double.NaN,
                        CurrentValue = ObjectiveFunctionEval,
                        CriteriaValue = PreviousPoint != null ? Math.Abs((ObjectiveFunctionEval - PreviousPoint.ObjectiveFunctionEval) / PreviousPoint.ObjectiveFunctionEval) : 1d,
                        IsActive = AppSS.I.NlOptOpt.IsOn_RelativeToleranceOnFunctionValue && PointIndex > 1,
                        Limit = AppSS.I.NlOptOpt.RelativeToleranceOnFunctionValue,
                        Name = ListDescSH.I.StopCriteriaTypeEnumDescriptions[StopCriteriaTypeEnum.FunctionRelativeChange],
                        StopCriteriaType = StopCriteriaTypeEnum.FunctionRelativeChange
                    });

                    // Parameter Absolute Change
                    for (int i = 0; i < InputValuesAsDoubleArray.Length; i++)
                    {
                        list.Add(new StopCriteriaStatus()
                            {
                            PreviousValue = PreviousPoint != null ? PreviousPoint.InputValuesAsDoubleArray[i] : double.NaN,
                            CurrentValue = InputValuesAsDoubleArray[i],
                            CriteriaValue = PreviousPoint != null ? Math.Abs(InputValuesAsDoubleArray[i] - PreviousPoint.InputValuesAsDoubleArray[i]) : Math.Abs(InputValuesAsDoubleArray[i]),
                            IsActive = AppSS.I.NlOptOpt.IsOn_AbsoluteToleranceOnParameterValue && PointIndex > 1,
                            Limit = AppSS.I.NlOptOpt.AbsoluteToleranceOnParameterValue[i].ParameterTolerance,
                            Name = AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i),
                            StopCriteriaType = StopCriteriaTypeEnum.ParameterAbsoluteChange
                            });
                    }

                    // Parameter Relative Change
                    for (int i = 0; i < InputValuesAsDoubleArray.Length; i++)
                    {
                        list.Add(new StopCriteriaStatus()
                            {
                            PreviousValue = PreviousPoint != null ? PreviousPoint.InputValuesAsDoubleArray[i] : double.NaN,
                            CurrentValue = InputValuesAsDoubleArray[i],
                            CriteriaValue = PreviousPoint != null ? Math.Abs((InputValuesAsDoubleArray[i] - PreviousPoint.InputValuesAsDoubleArray[i]) / PreviousPoint.InputValuesAsDoubleArray[i]) : Math.Abs(1d),
                            IsActive = AppSS.I.NlOptOpt.IsOn_RelativeToleranceOnParameterValue && PointIndex > 1,
                            Limit = AppSS.I.NlOptOpt.RelativeToleranceOnParameterValue,
                            Name = AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i),
                            StopCriteriaType = StopCriteriaTypeEnum.ParameterRelativeChange
                        });
                    }

                    // Saves the list
                    _stopCriteriaStatuses = list;
                }

                return _stopCriteriaStatuses;
            }
        }
        #endregion



        #region Constraint Calculation and Results
        public Dictionary<ProblemQuantity, NlOpt_Point_ConstraintData> ConstraintData { get; } = new Dictionary<ProblemQuantity, NlOpt_Point_ConstraintData>();

        private bool? _allConstraintRespected;
        public bool AllConstraintsRespected
        {
            get
            {
                if (!_allConstraintRespected.HasValue)
                {
                    if (HasAnyConstraint) _allConstraintRespected = ConstraintData.All(a => a.Value.IsRespected);
                    else _allConstraintRespected = true;
                }
                return _allConstraintRespected.Value;
            }
        }

        private bool? _hasAnyConstraint;
        public bool HasAnyConstraint
        {
            get
            {
                if (!_hasAnyConstraint.HasValue)
                {
                    _hasAnyConstraint = ConstraintData.Any();
                }
                return _hasAnyConstraint.Value;
            }
        }

        private string _wpf_ConstraintTextReport;
        public string Wpf_ConstraintTextReport
        {
            get
            {
                if (_wpf_ConstraintTextReport == null)
                {
                    if (ConstraintData.Any())
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (NlOpt_Point_ConstraintData c in ConstraintData.Values)
                        {
                            sb.AppendLine(c.IsRespectedReport);
                        }

                        _wpf_ConstraintTextReport = sb.ToString();
                    }
                    else _wpf_ConstraintTextReport = "No Defined Constraint";
                }
                return _wpf_ConstraintTextReport;
            }
        }

        public void CalculateAllConstraintResults()
        {
            // Sets the constraints as given by the quantity selections
            foreach (ProblemQuantity constraintQuantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
            {
                CalculateConstraintResult(constraintQuantity);
            }
        }
        public void CalculateConstraintResult(ProblemQuantity inQuantity)
        {
            // The value has already been calculated
            if (ConstraintData.ContainsKey(inQuantity)) return;

            // Gets the aggregate value of the quantity
            double quantity_aggregate_value = ProblemQuantityOutputs[inQuantity].AggregatedValue ?? double.NaN;

            // Could not get the aggregate value
            if (double.IsNaN(quantity_aggregate_value))
            {
                throw new Exception($"{inQuantity} => Failed to acquire the aggregate value to be used in the Constraint."); ;
            }

            // Got the aggregate value
            ConstraintData[inQuantity] = new NlOpt_Point_ConstraintData(inQuantity, quantity_aggregate_value);
        }
        #endregion
        

        #region Equality - Based on SequenceEquals of the _inputValuesAsDoubleArray
        public bool Equals(NlOpt_Point other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            //return Equals(_inputValuesAsDoubleArray, other._inputValuesAsDoubleArray);
            return _inputValuesAsDoubleArray.SequenceEqual(other._inputValuesAsDoubleArray);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NlOpt_Point)obj);
        }
        public override int GetHashCode()
        {
            return (_inputValuesAsDoubleArray != null ? _inputValuesAsDoubleArray.GetHashCode() : 0);
        }
        public static bool operator ==(NlOpt_Point left, NlOpt_Point right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(NlOpt_Point left, NlOpt_Point right)
        {
            return !Equals(left, right);
        }
        #endregion

        #region Output Data Model Management
        /// <summary>
        /// DataSet with all the results. Works also as a save location for the tables to avoid them being acquired more than once.
        /// </summary>
        public DataSet OutputDataSet { get; private set; } = new DataSet();

        private readonly double _doubleRoundingDecimals = 6;
        public DataTable GetRawOutputTable(IProblemQuantitySource inSource)
        {
            string tableName = inSource.DataTableName;

            // Checks if the table already exist in the Output DataSet
            if (OutputDataSet.Tables.Contains(tableName)) return OutputDataSet.Tables[tableName];
            
            // Creates a new table to save and to output
            DataTable dt;

            // Creates a Display DataTable related to the Selected Result Classification
            switch (inSource)
            {
                case FeResultClassification feResultClassification:
                    {
                        dt = new DataTable(tableName);

                        // The list of relevant Fe results by FAMILY
                        // ATTENTION - Family "Other" has different filtering strategies
                        IEnumerable<FeResultItem> relevantResults = from a in FeModel.Results
                                                                    where a.ResultClass.ResultFamily == feResultClassification.ResultFamily
                                                                          && a.ResultClass.TargetShape == feResultClassification.TargetShape
                                                                    select a;

                        // Results are saved by result FAMILY
                        switch (feResultClassification.ResultFamily)
                        {
                            case FeResultFamilyEnum.Nodal_Reaction:
                                // Creates the relevant columns
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });
                                dt.Columns.Add("Linked Beam Elements", typeof(string)) ;

                                dt.Columns.Add("FX (N)", typeof(double));
                                dt.Columns.Add("FY (N)", typeof(double));
                                dt.Columns.Add("FZ (N)", typeof(double));
                                dt.Columns.Add("MX (Nm)", typeof(double));
                                dt.Columns.Add("MY (Nm)", typeof(double));
                                dt.Columns.Add("MZ (Nm)", typeof(double));

                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_NodalReactions r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_NodalReactions but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.LinkedElementsString ?? "";

                                    row[i++] = r.FX.HasValue ? (object)r.FX : 0d;
                                    row[i++] = r.FY.HasValue ? (object)r.FY : 0d;
                                    row[i++] = r.FZ.HasValue ? (object)r.FZ : 0d;
                                    row[i++] = r.MX.HasValue ? (object)r.MX : 0d;
                                    row[i++] = r.MY.HasValue ? (object)r.MY : 0d;
                                    row[i++] = r.MZ.HasValue ? (object)r.MZ : 0d;
                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.Nodal_Displacement:
                                // Creates the relevant columns
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });
                                dt.Columns.Add("Linked Beam Elements", typeof(string));

                                dt.Columns.Add("Ux (m)", typeof(double));
                                dt.Columns.Add("Uy (m)", typeof(double));
                                dt.Columns.Add("Uz (m)", typeof(double));
                                dt.Columns.Add("U Tot (m)", typeof(double));
                                dt.Columns.Add("Rx (rad)", typeof(double));
                                dt.Columns.Add("Ry (rad)", typeof(double));
                                dt.Columns.Add("Rz (rad)", typeof(double));

                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_NodalDisplacements r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_NodalDisplacements but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.LinkedElementsString ?? "";
                                    row[i++] = r.UX;
                                    row[i++] = r.UY;
                                    row[i++] = r.UZ;
                                    row[i++] = r.UTot;

                                    row[i++] = r.RX;
                                    row[i++] = r.RY;
                                    row[i++] = r.RZ;
                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.ElementNodal_BendingStrain:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Axial Strain at End", typeof(double));
                                dt.Columns.Add("Bending Strain +Y", typeof(double));
                                dt.Columns.Add("Bending Strain +Z", typeof(double));
                                dt.Columns.Add("Bending Strain -Y", typeof(double));
                                dt.Columns.Add("Bending Strain -Z", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalBendingStrain r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementNodalBendingStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;

                                    row[i++] = r.EPELDIR;

                                    row[i++] = r.EPELByT;
                                    row[i++] = r.EPELBzT;

                                    row[i++] = r.EPELByB;
                                    row[i++] = r.EPELBzB;

                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.ElementNodal_Force:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Axial - Fx (N)", typeof(double));
                                dt.Columns.Add("Shear - SFy (N)", typeof(double));
                                dt.Columns.Add("Shear - SFz (N)", typeof(double));

                                dt.Columns.Add("Torque - Mx (Nm)", typeof(double));
                                dt.Columns.Add("Moment - My (Nm)", typeof(double));
                                dt.Columns.Add("Moment - Mz (Nm)", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalForces r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementNodalForces but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;


                                    row[i++] = r.Fx;
                                    row[i++] = r.SFy;
                                    row[i++] = r.SFz;

                                    row[i++] = r.Tq;
                                    row[i++] = r.My;
                                    row[i++] = r.Mz;

                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.ElementNodal_Strain:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Axial - Ex", typeof(double));
                                dt.Columns.Add("Shear - SEy", typeof(double));
                                dt.Columns.Add("Shear - SEz", typeof(double));
                                dt.Columns.Add("Torsional - Te", typeof(double));
                                dt.Columns.Add("Curvature - y", typeof(double));
                                dt.Columns.Add("Curvature - z", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalStrain r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementNodalStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;


                                    row[i++] = r.Ex;
                                    row[i++] = r.SEy;
                                    row[i++] = r.SEz;

                                    row[i++] = r.Te;
                                    row[i++] = r.Ky;
                                    row[i++] = r.Kz;

                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.ElementNodal_Stress:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(string));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Axial Direct Stress (Pa)", typeof(double));
                                dt.Columns.Add("Bending Stress +Y", typeof(double));
                                dt.Columns.Add("Bending Stress +Z", typeof(double));

                                dt.Columns.Add("Bending Stress -Y", typeof(double));
                                dt.Columns.Add("Bending Stress -Z", typeof(double));

                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalStress r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementNodalStress but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;


                                    row[i++] = r.SDIR;
                                    row[i++] = r.SByT;
                                    row[i++] = r.SBzT;

                                    row[i++] = r.SByB;
                                    row[i++] = r.SBzB;

                                    dt.Rows.Add(row);
                                }
                                break;

                            case FeResultFamilyEnum.SectionNode_Stress:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Owner Mesh Node Id", typeof(string));
                                dt.Columns.Add("Section Node Id", typeof(int));
                                dt.Columns.Add("Owner Mesh Node X (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Y (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Stress - Principal 1 (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Principal 2 (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Principal 3 (Pa)", typeof(double));

                                dt.Columns.Add("Stress - Intensity (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Von-Mises (Pa)", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_SectionNodalStress r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_SectionNodalStress but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.SectionNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;


                                    row[i++] = r.S1;
                                    row[i++] = r.S2;
                                    row[i++] = r.S3;

                                    row[i++] = r.SINT;
                                    row[i++] = r.SEQV;

                                    dt.Rows.Add(row);
                                }
                                break;

                            case FeResultFamilyEnum.SectionNode_Strain:
                                // Creates the relevant columns
                                dt.Columns.Add("Frame Id", typeof(string));
                                dt.Columns.Add("Element Id", typeof(string));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Owner Mesh Node Id", typeof(string));
                                dt.Columns.Add("Section Node Id", typeof(int));
                                dt.Columns.Add("Owner Mesh Node X (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Y (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                dt.Columns.Add("Strain - Principal 1", typeof(double));
                                dt.Columns.Add("Strain - Principal 2", typeof(double));
                                dt.Columns.Add("Strain - Principal 3", typeof(double));

                                dt.Columns.Add("Strain - Intensity", typeof(double));
                                dt.Columns.Add("Strain - Von-Mises", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_SectionNodalStrain r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_SectionNodalStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                    row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                    row[i++] = resultItem.FeLocation.BeamNodeString;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.SectionNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;

                                    row[i++] = r.EPTT1;
                                    row[i++] = r.EPTT2;
                                    row[i++] = r.EPTT3;

                                    row[i++] = r.EPTTINT;
                                    row[i++] = r.EPTTEQV;

                                    dt.Rows.Add(row);
                                }
                                break;

                            case FeResultFamilyEnum.Others:
                                switch (feResultClassification.ResultType)
                                {
                                    case FeResultTypeEnum.ElementNodal_CodeCheck:
                                        // LINQ's filter is lazy, so they only do it when requested - thus it is ok to overwrite the IEnumerable
                                        relevantResults = from a in FeModel.Results
                                                            where a.ResultClass.ResultFamily == feResultClassification.ResultFamily
                                                                  && a.ResultClass.TargetShape == feResultClassification.TargetShape
                                                                  && a.ResultClass.ResultType == FeResultTypeEnum.ElementNodal_CodeCheck
                                                            select a;

                                        // Creates the relevant columns
                                        dt.Columns.Add("Frame Id", typeof(string));
                                        dt.Columns.Add("Element Id", typeof(string));
                                        dt.Columns.Add("Node", typeof(string));
                                        dt.Columns.Add("Mesh Node Id", typeof(string));
                                        dt.Columns.Add("X (m)", typeof(double));
                                        dt.Columns.Add("Y (m)", typeof(double));
                                        dt.Columns.Add("Z (m)", typeof(double));
                                        dt.Columns.Add(new DataColumn("Joint Id", typeof(string)) { AllowDBNull = true });

                                        dt.Columns.Add(new DataColumn("Pr", typeof(double)) {DefaultValue = 0d});
                                        dt.Columns.Add(new DataColumn("MrMajor", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("MrMinor", typeof(double)) { DefaultValue = 0d });

                                        dt.Columns.Add(new DataColumn("VrMajor", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("VrMinor", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("Tr", typeof(double)) { DefaultValue = 0d });

                                        dt.Columns.Add(new DataColumn("PRatio", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("MMajRatio", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("MMinRatio", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("VMajRatio", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("VMinRatio", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("TorRatio", typeof(double)) { DefaultValue = 0d });

                                        dt.Columns.Add(new DataColumn("PcComp", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("PcTension", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("MrMajorDsgn", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("McMajor", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("MrMinorDsgn", typeof(double)) { DefaultValue = 0d });
                                        dt.Columns.Add(new DataColumn("McMinor", typeof(double)) { DefaultValue = 0d });

                                        dt.Columns.Add("Total Ratio", typeof(double));

                                        foreach (FeResultItem resultItem in relevantResults)
                                        {
                                            if (!(resultItem.ResultValue is FeResultValue_ElementNodalCodeCheck r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementNodalCodeCheck but found a {resultItem.ResultValue.GetType()}.");

                                            DataRow row = dt.NewRow();
                                            int i = 0;
                                            row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                            row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                            row[i++] = resultItem.FeLocation.BeamNodeString;
                                            row[i++] = resultItem.FeLocation.MeshNode.Id;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                            row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;

                                            row[i++] = r.Pr;
                                            row[i++] = r.MrMajor;
                                            row[i++] = r.MrMinor;
                                            row[i++] = r.VrMajor;
                                            row[i++] = r.VrMinor;
                                            row[i++] = r.Tr;

                                            row[i++] = r.PRatio;
                                            row[i++] = r.MMajRatio;
                                            row[i++] = r.MMinRatio;
                                            row[i++] = r.VMajRatio;
                                            row[i++] = r.VMinRatio;
                                            row[i++] = r.TorRatio;

                                            row[i++] = r.PcComp;
                                            row[i++] = r.PcTension;
                                            row[i++] = r.MrMajorDsgn;
                                            row[i++] = r.McMajor;
                                            row[i++] = r.MrMinorDsgn;
                                            row[i++] = r.McMinor;

                                            row[i++] = r.TotalRatio;

                                            dt.Rows.Add(row);
                                        }
                                        break;

                                    case FeResultTypeEnum.Element_StrainEnergy:
                                        // LINQ's filter is lazy, so they only do it when requested - thus it is ok to overwrite the IEnumerable
                                        relevantResults = from a in FeModel.Results
                                            where a.ResultClass.ResultFamily == feResultClassification.ResultFamily
                                                  && a.ResultClass.TargetShape == feResultClassification.TargetShape
                                                  && a.ResultClass.ResultType == FeResultTypeEnum.Element_StrainEnergy
                                            select a;

                                        // Creates the relevant columns
                                        dt.Columns.Add("Frame Id", typeof(string));
                                        dt.Columns.Add("Element Id", typeof(string));
                                        dt.Columns.Add("Strain Energy", typeof(double));

                                        foreach (FeResultItem resultItem in relevantResults)
                                        {
                                            if (!(resultItem.ResultValue is FeResultValue_ElementStrainEnergy r)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_ElementStrainEnergy but found a {resultItem.ResultValue.GetType()}.");

                                            DataRow row = dt.NewRow();
                                            int i = 0;
                                            row[i++] = resultItem.FeLocation.MeshBeam.OwnerFrame.Id;
                                            row[i++] = resultItem.FeLocation.MeshBeam.Id;

                                            row[i++] = r.StrainEnergy;

                                            dt.Rows.Add(row);
                                        }
                                        break;

                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                                    case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                                    {
                                        // LINQ's filter is lazy, so they only do it when requested - thus it is ok to overwrite the IEnumerable
                                        relevantResults = from a in FeModel.Results
                                            where a.ResultClass.ResultFamily == feResultClassification.ResultFamily
                                                  && a.ResultClass.TargetShape == feResultClassification.TargetShape
                                                  && (a.ResultClass.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor ||
                                                      a.ResultClass.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor ||
                                                      a.ResultClass.ResultType == FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor)
                                            select a;

                                            // Creates the relevant columns
                                        dt.Columns.Add("Mode", typeof(int));
                                        dt.Columns.Add("Multiplier", typeof(double));

                                        if (relevantResults.Count() != 1) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects only one FeResultValue but found a {relevantResults.Count()}.");
                                        if (!(relevantResults.First().ResultValue is FeResultValue_EigenvalueBucklingSummary evRes)) throw new Exception($"Result family {ListDescSH.I.FeResultFamilyEnumDescriptions[feResultClassification.ResultFamily].Item2} expects a FeResultValue_EigenvalueBucklingSummary but found a {relevantResults.First().ResultValue.GetType()}.");

                                        foreach (KeyValuePair<int, double> pair in evRes.EigenvalueBucklingMultipliers_NonNegative)
                                        {
                                            DataRow row = dt.NewRow();
                                            int i = 0;
                                            row[i++] = pair.Key;
                                            row[i++] = pair.Value;
                                            dt.Rows.Add(row);
                                        }
                                    }
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;

                case DoubleList_GhGeom_ParamDef doubleList_GhGeom_ParamDef:
                    {
                        dt = new DataTable(tableName);
                        dt.Columns.Add("Index", typeof(int));
                        dt.Columns.Add("Double Value", typeof(double));


                        // Gets the GhParameter
                        if (!GhGeom_Values.ContainsKey(doubleList_GhGeom_ParamDef)) throw new Exception($"Could not find values for Grasshopper Double List named {doubleList_GhGeom_ParamDef.Name} in the Solution Point.");

                        // Gets th list of doubles in this parameter
                        if (!(GhGeom_Values[doubleList_GhGeom_ParamDef] is List<double> list)) throw new Exception($"The values for Grasshopper Double List named {doubleList_GhGeom_ParamDef.Name} are not stored as a List<double>.");

                        int index = 0;

                        foreach (double val in list)
                        {
                            DataRow row = dt.NewRow();
                            row[0] = index++;
                            row[1] = val;
                            dt.Rows.Add(row);
                        }
                        return dt;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Adds the table to the output dataset
            OutputDataSet.Tables.Add(dt);

            return dt;
        }

        // This is where all outputs are saved.
        public Dictionary<ProblemQuantity, NlOpt_Point_ProblemQuantity_Output> ProblemQuantityOutputs { get; } = new Dictionary<ProblemQuantity, NlOpt_Point_ProblemQuantity_Output>();
        public ICollectionView Wpf_ProblemQuantityOutputs_OutputOnly { get; private set; } // of NlOpt_Point_ProblemQuantity_Output
        public ICollectionView Wpf_ProblemQuantityOutputs_ObjectiveFunction { get; private set; } // of NlOpt_Point_ProblemQuantity_Output
        public ICollectionView Wpf_ProblemQuantityOutputs_Constraints { get; private set; } // of NlOpt_Point_ProblemQuantity_Output
        public ICollectionView Wpf_ProblemQuantityOutputs { get; private set; } // of NlOpt_Point_ProblemQuantity_Output

        public void InitializeProblemQuantityOutputs()
        {
            Phase = NlOpt_Point_PhaseEnum.Outputs_Initializing;

            #region Initialization of *** ALL *** Problem Quantities - Fe or Not
            foreach (ProblemQuantity quantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_All.OfType<ProblemQuantity>())
            {
                ProblemQuantityOutputs.Add(quantity, new NlOpt_Point_ProblemQuantity_Output(this, quantity));
            }
            #endregion

            // Initializes the ICollectionViews
            Wpf_ProblemQuantityOutputs = (new CollectionViewSource() { Source = ProblemQuantityOutputs.Values }).View;

            Wpf_ProblemQuantityOutputs_OutputOnly = (new CollectionViewSource() {Source = ProblemQuantityOutputs.Values }).View;
            Wpf_ProblemQuantityOutputs_OutputOnly.Filter += inO =>
            {
                if (inO is NlOpt_Point_ProblemQuantity_Output pqo)
                {
                    if (pqo.Quantity.IsOutputOnly) return true;
                }

                return false;
            };

            Wpf_ProblemQuantityOutputs_ObjectiveFunction = (new CollectionViewSource() { Source = ProblemQuantityOutputs.Values }).View;
            Wpf_ProblemQuantityOutputs_ObjectiveFunction.Filter += inO =>
            {
                if (inO is NlOpt_Point_ProblemQuantity_Output pqo)
                {
                    if (pqo.Quantity.IsObjectiveFunctionMinimize) return true;
                }

                return false;
            };

            Wpf_ProblemQuantityOutputs_Constraints = (new CollectionViewSource() { Source = ProblemQuantityOutputs.Values }).View;
            Wpf_ProblemQuantityOutputs_Constraints.Filter += inO =>
            {
                if (inO is NlOpt_Point_ProblemQuantity_Output pqo)
                {
                    if (pqo.Quantity.IsConstraint) return true;
                }

                return false;
            };
        }
        #endregion

        private void OtherElements_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ScreenShotOptions && e.PropertyName == "SelectedDisplayDirection")
            {
                // Updates the image accordingly
                RaisePropertyChanged("SelectedDisplayScreenShot");
            }
            if (sender is ScreenShotOptions && e.PropertyName == "SelectedDisplayImageResultClassification")
            {
                // Updates the image accordingly
                RaisePropertyChanged("SelectedDisplayScreenShot");
            }
            else if (sender is FeOptions && e.PropertyName == "SelectedDisplayImageResultClassification")
            {
                // Updates the image accordingly
                RaisePropertyChanged("SelectedDisplayScreenShot");
            }
            else if (sender is FeOptions && e.PropertyName == "SelectedDisplayDataResultClassification")
            {
                // Updates the image accordingly
                RaisePropertyChanged("SelectedDisplayData");
            }
        }


        public ImageSource SelectedDisplayScreenShot
        {
            get
            {
                // Gets the Screen shot
                NlOpt_Point_ScreenShot screenShot = ScreenShots.FirstOrDefault(a => a.Direction == AppSS.I.ScreenShotOpt.SelectedDisplayDirection &&
                                                                                    a.Result == AppSS.I.ScreenShotOpt.SelectedDisplayImageResultClassification);
                if (screenShot == null) return null;

                MemoryStream ms = new MemoryStream();
                screenShot.Image.Save(ms, ImageFormat.Png);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }


        #region Wpf
        public string WpfName => $"{PointIndex} - {ObjectiveFunctionEval}";
        
        public int? SelectedDisplayData_FirstDataColumnIndex = null;
        public DataView SelectedDisplayData
        {
            get
            {
                // Gets the DataTable
                DataTable dt = GetRawOutputTable(AppSS.I.FeOpt.SelectedDisplayDataResultClassification);

                // Outputs a DataView
                DataView dv = new DataView(dt);
                return dv;
            }
        }

        public readonly string GradientTableName = $"Gradient Value Table";
        public DataView GradientDataAsDataView
        {
            get
            {
                if (!OutputDataSet.Tables.Contains(GradientTableName))
                {
                    // Creates the input list as a table
                    DataTable table = new DataTable(GradientTableName);

                    table.Columns.Add("Index", typeof(int));
                    table.Columns.Add("Parameter", typeof(string));
                    table.Columns.Add("Value", typeof(double));

                    if (HasGradient)
                    {
                        table.Columns.Add("Input Δ", typeof(double));
                        table.Columns.Add("f(x) Δ", typeof(double));

                        // A column for each of the constraints
                        foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ConstraintData> constraint in ConstraintData)
                        {
                            table.Columns.Add($"C({constraint.Key.InternalId}) Δ", typeof(double));
                        }
                    }

                    int position = 0;
                    foreach (KeyValuePair<Input_ParamDefBase, object> inputParam in GhInput_Values)
                    {
                        DataRow newRow = null;

                        switch (inputParam.Key)
                        {
                            case Double_Input_ParamDef doubleInputParamDef:
                                newRow = table.NewRow();
                                newRow["Index"] = position;
                                newRow["Parameter"] = doubleInputParamDef.Name;
                                newRow["Value"] = (double)inputParam.Value;

                                if (HasGradient)
                                {
                                    newRow["Input Δ"] = GradientSolutionPoints[position].InputValuesAsDoubleArray[position] - InputValuesAsDoubleArray[position];
                                    newRow["f(x) Δ"] = ObjectiveFunctionGradient[position];

                                    // A column for each of the constraints
                                    foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ConstraintData> constraint in ConstraintData)
                                    {
                                        newRow[$"C({constraint.Key.InternalId}) Δ"] = constraint.Value.Gradients[position];
                                    }
                                }

                                table.Rows.Add(newRow);
                                position++;
                                break;

                            case Point_Input_ParamDef pointInputParamDef:
                                Point3d p = (Point3d)inputParam.Value;

                                newRow = table.NewRow();
                                newRow["Index"] = position;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - X";
                                newRow["Value"] = p.X;

                                if (HasGradient)
                                {
                                    newRow["Input Δ"] = GradientSolutionPoints[position].InputValuesAsDoubleArray[position] - InputValuesAsDoubleArray[position];
                                    newRow["f(x) Δ"] = ObjectiveFunctionGradient[position];

                                    // A column for each of the constraints
                                    foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ConstraintData> constraint in ConstraintData)
                                    {
                                        newRow[$"C({constraint.Key.InternalId}) Δ"] = constraint.Value.Gradients[position];
                                    }
                                }

                                table.Rows.Add(newRow);
                                position++;

                                newRow = table.NewRow();
                                newRow["Index"] = position;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - Y";
                                newRow["Value"] = p.Y;

                                if (HasGradient)
                                {
                                    newRow["Input Δ"] = GradientSolutionPoints[position].InputValuesAsDoubleArray[position] - InputValuesAsDoubleArray[position];
                                    newRow["f(x) Δ"] = ObjectiveFunctionGradient[position];

                                    // A column for each of the constraints
                                    foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ConstraintData> constraint in ConstraintData)
                                    {
                                        newRow[$"C({constraint.Key.InternalId}) Δ"] = constraint.Value.Gradients[position];
                                    }
                                }

                                table.Rows.Add(newRow);
                                position++;

                                newRow = table.NewRow();
                                newRow["Index"] = position;
                                newRow["Parameter"] = $"{pointInputParamDef.Name} - Z";
                                newRow["Value"] = p.Z;

                                if (HasGradient)
                                {
                                    newRow["Input Δ"] = GradientSolutionPoints[position].InputValuesAsDoubleArray[position] - InputValuesAsDoubleArray[position];
                                    newRow["f(x) Δ"] = ObjectiveFunctionGradient[position];

                                    // A column for each of the constraints
                                    foreach (KeyValuePair<ProblemQuantity, NlOpt_Point_ConstraintData> constraint in ConstraintData)
                                    {
                                        newRow[$"C({constraint.Key.InternalId}) Δ"] = constraint.Value.Gradients[position];
                                    }
                                }

                                table.Rows.Add(newRow);
                                position++;

                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(inputParam.Key));
                        }
                        //position += inputParam.Key.VarCount;
                    }

                    // Saves in the OutputDataSet
                    OutputDataSet.Tables.Add(table);
                }

                // Returns the table as a DataView
                return new DataView(OutputDataSet.Tables[GradientTableName]);
            }
        }
        #endregion

        #region Phase and Messages
        public FastObservableCollection<NlOpt_Point_Message> RuntimeWarningMessages { get; private set; } = new FastObservableCollection<NlOpt_Point_Message>();


        private NlOpt_Point_PhaseEnum _phase = NlOpt_Point_PhaseEnum.Initializing;
        public NlOpt_Point_PhaseEnum Phase
        {
            get => _phase;
            set
            {
                SetProperty(ref _phase, value);

                // Updates the elapsed time since the beginning
                SinceNlOptStartTimeSpan = DateTime.Now - Owner.NlOptSolverWrapper.NlOpt_OptimizationStartTime;
            }
        }
        public string PhaseAsString => ListDescSH.I.NlOpt_Point_PhaseEnumDescriptions[Phase];
        #endregion

        public override string ToString()
        {
            return $"{PointIndex} {Phase} {ObjectiveFunctionEval}";
        }

        public void ReleaseManagedResources()
        {
            _previousPoint = null;
            // Unsubscribe from events
            AppSS.I.ScreenShotOpt.PropertyChanged -= OtherElements_PropertyChanged;
            AppSS.I.FeOpt.PropertyChanged -= OtherElements_PropertyChanged;
        }
    }

    public class NlOpt_Point_ConstraintData : BindableBase, IEquatable<NlOpt_Point_ConstraintData>
    {
        public NlOpt_Point_ConstraintData([NotNull] ProblemQuantity inProbQuantity, double inEvalValue)
        {
            ProbQuantity = inProbQuantity ?? throw new ArgumentNullException(nameof(inProbQuantity));
            EvalValue = inEvalValue;
        }

        public ProblemQuantity ProbQuantity { get; }
        public double EvalValue { get; }

        private double[] _gradients;
        public double[] Gradients
        {
            get => _gradients;
            set => SetProperty(ref _gradients, value);
        }

        private bool? _isRespected;
        public bool IsRespected
        {
            get
            {
                if (!_isRespected.HasValue)
                {
                    _isRespected = IsRespectedStatic(ProbQuantity, EvalValue);
                }

                return _isRespected.Value;
            }
        }

        private string _isRespectedReport;
        public string IsRespectedReport
        {
            get
            {
                if (_isRespectedReport == null)
                {
                    string respectedValueText;
                    switch (ProbQuantity.ConstraintObjective)
                    {
                        case Quantity_ConstraintObjectiveEnum.EqualTo:
                            respectedValueText = $"{(IsRespected ? "OK" : "FAIL")} EQUAL {(ProbQuantity.ConstraintObjective_CompareValue - ProbQuantity.ConstraintTolerance)} <= {EvalValue:+0.000e+000;-0.000e+000;0.0} <= {(ProbQuantity.ConstraintObjective_CompareValue + ProbQuantity.ConstraintTolerance)}";
                            break;

                        case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                            respectedValueText = $"{(IsRespected ? "OK" : "FAIL")} LTorE {EvalValue:+0.000e+000;-0.000e+000;0.0} <= {(ProbQuantity.ConstraintObjective_CompareValue + ProbQuantity.ConstraintTolerance)}";

                            _isRespected = EvalValue <= (ProbQuantity.ConstraintObjective_CompareValue + ProbQuantity.ConstraintTolerance);
                            break;

                        case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                            respectedValueText = $"{(IsRespected ? "OK" : "FAIL")} HTorE {EvalValue:+0.000e+000;-0.000e+000;0.0} >= {(ProbQuantity.ConstraintObjective_CompareValue - ProbQuantity.ConstraintTolerance)}";
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _isRespectedReport = $"{ProbQuantity.InternalId} :: {ProbQuantity.QuantitySource.ToString()} :: {ListDescSH.I.Quantity_AggregateTypeEnumDescriptions[ProbQuantity.QuantityAggregatorOptions.AggregateType]} :: {respectedValueText}";
                }

                return _isRespectedReport;
            }
        }
        
        #region Equality based on Problem Quantity
        public bool Equals(NlOpt_Point_ConstraintData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ProbQuantity, other.ProbQuantity);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NlOpt_Point_ConstraintData)obj);
        }
        public override int GetHashCode()
        {
            return (ProbQuantity != null ? ProbQuantity.GetHashCode() : 0);
        }
        public static bool operator ==(NlOpt_Point_ConstraintData left, NlOpt_Point_ConstraintData right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(NlOpt_Point_ConstraintData left, NlOpt_Point_ConstraintData right)
        {
            return !Equals(left, right);
        } 
        #endregion

        public static bool IsRespectedStatic(ProblemQuantity inProblemQuantity, double evalValue)
        {
            if (!inProblemQuantity.IsConstraint) throw new Exception($"Checking if the constraint is respected expects that the ProbQuantity be of type Constraint. Type received: {inProblemQuantity.TreatmentType}");

            switch (inProblemQuantity.ConstraintObjective)
            {
                case Quantity_ConstraintObjectiveEnum.EqualTo:
                    return evalValue >= (inProblemQuantity.ConstraintObjective_CompareValue - inProblemQuantity.ConstraintTolerance) &&
                           evalValue <= (inProblemQuantity.ConstraintObjective_CompareValue + inProblemQuantity.ConstraintTolerance);

                case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                    return evalValue <= (inProblemQuantity.ConstraintObjective_CompareValue + inProblemQuantity.ConstraintTolerance);

                case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                    return evalValue >= (inProblemQuantity.ConstraintObjective_CompareValue - inProblemQuantity.ConstraintTolerance);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    public enum NlOpt_Point_CalcTypeEnum
    {
        ObjectiveFunction,
        Gradient,
    }
    public enum NlOpt_Point_PhaseEnum
    {
        Initializing,
        Grasshopper_Updating,
        FiniteElement_Running,
        Outputs_Initializing,
        ObjectiveFunctionResult_Calculating,
        Gradients_Running,
        Ended,
    }
    public class NlOpt_Point_ScreenShot
    {
        /// <summary>
        /// Creates a new ScreenShot Instance
        /// </summary>
        /// <param name="inResult">The linked FeResult. Set to null if Rhino ScreenShot</param>
        /// <param name="inDirection"></param>
        /// <param name="inImage"></param>
        public NlOpt_Point_ScreenShot(IProblemQuantitySource inResult, ImageCaptureViewDirectionEnum inDirection, [NotNull] Image inImage)
        {
            Result = inResult;
            Direction = inDirection;
            Image = inImage ?? throw new ArgumentNullException(nameof(inImage));
        }

        /// <summary>
        /// The result that is linked to this ScreenShot. Null means that it is a Rhino ScreenShot
        /// </summary>
        public IProblemQuantitySource Result { get; set; }

        public Image Image { get; set; }

        public ImageCaptureViewDirectionEnum Direction { get; set; }
        public string WpfImageDirection => ScreenShotOptions.GetFriendlyEnumName(Direction);
    }
    public class NlOpt_Point_Message
    {
        public NlOpt_Point_Message([NotNull] string inMessage, NlOpt_Point_MessageSourceEnum inSource, NlOpt_Point_MessageLevelEnum inLevel, Exception inInnerException = null)
        {
            Message = inMessage ?? throw new ArgumentNullException(nameof(inMessage));

            Source = inSource;
            Level = inLevel;
            InnerException = inInnerException;
        }

        public string Message { get; set; }
        public NlOpt_Point_MessageSourceEnum Source { get; set; }
        public NlOpt_Point_MessageLevelEnum Level { get; set; }
        public Exception InnerException { get; set; }
    }
    public enum NlOpt_Point_MessageSourceEnum
    {
        Internal,
        FiniteElementSolver,
        Grasshopper
    }
    public enum NlOpt_Point_MessageLevelEnum
    {
        Remark,
        Warning,
    }

    public class StopCriteriaStatus
    {
        public string Name { get; set; }

        public double PreviousValue { get; set; }
        public double CurrentValue { get; set; }
        public double CriteriaValue { get; set; }

        public double Limit { get; set; }

        public bool LimitReached
        {
            get
            {
                switch (StopCriteriaType)
                {
                    case StopCriteriaTypeEnum.Time:
                    case StopCriteriaTypeEnum.Iterations:
                        return CriteriaValue >= Limit;

                    case StopCriteriaTypeEnum.FunctionValue:
                    case StopCriteriaTypeEnum.FunctionAbsoluteChange:
                    case StopCriteriaTypeEnum.FunctionRelativeChange:
                    case StopCriteriaTypeEnum.ParameterAbsoluteChange:
                    case StopCriteriaTypeEnum.ParameterRelativeChange:
                        return CriteriaValue <= Limit; 

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool IsActive { get; set; }

        public StopCriteriaTypeEnum StopCriteriaType { get; set; }
    }
    public enum StopCriteriaTypeEnum
    {
        Time,
        Iterations,
        FunctionValue,
        FunctionAbsoluteChange,
        FunctionRelativeChange,
        ParameterAbsoluteChange,
        ParameterRelativeChange,
    }

    public class PointNlParameterInputData
    {
        public int Index { get; set; }
        public double Value { get; set; }
        public double MinLimit { get; set; }
        public double MaxLimit { get; set; }
        public string ParameterName { get; set; }
        public string ToolTipValue
        {
            get
            {
                return $"{DefaultNumberConverter.GetString(Value)} [{DefaultNumberConverter.GetString(MinLimit)} -- {DefaultNumberConverter.GetString(MaxLimit)}]";
            }
        }
    }
}

