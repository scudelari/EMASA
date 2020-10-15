extern alias r3dm;
using System;
using System.Collections.Generic;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;


namespace Emasa_Optimizer.Opt
{
    public class SolutionPoint : BindableBase, IEquatable<SolutionPoint>
    {
        [NotNull] private readonly SolveManager _owner;
        public SolveManager Owner => _owner;
        public SolutionPoint([NotNull] SolveManager inOwner, double[] inInput, SolutionPointCalcTypeEnum inSolutionPointCalcType)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            Status = SolutionPointStatusEnum.SolutionPoint_Initializing;

            #region Grasshopper Inputs and Geometry
            // Sets and initializes the Gh related dictionaries
            GhInput_Values = new Dictionary<Input_ParamDefBase, object>();
            foreach (Input_ParamDefBase ghAlgInputDef in _owner.Gh_Alg.InputDefs)
            {
                GhInput_Values.Add(ghAlgInputDef, null);
            }

            GhGeom_Values = new Dictionary<GhGeom_ParamDefBase, object>();
            foreach (GhGeom_ParamDefBase ghAlgGeometryDef in _owner.Gh_Alg.GeometryDefs)
            {
                GhGeom_Values.Add(ghAlgGeometryDef, null);
            }
            #endregion


            InputValuesAsDoubleArray = (double[])inInput.Clone();
            SolutionPointCalcType = inSolutionPointCalcType;

            #region Wpf Communication
            _owner.FeOptions.ScreenShotOptions.PropertyChanged += OtherElements_PropertyChanged;
            _owner.FeOptions.PropertyChanged += OtherElements_PropertyChanged;
            #endregion
        }

        #region GH values in this solution point
        public Dictionary<Input_ParamDefBase, object> GhInput_Values { get; private set; }
        public Dictionary<GhGeom_ParamDefBase, object> GhGeom_Values { get; private set; }
        #endregion

        public SolutionPointCalcTypeEnum SolutionPointCalcType { get; set; }

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

                        case Integer_Input_ParamDef integerInputParamDef:
                            GhInput_Values[inputParamDef] = (int)_inputValuesAsDoubleArray[position];
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

                            case Integer_Input_ParamDef integerInputParamDef:
                                newRow = table.NewRow();
                                newRow["Index"] = position++;
                                newRow["Parameter"] = integerInputParamDef.Name;
                                newRow["Value"] = (double)(int)inputParam.Value;
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


