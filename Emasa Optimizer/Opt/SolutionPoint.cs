extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt
{
    public class SolutionPoint : BindableBase, IEquatable<SolutionPoint>
    {
        [NotNull] private readonly SolveManager _owner;
        public SolveManager Owner => _owner;
        public SolutionPoint([NotNull] SolveManager inOwner, double[] inInput, EvalTypeEnum inEvalType)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

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

            InputValuesAsDoubleArray = inInput;
            EvalType = inEvalType;

            GradientAtThisPoint = new double[InputValuesAsDoubleArray.Length];
        }
        
        #region GH values in this solution point
        public Dictionary<Input_ParamDefBase, object> GhInput_Values { get; private set; }
        public Dictionary<GhGeom_ParamDefBase, object> GhGeom_Values { get; private set; }
        #endregion

        public EvalTypeEnum EvalType { get; set; }

        private double[] _inputValuesAsDoubleArray;
        public double[] InputValuesAsDoubleArray
        {
            get => _inputValuesAsDoubleArray;
            private set
            {
                _inputValuesAsDoubleArray = value;

                // Updates the Input Dictionary
                int position = 0;
                foreach (Input_ParamDefBase inputParamDef in GhInput_Values.Keys)
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

        private double _functionEval;
        public double FunctionEval
        {
            get => _functionEval;
            set => SetProperty(ref _functionEval, value);
        }
        
        public double[] GradientAtThisPoint { get; set; }

        private int _pointIndex;
        public int PointIndex
        {
            get => _pointIndex;
            set => SetProperty(ref _pointIndex, value);
        }

        #region TimeSpans
        private TimeSpan _ghUpdateTimeSpan = TimeSpan.Zero;
        public TimeSpan GhUpdateTimeSpan
        {
            get => _ghUpdateTimeSpan;
            set => SetProperty(ref _ghUpdateTimeSpan, value);
        }
        
        private TimeSpan _evalTimeSpan = TimeSpan.Zero;
        public TimeSpan EvalTimeSpan
        {
            get => _evalTimeSpan;
            set => SetProperty(ref _evalTimeSpan, value);
        }

        private TimeSpan _totalGradientTimeSpan = TimeSpan.Zero;
        public TimeSpan TotalGradientTimeSpan
        {
            get => _totalGradientTimeSpan;
            set => SetProperty(ref _totalGradientTimeSpan, value);
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
    }

    public enum EvalTypeEnum
    {
        ObjectiveFunction,
        Gradient,
        SectionDefinition
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
}
