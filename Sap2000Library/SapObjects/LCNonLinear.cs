using System.Collections.Generic;
using Sap2000Library.Managers;
using SAP2000v1;

namespace Sap2000Library.SapObjects
{
    public class LCNonLinear : LoadCase
    {
        internal LCNonLinear(LCManager lCManager) : base(lCManager) { }

        private string _initialCase;
        public string InitialCase
        {
            get
            {
                if (string.IsNullOrEmpty(_initialCase))
                {
                    string temp = null;
                    int ret;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetInitialCase(Name, ref temp);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetInitialCase(Name, ref temp);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    _initialCase = ret == 0 ? temp : _initialCase = null;
                }
                return _initialCase;
            }
            set
            {
                _initialCase = value;
            }
        }

        // ! To be implemented !
        // GetLoads
        // GetLoadApplication
        // GetMassSource
        // GetModalCase

        private LCNonLinear_SubType? _nLSubType;
        public LCNonLinear_SubType NLSubType
        {
            get
            {
                if (!_nLSubType.HasValue)
                {
                    eLoadCaseType caseType = 0;
                    int subType = 0;
                    eLoadPatternType designType = 0;
                    int designTypeOption = 0;
                    int auto = 0;
                    int ret = owner.SapApi.LoadCases.GetTypeOAPI_1(Name, ref caseType, ref subType, ref designType, ref designTypeOption, ref auto);

                    if (ret == 0) _nLSubType = (LCNonLinear_SubType)(int)subType;
                    else _nLSubType = null;
                }
                return _nLSubType.Value;
            }
            set { _nLSubType = value; }
        }

        private LCNonLinear_NLGeomType? _nLGeomType;
        public LCNonLinear_NLGeomType nLGeomType
        {
            get
            {
                if (!_nLGeomType.HasValue)
                {
                    int temp = 0;
                    int ret = 0;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetGeometricNonlinearity(Name, ref temp);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetGeometricNonlinearity(Name, ref temp);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    if (ret == 0) _nLGeomType = (LCNonLinear_NLGeomType)temp;
                    else _nLGeomType = null;
                }
                return _nLGeomType.Value;
            }
            set { _nLGeomType = value; }
        }

        private LCNonLinear_UnloadType? _nLUnloadType;
        public LCNonLinear_UnloadType NLUnloadType
        {
            get
            {
                if (!_nLUnloadType.HasValue)
                {
                    int temp = 0;
                    int ret = 0;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetHingeUnloading(Name, ref temp);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetHingeUnloading(Name, ref temp);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    if (ret == 0) _nLUnloadType = (LCNonLinear_UnloadType)temp;
                    else _nLUnloadType = null;
                }
                return _nLUnloadType.Value;
            }
            set { _nLUnloadType = value; }
        }

        private LCNonLinear_SolverControlParams _solControlParams;
        public LCNonLinear_SolverControlParams SolControlParams
        {
            get
            {
                if (_solControlParams == null)
                {
                    int MaxTotalSteps = 0;
                    int MaxFailedSubSteps = 0;
                    int MaxIterCS = 0;
                    int MaxIterNR = 0;
                    double TolConvD = 0;
                    bool UseEventStepping = false;
                    double TolEventD = 0;
                    int MaxLineSearchPerIter = 0;
                    double TolLineSearch = 0;
                    double LineSearchStepFact = 0;

                    int ret = 0; 

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetSolControlParameters
                                    (Name, ref MaxTotalSteps, ref MaxFailedSubSteps, ref MaxIterCS, ref MaxIterNR,
                                    ref TolConvD, ref UseEventStepping, ref TolEventD, ref MaxLineSearchPerIter,
                                    ref TolLineSearch, ref LineSearchStepFact);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetSolControlParameters
                                    (Name, ref MaxTotalSteps, ref MaxFailedSubSteps, ref MaxIterCS, ref MaxIterNR,
                                    ref TolConvD, ref UseEventStepping, ref TolEventD, ref MaxLineSearchPerIter,
                                    ref TolLineSearch, ref LineSearchStepFact);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    if (ret == 0)
                    {
                        _solControlParams = new LCNonLinear_SolverControlParams()
                        {
                            MaxTotalSteps = MaxTotalSteps,
                            MaxFailedSubSteps = MaxFailedSubSteps,
                            MaxIterCS = MaxIterCS,
                            MaxIterNR = MaxIterNR,
                            TolConvD = TolConvD,
                            UseEventStepping = UseEventStepping,
                            TolEventD = TolEventD,
                            MaxLineSearchPerIter = MaxLineSearchPerIter,
                            TolLineSearch = TolLineSearch,
                            LineSearchStepFact = LineSearchStepFact
                        };
                    }
                    else _solControlParams = null;
                }
                return _solControlParams;
            }
            set => _solControlParams = value;
        }