        #region TimeSpans
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
        public readonly List<SolutionPoint_ScreenShot> ScreenShots = new List<SolutionPoint_ScreenShot>();
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
                    GradientSolutionPoints = new SolutionPoint[InputValuesAsDoubleArray.Length];
                    ConstraintGradients = new Dictionary<ProblemQuantity, double[]>();
                }
            }
        }

        private double[] _objectiveFunctionGradient;
        public double[] ObjectiveFunctionGradient
        {
            get => _objectiveFunctionGradient;
            set => SetProperty(ref _objectiveFunctionGradient, value); 
        }
        private SolutionPoint[] _gradientSolutionPoints;
        public SolutionPoint[] GradientSolutionPoints
        {
            get => _gradientSolutionPoints;
            set => SetProperty(ref _gradientSolutionPoints, value);
        }

        public void CalculateObjectiveFunctionResult()
        {
            Status = SolutionPointStatusEnum.ObjectiveFunctionResult_Calculating;

            // The value has already been calculated
            if (!double.IsNaN(ObjectiveFunctionEval)) return;

            try
            {
                StringBuilder sb_errorMessages = new StringBuilder();

                #region Quantities related to the Objective Function
                // It will contain the sum of the problem quantities
                double eval = 0d;

                // Traverse all the output quantities that will be used to calculate the objective function
                foreach (KeyValuePair<ProblemQuantity, SolutionPoint_ProblemQuantity_Output> quantity_Output in ProblemQuantityOutputs.Where(a => a.Key.IsObjectiveFunctionMinimize))
                {
                    double quantity_aggregate_value = quantity_Output.Value.AggregatedValue ?? double.NaN;

                    // Could not get the aggregate value
                    if (double.IsNaN(quantity_aggregate_value))
                    {
                        sb_errorMessages.AppendLine($"{quantity_Output.Key} => Failed to acquire the aggregate value to be used in the Objective Function.{Environment.NewLine}");
                        continue;
                    }

                    // The transformations to the concerned value list are already done within the SolutionPoint_ProblemQuantity_Output.

                    // THE OBJECTIVE IS TO MINIMIZE
                    switch (quantity_Output.Key.FunctionObjective)
                    {
                        case Quantity_FunctionObjectiveEnum.Target:
                            {
                                double tmp = quantity_aggregate_value - quantity_Output.Key.FunctionObjective_TargetValue;
                                switch (_owner.NlOptManager.ObjectiveFunctionSumType)
                                {
                                    case ObjectiveFunctionSumTypeEnum.Simple:
                                        eval += tmp;
                                        break;

                                    case ObjectiveFunctionSumTypeEnum.Squares:
                                        eval += (tmp * tmp);
                                        break;

                                    // An unexpected value of the enum
                                    default:
                                        sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {_owner.NlOptManager.ObjectiveFunctionSumType}");
                                        continue;
                                }
                            }
                            break;

                        case Quantity_FunctionObjectiveEnum.Minimize:
                            {
                                double tmp = quantity_aggregate_value;
                                switch (_owner.NlOptManager.ObjectiveFunctionSumType)
                                {
                                    case ObjectiveFunctionSumTypeEnum.Simple:
                                        eval += tmp;
                                        break;

                                    case ObjectiveFunctionSumTypeEnum.Squares:
                                        eval += (tmp * tmp);
                                        break;

                                    // An unexpected value of the enum
                                    default:
                                        sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {_owner.NlOptManager.ObjectiveFunctionSumType}");
                                        continue;
                                }

                                break;
                            }

                        case Quantity_FunctionObjectiveEnum.Maximize:
                            {
                                double tmp = quantity_aggregate_value;
                                switch (_owner.NlOptManager.ObjectiveFunctionSumType)
                                {
                                    case ObjectiveFunctionSumTypeEnum.Simple:
                                        eval -= tmp;
                                        break;

                                    case ObjectiveFunctionSumTypeEnum.Squares:
                                        eval -= (tmp * tmp);
                                        break;

                                    // An unexpected value of the enum
                                    default:
                                        sb_errorMessages.AppendLine($"{quantity_Output.Key} => Invalid: {_owner.NlOptManager.ObjectiveFunctionSumType}");
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
            catch (Exception error)
            {
                FailureException = error;
                ObjectiveFunctionEval = double.NaN;
            }
        }
        #endregion

        #region Constraint Calculation and Results
        public Dictionary<ProblemQuantity, double> ConstraintEvals { get; } = new Dictionary<ProblemQuantity, double>();
        public Dictionary<ProblemQuantity, double[]> ConstraintGradients { get; set; }

        public void CalculateConstraintResult(ProblemQuantity inQuantity)
        {
            // The value has already been calculated
            if (ConstraintEvals.ContainsKey(inQuantity)) return;

            // Gets the aggregate value of the quantity
            double quantity_aggregate_value = ProblemQuantityOutputs[inQuantity].AggregatedValue ?? double.NaN;

            // Could not get the aggregate value
            if (double.IsNaN(quantity_aggregate_value))
            {
                FailureException = new Exception($"{inQuantity} => Failed to acquire the aggregate value to be used in the Constraint.{Environment.NewLine}"); ;
                ConstraintEvals[inQuantity] = double.NaN;
            }

            // Got the aggregate value
            ConstraintEvals[inQuantity] = quantity_aggregate_value;
        }
        #endregion

        #region Equality - Based on SequenceEquals of the _inputValuesAsDoubleArray
        public bool Equals(SolutionPoint other)
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
            return Equals((SolutionPoint)obj);
        }
        public override int GetHashCode()
        {
            return (_inputValuesAsDoubleArray != null ? _inputValuesAsDoubleArray.GetHashCode() : 0);
        }
        public static bool operator ==(SolutionPoint left, SolutionPoint right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(SolutionPoint left, SolutionPoint right)
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
            string tableName = string.Empty;

            switch (inSource)
            {
                case FeResultClassification feResultClassification:
                    switch (feResultClassification.ResultType)
                    {
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode1Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode2Factor:
                        case FeResultTypeEnum.Model_EigenvalueBuckling_Mode3Factor:
                            tableName = $"{feResultClassification.TargetShapeDescription} - {feResultClassification.ResultFamilyGroupName} - EV Blk Modes";
                            break;

                        default:
                            tableName = $"{feResultClassification.TargetShapeDescription} - {feResultClassification.ResultFamilyGroupName} - {feResultClassification.ResultTypeDescription}";
                            break;
                    }
                    
                    break;

                case DoubleList_GhGeom_ParamDef doubleList_GhGeom_ParamDef:
                    tableName = doubleList_GhGeom_ParamDef.Name;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inSource));
            }

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
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });
                                dt.Columns.Add("Linked Beam Elements", typeof(string));

                                dt.Columns.Add("FX (N)", typeof(double));
                                dt.Columns.Add("FY (N)", typeof(double));
                                dt.Columns.Add("FZ (N)", typeof(double));
                                dt.Columns.Add("MX (Nm)", typeof(double));
                                dt.Columns.Add("MY (Nm)", typeof(double));
                                dt.Columns.Add("MZ (Nm)", typeof(double));

                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_NodalReactions r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_NodalReactions but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.LinkedElementsString;

                                    row[i++] = r.FX;
                                    row[i++] = r.FY;
                                    row[i++] = r.FZ;
                                    row[i++] = r.MX;
                                    row[i++] = r.MY;
                                    row[i++] = r.MZ;
                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.Nodal_Displacement:
                                // Creates the relevant columns
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });
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
                                    if (!(resultItem.ResultValue is FeResultValue_NodalDisplacements r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_NodalDisplacements but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
                                    row[i++] = resultItem.FeLocation.MeshNode.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                    row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                    row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;
                                    row[i++] = resultItem.FeLocation.MeshNode.LinkedElementsString;

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
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Axial Strain at End", typeof(double));
                                dt.Columns.Add("Bending Strain +Y", typeof(double));
                                dt.Columns.Add("Bending Strain +Z", typeof(double));
                                dt.Columns.Add("Bending Strain -Y", typeof(double));
                                dt.Columns.Add("Bending Strain -Z", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalBendingStrain r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementNodalBendingStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Axial - Fx (N)", typeof(double));
                                dt.Columns.Add("Shear - SFy (N)", typeof(double));
                                dt.Columns.Add("Shear - SFz (N)", typeof(double));

                                dt.Columns.Add("Torque - Mx (Nm)", typeof(double));
                                dt.Columns.Add("Moment - My (Nm)", typeof(double));
                                dt.Columns.Add("Moment - Mz (Nm)", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalForces r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementNodalForces but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Axial - Ex", typeof(double));
                                dt.Columns.Add("Shear - SEy", typeof(double));
                                dt.Columns.Add("Shear - SEz", typeof(double));

                                dt.Columns.Add("Curvature - y", typeof(double));
                                dt.Columns.Add("Curvature - z", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalStrain r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementNodalStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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

                                    row[i++] = r.Ky;
                                    row[i++] = r.Kz;

                                    dt.Rows.Add(row);
                                }

                                break;

                            case FeResultFamilyEnum.ElementNodal_Stress:
                                // Creates the relevant columns
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Mesh Node Id", typeof(int));
                                dt.Columns.Add("X (m)", typeof(double));
                                dt.Columns.Add("Y (m)", typeof(double));
                                dt.Columns.Add("Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Axial Direct Stress (Pa)", typeof(double));
                                dt.Columns.Add("Bending Stress +Y", typeof(double));
                                dt.Columns.Add("Bending Stress +Z", typeof(double));

                                dt.Columns.Add("Bending Stress -Y", typeof(double));
                                dt.Columns.Add("Bending Stress -Z", typeof(double));

                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_ElementNodalStress r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementNodalStress but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Owner Mesh Node Id", typeof(int));
                                dt.Columns.Add("Section Node Id", typeof(int));
                                dt.Columns.Add("Owner Mesh Node X (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Y (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Stress - Principal 1 (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Principal 2 (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Principal 3 (Pa)", typeof(double));

                                dt.Columns.Add("Stress - Intensity (Pa)", typeof(double));
                                dt.Columns.Add("Stress - Von-Mises (Pa)", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_SectionNodalStress r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_SectionNodalStress but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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
                                dt.Columns.Add("Element Id", typeof(int));
                                dt.Columns.Add("Node", typeof(string));
                                dt.Columns.Add("Owner Mesh Node Id", typeof(int));
                                dt.Columns.Add("Section Node Id", typeof(int));
                                dt.Columns.Add("Owner Mesh Node X (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Y (m)", typeof(double));
                                dt.Columns.Add("Owner Mesh Node Z (m)", typeof(double));
                                dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                dt.Columns.Add("Strain - Principal 1", typeof(double));
                                dt.Columns.Add("Strain - Principal 2", typeof(double));
                                dt.Columns.Add("Strain - Principal 3", typeof(double));

                                dt.Columns.Add("Strain - Intensity", typeof(double));
                                dt.Columns.Add("Strain - Von-Mises", typeof(double));


                                foreach (FeResultItem resultItem in relevantResults)
                                {
                                    if (!(resultItem.ResultValue is FeResultValue_SectionNodalStrain r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_SectionNodalStrain but found a {resultItem.ResultValue.GetType()}.");

                                    DataRow row = dt.NewRow();
                                    int i = 0;
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
                                        dt.Columns.Add("Element Id", typeof(int));
                                        dt.Columns.Add("Node", typeof(string));
                                        dt.Columns.Add("Mesh Node Id", typeof(int));
                                        dt.Columns.Add("X (m)", typeof(double));
                                        dt.Columns.Add("Y (m)", typeof(double));
                                        dt.Columns.Add("Z (m)", typeof(double));
                                        dt.Columns.Add(new DataColumn("Joint Id", typeof(int)) { AllowDBNull = true });

                                        dt.Columns.Add("Axial Stress Fx DivBy Area (Pa)", typeof(double));
                                        dt.Columns.Add("Bending Stress DivBy M2 DivBy Z2 (Pa)", typeof(double));
                                        dt.Columns.Add("Bending Stress DivBy M3 DivBy Z3 (Pa)", typeof(double));

                                        dt.Columns.Add("SUM (Pa)", typeof(double));
                                        dt.Columns.Add("Yield * Gamma_Mat (Pa)", typeof(double));
                                        dt.Columns.Add("Ratio", typeof(double));

                                        foreach (FeResultItem resultItem in relevantResults)
                                        {
                                            if (!(resultItem.ResultValue is FeResultValue_ElementNodalCodeCheck r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementNodalCodeCheck but found a {resultItem.ResultValue.GetType()}.");

                                            DataRow row = dt.NewRow();
                                            int i = 0;
                                            row[i++] = resultItem.FeLocation.MeshBeam.Id;
                                            row[i++] = resultItem.FeLocation.BeamNodeString;
                                            row[i++] = resultItem.FeLocation.MeshNode.Id;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.X;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.Y;
                                            row[i++] = resultItem.FeLocation.MeshNode.Point.Z;
                                            row[i++] = resultItem.FeLocation.MeshNode.MatchingJoint == null ? DBNull.Value : (object)resultItem.FeLocation.MeshNode.MatchingJoint.Id;


                                            row[i++] = r.P_A;
                                            row[i++] = r.M2_Z2;
                                            row[i++] = r.M3_Z3;

                                            row[i++] = r.SUM;
                                            row[i++] = r.G_MAT_FY;
                                            row[i++] = r.RATIO;

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
                                        dt.Columns.Add("Element Id", typeof(int));
                                        dt.Columns.Add("Strain Energy", typeof(double));

                                        foreach (FeResultItem resultItem in relevantResults)
                                        {
                                            if (!(resultItem.ResultValue is FeResultValue_ElementStrainEnergy r)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_ElementStrainEnergy but found a {resultItem.ResultValue.GetType()}.");

                                            DataRow row = dt.NewRow();
                                            int i = 0;
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

                                        if (relevantResults.Count() != 1) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects only one FeResultValue but found a {relevantResults.Count()}.");
                                        if (!(relevantResults.First().ResultValue is FeResultValue_EigenvalueBucklingSummary evRes)) throw new Exception($"Result family {feResultClassification.ResultFamilyGroupName} expects a FeResultValue_EigenvalueBucklingSummary but found a {relevantResults.First().ResultValue.GetType()}.");

                                        foreach (KeyValuePair<int, double> pair in evRes.EigenvalueBucklingMultipliers)
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

        public Dictionary<ProblemQuantity, SolutionPoint_ProblemQuantity_Output> ProblemQuantityOutputs { get; } = new Dictionary<ProblemQuantity, SolutionPoint_ProblemQuantity_Output>();

        public void InitializeProblemQuantityOutputs()
        {
            Status = SolutionPointStatusEnum.Outputs_Initializing;

            #region Initialization of *** ALL *** Problem Quantities - Fe or Not
            foreach (ProblemQuantity quantity in _owner.WpfProblemQuantities_All.OfType<ProblemQuantity>())
            {
                ProblemQuantityOutputs.Add(quantity, new SolutionPoint_ProblemQuantity_Output(this, quantity));
            }
            #endregion
        }
        #endregion

        #region Wpf
        private void OtherElements_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is FeScreenShotOptions && e.PropertyName == "SelectedDisplayDirection")
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

        public string WpfName => $"{PointIndex} - {ObjectiveFunctionEval}";

        public ImageSource SelectedDisplayScreenShot
        {
            get
            {
                // Gets the Screen shot
                SolutionPoint_ScreenShot screenShot = ScreenShots.FirstOrDefault(a => a.Direction == _owner.FeOptions.ScreenShotOptions.SelectedDisplayDirection &&
                                                                                      a.Result == _owner.FeOptions.SelectedDisplayImageResultClassification);
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

        public int? SelectedDisplayData_FirstDataColumnIndex = null;
        public DataView SelectedDisplayData
        {
            get
            {
                // Gets the DataTable
                DataTable dt = GetRawOutputTable(_owner.FeOptions.SelectedDisplayDataResultClassification);

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
                        foreach (KeyValuePair<ProblemQuantity, double[]> constraintGradient in ConstraintGradients)
                        {
                            table.Columns.Add($"C({constraintGradient.Key.InternalId}) Δ", typeof(double));
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
                                    foreach (KeyValuePair<ProblemQuantity, double[]> constraintGradient in ConstraintGradients)
                                    {
                                        newRow[$"C({constraintGradient.Key.InternalId}) Δ"] = constraintGradient.Value[position];
                                    }
                                }

                                table.Rows.Add(newRow);
                                position++;
                                break;

                            case Integer_Input_ParamDef integerInputParamDef:
                                //newRow = table.NewRow();
                                //newRow["Index"] = position++;
                                //newRow["Parameter"] = integerInputParamDef.Name;
                                //newRow["Value"] = (double)(int)inputParam.Value;
                                //table.Rows.Add(newRow);
                                //position++;
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
                                    foreach (KeyValuePair<ProblemQuantity, double[]> constraintGradient in ConstraintGradients)
                                    {
                                        newRow[$"C({constraintGradient.Key.InternalId}) Δ"] = constraintGradient.Value[position];
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
                                    foreach (KeyValuePair<ProblemQuantity, double[]> constraintGradient in ConstraintGradients)
                                    {
                                        newRow[$"C({constraintGradient.Key.InternalId}) Δ"] = constraintGradient.Value[position];
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
                                    foreach (KeyValuePair<ProblemQuantity, double[]> constraintGradient in ConstraintGradients)
                                    {
                                        newRow[$"C({constraintGradient.Key.InternalId}) Δ"] = constraintGradient.Value[position];
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

        #region Failure
        private bool _isSuccess = true;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string StatusString => IsSuccess ? "Success" : "Failed";
        public Visibility ErrorMessageVisibility => IsSuccess ? Visibility.Collapsed : Visibility.Visible;

        private Exception _failureException = null;
        public Exception FailureException
        {
            get => _failureException;
            set
            {
                IsSuccess = value == null;
                SetProperty(ref _failureException, value);
            }
        }
        #endregion


        #region Status and Messages
        public FastObservableCollection<SolutionPoint_Message> RuntimeMessages { get; private set; } = new FastObservableCollection<SolutionPoint_Message>();

        private SolutionPointStatusEnum _status = SolutionPointStatusEnum.Outputs_Initializing;
        public SolutionPointStatusEnum Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        #endregion

        public override string ToString()
        {
            return $"{PointIndex} {Status} {ObjectiveFunctionEval}";
        }
    }

    public enum SolutionPointCalcTypeEnum
    {
        ObjectiveFunction,
        Gradient,
    }

    public enum SolutionPointStatusEnum
    {
        SolutionPoint_Initializing,
        Grasshopper_Updating,
        FiniteElement_Running,
        Outputs_Initializing,
        ObjectiveFunctionResult_Calculating,
        Gradients_Running,
        Ended_Success,
        Ended_Failure
    }

    public class SolutionPoint_ScreenShot
    {
        /// <summary>
        /// Creates a new ScreenShot Instance
        /// </summary>
        /// <param name="inResult">The linked FeResult. Set to null if Rhino ScreenShot</param>
        /// <param name="inDirection"></param>
        /// <param name="inImage"></param>
        public SolutionPoint_ScreenShot(FeResultClassification inResult, ImageCaptureViewDirectionEnum inDirection, [NotNull] Image inImage)
        {
            Result = inResult;
            Direction = inDirection;
            Image = inImage ?? throw new ArgumentNullException(nameof(inImage));
        }

        /// <summary>
        /// The result that is linked to this ScreenShot. Null means that it is a Rhino ScreenShot
        /// </summary>
        public FeResultClassification Result { get; set; }
        public string WpfResultShape => Result != null ? FeResultClassification.GetFriendlyEnumName(Result.TargetShape) : string.Empty;
        public string WpfResultFamily => Result != null ? FeResultClassification.GetFriendlyEnumName(Result.ResultFamily) : "Rhino";
        public string WpfResultType => Result != null ? FeResultClassification.GetFriendlyEnumName(Result.ResultType) : string.Empty;

        public Image Image { get; set; }

        public ImageCaptureViewDirectionEnum Direction { get; set; }
        public string WpfImageDirection => FeScreenShotOptions.GetFriendlyEnumName(Direction);
    }

    public class SolutionPoint_Message
    {
        public SolutionPoint_Message([NotNull] string inMessage, SolutionPoint_MessageSourceEnum inSource, SolutionPoint_MessageLevelEnum inLevel, Exception inInnerException = null)
        {
            Message = inMessage ?? throw new ArgumentNullException(nameof(inMessage));

            Source = inSource;
            Level = inLevel;
            InnerException = inInnerException;
        }

        public string Message { get; set; }
        public SolutionPoint_MessageSourceEnum Source { get; set; }
        public SolutionPoint_MessageLevelEnum Level { get; set; }
        public Exception InnerException { get; set; }
    }
    public enum SolutionPoint_MessageSourceEnum
    {
        Internal,
        NlOptSolver,
        FiniteElementSolver,
        Grasshopper
    }
    public enum SolutionPoint_MessageLevelEnum
    {
        Remark,
        Warning,
        Error
    }
}