        private LCNonLinear_TargetForceParams _targetForceParams;
        public LCNonLinear_TargetForceParams TargetForceParams
        {
            get
            {
                if (_targetForceParams == null)
                {
                    double TolConvF = 0;
                    int MaxIter = 0;
                    double AccelFact = 0;
                    bool NoStop = false;

                    int ret = 0;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetTargetForceParameters
                                (Name, ref TolConvF, ref MaxIter, ref AccelFact, ref NoStop);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetTargetForceParameters
                                (Name, ref TolConvF, ref MaxIter, ref AccelFact, ref NoStop);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    if (ret == 0)
                    {
                        _targetForceParams = new LCNonLinear_TargetForceParams()
                        {
                            TolConvF = TolConvF,
                            MaxIter = MaxIter,
                            AccelFact = AccelFact,
                            NoStop = NoStop
                        };
                    }
                    else _targetForceParams = null;
                }
                return _targetForceParams;
            }
            set
            {
                _targetForceParams = value;
            }
        }

        private LCNonLinear_ResultsSavedNL _resultsSavedNL;
        public LCNonLinear_ResultsSavedNL ResultsSavedNL
        {
            get
            {
                if (_resultsSavedNL == null)
                {
                    bool SaveMultipleSteps = false;
                    int MinSavedStates = 0;
                    int MaxSavedStates = 0;
                    bool PositiveOnly = false;

                    int ret = 0;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            ret = owner.SapApi.LoadCases.StaticNonlinear.GetResultsSaved
                                (Name, ref SaveMultipleSteps, ref MinSavedStates, ref MaxSavedStates, ref PositiveOnly);
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            break;
                        default:
                            ret = -1;
                            break;
                    }
                    
                    // It could actually fail, then we need to return an empty object to be bound.
                    _resultsSavedNL = new LCNonLinear_ResultsSavedNL()
                    {
                        SaveMultipleSteps = SaveMultipleSteps,
                        MinSavedStates = MinSavedStates,
                        MaxSavedStates = MaxSavedStates,
                        PositiveOnly = PositiveOnly
                    };
                }
                return _resultsSavedNL;
            }
            set
            {
                _resultsSavedNL = value;
            }
        }

        private LCNonLinear_ResultsSavedStaged _resultsSavedStaged;
        public LCNonLinear_ResultsSavedStaged ResultsSavedStaged
        {
            get
            {
                if (_resultsSavedStaged == null)
                {
                    int StagedSaveOption = 0;
                    int StagedMinSteps = 0;
                    int StagedMinStepsTD = 0;

                    int ret = 0;

                    switch (NLSubType)
                    {
                        case LCNonLinear_SubType.Nonlinear:
                            break;
                        case LCNonLinear_SubType.StagedConstruction:
                            ret = owner.SapApi.LoadCases.StaticNonlinearStaged.GetResultsSaved
                                (Name, ref StagedSaveOption, ref StagedMinSteps, ref StagedMinStepsTD);
                            break;
                        default:
                            ret = -1;
                            break;
                    }

                    // It could actually fail, then we need to return an empty object to be bound.
                    _resultsSavedStaged = new LCNonLinear_ResultsSavedStaged()
                    {
                        StagedSaveOption = (LCNonLinear_StagedSaveOption)StagedSaveOption,
                        StagedMinSteps = StagedMinSteps,
                        StagedMinStepsTD = StagedMinStepsTD
                    };
                }
                return _resultsSavedStaged;
            }
            set
            {
                _resultsSavedStaged = value;
            }
        }

        private LCNonLinear_LoadApplicationOptions _loadApplicationOptions;
        public LCNonLinear_LoadApplicationOptions LoadApplicationOptions
        {
            get
            {
                if (_loadApplicationOptions == null)
                {
                    int loadControl = 0;
                    int dispType = 0;
                    double disp = 0d;
                    int monitor = 0;
                    int dof = 0;
                    string pointName = "";
                    string genDisp = "";

                    if (0 != owner.SapApi.LoadCases.StaticNonlinear.GetLoadApplication(
                        Name,
                        ref loadControl,
                        ref dispType,
                        ref disp,
                        ref monitor,
                        ref dof,
                        ref pointName,
                        ref genDisp)) throw new S2KHelperException($"Could not get the Load Application Options for load case named {Name}.");

                    _loadApplicationOptions = new LCNonLinear_LoadApplicationOptions()
                    {
                        Displacement = disp,
                        DispType = (LCNonLinear_DispType)dispType,
                        DOF = (LCNonLinear_DOF)dof,
                        GeneralizedDisplacementName = genDisp,
                        LoadControl = (LCNonLinear_LoadControl)loadControl,
                        Monitor = (LCNonLinear_Monitor)monitor,
                        PointName = pointName
                    };
                }
                return _loadApplicationOptions;
            }
            set
            {
                _loadApplicationOptions = value;
            }
        }

        public void FillSolverControlData()
        {
            // Just pings the properties so that they grab the data
            bool solControlParamsEnable = SolControlParams == null;
            bool targetForceParamsEnable = TargetForceParams == null;
            bool InitialCaseEnable = InitialCase == null;
            bool StatusEnable = Status == null;
            bool RunFlagEnable = RunFlag == null;
            bool ResultsSavedNLEnable = ResultsSavedNL == null;
            bool ResultsSavedEnable = ResultsSavedStaged == null;
        }

        public List<LCNonLinear_PossibleStagedSaveOptions> StagedSavedAtOptions
        {
            get;
        } = new List<LCNonLinear_PossibleStagedSaveOptions>
                {
                new LCNonLinear_PossibleStagedSaveOptions { Text = "End Each Stage", EnumVal = LCNonLinear_StagedSaveOption.EndOfEachStage },
                new LCNonLinear_PossibleStagedSaveOptions { Text = "End Final", EnumVal = LCNonLinear_StagedSaveOption.EndOfFinalStage },
                new LCNonLinear_PossibleStagedSaveOptions { Text = "Start & End", EnumVal = LCNonLinear_StagedSaveOption.StartAndEndOfEachStage },
                new LCNonLinear_PossibleStagedSaveOptions { Text = "More Each Stage", EnumVal = LCNonLinear_StagedSaveOption.TwoOrMoreTimesInEachStage }
                };
    }
    public class LCNonLinear_SolverControlParams
    {
        /// <summary>
        /// The maximum total steps per stage.
        /// </summary>
        private int? maxTotalSteps;

        /// <summary>
        /// The maximum null (zero) steps per stage.
        /// </summary>
        private int? maxFailedSubSteps;

        /// <summary>
        /// The maximum constant-stiffness iterations per step.
        /// </summary>
        private int? maxIterCS;

        /// <summary>
        /// The maximum Newton_Raphson iterations per step.
        /// </summary>
        private int? maxIterNR;

        /// <summary>
        /// The relative iteration convergence tolerance.
        /// </summary>
        private double? tolConvD;

        /// <summary>
        /// This item is True if event-to-event stepping is used.
        /// </summary>
        private bool useEventStepping;

        /// <summary>
        /// The relative event lumping tolerance.
        /// </summary>
        private double? tolEventD;

        /// <summary>
        /// The maximum number of line searches per iteration.
        /// </summary>
        private int? maxLineSearchPerIter;

        /// <summary>
        /// The relative line-search acceptance tolerance.
        /// </summary>
        private double? tolLineSearch;

        /// <summary>
        /// The line-search step factor.
        /// </summary>
        private double? lineSearchStepFact;

        public int? MaxTotalSteps { get => maxTotalSteps; set => maxTotalSteps = value; }
        public int? MaxFailedSubSteps { get => maxFailedSubSteps; set => maxFailedSubSteps = value; }
        public int? MaxIterCS { get => maxIterCS; set => maxIterCS = value; }
        public int? MaxIterNR { get => maxIterNR; set => maxIterNR = value; }
        public double? TolConvD { get => tolConvD; set => tolConvD = value; }
        public bool UseEventStepping { get => useEventStepping; set => useEventStepping = value; }
        public double? TolEventD { get => tolEventD; set => tolEventD = value; }
        public int? MaxLineSearchPerIter { get => maxLineSearchPerIter; set => maxLineSearchPerIter = value; }
        public double? TolLineSearch { get => tolLineSearch; set => tolLineSearch = value; }
        public double? LineSearchStepFact { get => lineSearchStepFact; set => lineSearchStepFact = value; }
    }
    public class LCNonLinear_TargetForceParams
    {
        /// <summary>
        /// The relative convergence tolerance for target force iteration.
        /// </summary>
        private double? tolConvF;

        /// <summary>
        /// The maximum iterations per stage for target force iteration.
        /// </summary>
        private int? maxIter;

        /// <summary>
        /// The acceleration factor.
        /// </summary>
        private double? accelFact;

        /// <summary>
        /// If this item is True, the analysis is continued when there is no convergence in the target force iteration.
        /// </summary>
        private bool noStop;

        public int? MaxIter { get => maxIter; set => maxIter = value; }
        public double? TolConvF { get => tolConvF; set => tolConvF = value; }
        public double? AccelFact { get => accelFact; set => accelFact = value; }
        public bool NoStop { get => noStop; set => noStop = value; }
    }
    public class LCNonLinear_ResultsSavedNL
    {
        private bool saveMultipleSteps;
        private int? minSavedStates;
        private int? maxSavedStates;
        private bool positiveOnly;

        public bool SaveMultipleSteps { get => saveMultipleSteps; set => saveMultipleSteps = value; }
        public int? MinSavedStates { get => minSavedStates; set => minSavedStates = value; }
        public int? MaxSavedStates { get => maxSavedStates; set => maxSavedStates = value; }
        public bool PositiveOnly { get => positiveOnly; set => positiveOnly = value; }
    }
    public class LCNonLinear_ResultsSavedStaged
    {
        private LCNonLinear_StagedSaveOption stagedSaveOption;
        private int? stagedMinSteps;
        private int? stagedMinStepsTD;

        public LCNonLinear_StagedSaveOption StagedSaveOption { get => stagedSaveOption; set => stagedSaveOption = value; }
        public int? StagedMinSteps { get => stagedMinSteps; set => stagedMinSteps = value; }
        public int? StagedMinStepsTD { get => stagedMinStepsTD; set => stagedMinStepsTD = value; }
    }
    public class LCNonLinear_PossibleStagedSaveOptions
    {
        private string text;
        private LCNonLinear_StagedSaveOption enumVal;

        public string Text { get => text; set => text = value; }
        public LCNonLinear_StagedSaveOption EnumVal { get => enumVal; set => enumVal = value; }
    }

    public class LCNonLinear_LoadApplicationOptions
    {
        private LCNonLinear_LoadControl _loadControl;
        public LCNonLinear_LoadControl LoadControl
        {
            get { return _loadControl; }
            set { _loadControl = value; }
        }

        private LCNonLinear_DispType _dispType;
        public LCNonLinear_DispType DispType
        {
            get { return _dispType; }
            set { _dispType = value; }
        }


        private double _disp;
        public double Displacement
        {
            get { return _disp; }
            set { _disp = value; }
        }


        private LCNonLinear_Monitor _monitor;
        public LCNonLinear_Monitor Monitor
        {
            get { return _monitor; }
            set { _monitor = value; }
        }

        private LCNonLinear_DOF _DOF;
        public LCNonLinear_DOF DOF
        {
            get { return _DOF; }
            set { _DOF = value; }
        }

        private string _pointName;
        public string PointName
        {
            get { return _pointName; }
            set { _pointName = value; }
        }

        private string _gDispl;
        public string GeneralizedDisplacementName
        {
            get { return _gDispl; }
            set { _gDispl = value; }
        }

    }
}
