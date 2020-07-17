using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Converters;

namespace Sap2000Library
{
    public enum SapObjectType
    {
        Point = 1,
        Frame = 2,
        Link = 7,
        Cable = 3,
        Area = 5,
        Solid = 6,
        Tendon = 4
    }
    public enum MatTypeEnum
    {
        Steel = 1,
        Concrete = 2,
        NoDesign = 3,
        Aluminum = 4,
        ColdFormed = 5,
        Rebar = 6,
        Tendon = 7,
        Masonry = 8
    }
    public enum UnitsEnum
    {
        lb_in_F = 1,
        lb_ft_F = 2,
        kip_in_F = 3,
        kip_ft_F = 4,
        kN_mm_C = 5,
        kN_m_C = 6,
        kgf_mm_C = 7,
        kgf_m_C = 8,
        N_mm_C = 9,
        N_m_C = 10,
        Ton_mm_C = 11,
        Ton_m_C = 12,
        kN_cm_C = 13,
        kgf_cm_C = 14,
        N_cm_C = 15,
        Ton_cm_C = 16
    }
    public enum ItemTypeEnum
    {
        Objects = 0,
        Group = 1,
        SelectedObjects = 2
    }
    public enum ItemTypeElmEnum
    {
        ObjectElm = 0,
        Element = 1,
        GroupElm = 2,
        SelectionElm = 3
    }
    public enum ObjTypeEnum
    {
        Point = 1,
        Frame = 2,
        Area = 3,
        Solid = 6
    }
    

    public enum SelectObjectType
    {
        PointObject = 1,
        FrameObject = 2,
        CableObject = 3,
        TendonObject = 4,
        AreaObject = 5,
        SolidObject = 6,
        LinkObject = 7
    }

    public enum ConstraintTypeEnum
    {
        Body = 1,
        Diaphragm = 2,
        Plate = 3,
        Rod = 4,
        Beam = 5,
        Equal = 6,
        Local = 7,
        Weld = 8,
        Line = 13
    }

    public enum ConstraintAxisEnum
    {
        X=1,
        Y=2,
        Z=3,
        Auto=4
    }

    public enum AreaAdvancedAxes_Plane
    {
        Plane31 = 31,
        Plane32 = 32
    }

    public enum FrameAdvancedAxes_Plane2
    {
        Plane12 = 12,
        Plane13 = 13
    }

    public enum PointAdvancedAxes_Plane2
    {
        Plane12 = 12,
        Plane13 = 13,
        Plane21 = 21,
        Plane23 = 23,
        Plane31 = 31,
        Plane32 = 32
    }

    public enum AdvancedAxesAngle_Vector
    {
        CoordinateDirection = 1,
        TwoJoints = 2,
        UserVector = 3
    }

    public enum AdvancedAxesAngle_PlaneReference
    {
        PosX = 1,
        PosY = 2,
        PosZ = 3,
        PosCR = 4,
        PosCA = 5,
        PosCZ = 6,
        PosSR = 7,
        PosSA = 8,
        PosSB = 9,
        NegX = -1,
        NegY = -2,
        NegZ = -3,
        NegCR = -4,
        NegCA = -5,
        NegCZ = -6,
        NegSR = -7,
        NegSA = -8,
        NegSB = -9
    }

    public enum LoadCaseTypeEnum
    {
        CASE_LINEAR_STATIC = 1,
        CASE_NONLINEAR_STATIC = 2,
        CASE_MODAL = 3,
        CASE_RESPONSE_SPECTRUM = 4,
        CASE_LINEAR_HISTORY = 5, //(Modal Time History)
        CASE_NONLINEAR_HISTORY = 6, //(Modal Time History)
        CASE_LINEAR_DYNAMIC = 7, //(Direct Integration Time History)
        CASE_NONLINEAR_DYNAMIC = 8, //(Direct Integration Time History)
        CASE_MOVING_LOAD = 9,
        CASE_BUCKLING = 10,
        CASE_STEADY_STATE = 11,
        CASE_POWER_SPECTRAL_DENSITY = 12,
        CASE_LINEAR_STATIC_MULTISTEP = 13,
        CASE_HYPERSTATIC = 14,
        ExternalResults = 15,
        StagedConstruction = 16,
        NonlinearStaticMultiStep = 17,
        EMS_ALL = 50 // Used to get all the case types
    }
    public enum LoadCaseDesignType
    {
        Dead = 1,
        SuperDead = 2,
        Live = 3,
        ReduceLive = 4,
        Quake = 5,
        Wind = 6,
        Snow = 7,
        Other = 8,
        Move = 9,
        Temperature = 10,
        RoofLive = 11,
        Notional = 12,
        PatternLive = 13,
        Wave = 14,
        Braking = 15,
        Centrifugal = 16,
        Friction = 17,
        Ice = 18,
        WindOnLiveLoad = 19,
        HorizontalEarthPressure = 20,
        VerticalEarthPressure = 21,
        EarthSurcharge = 22,
        DownDrag = 23,
        VehicleCollision = 24,
        VesselCollision = 25,
        TemperatureGradient = 26,
        Settlement = 27,
        Shrinkage = 28,
        Creep = 29,
        WaterLoadPressure = 30,
        LiveLoadSurcharge = 31,
        LockedInForces = 32,
        PedestrianLL = 33,
        Prestress = 34,
        Hyperstatic = 35,
        Bouyancy = 36,
        StreamFlow = 37,
        Impact = 38,
        Construction = 39,
        DeadWearing = 40,
        DeadWater = 41,
        DeadManufacture = 42,
        EarthHydrostatic = 43,
        PassiveEarthPressure = 44,
        ActiveEarthPressure = 45,
        PedestrianLLReduced = 46,
        SnowHighAltitude = 47,
        EuroLm1Char = 48,
        EuroLm1Freq = 49,
        EuroLm2 = 50,
        EuroLm3 = 51,
        EuroLm4 = 52
    }

    public enum LCNonLinear_NLGeomType
    {
        None = 0,
        PDelta = 1,
        PDeltaLargeDisp = 2
    }
    public enum LCNonLinear_UnloadType
    {
        UnloadEntireStructure = 1,
        ApplyLocalRedistribution2 = 2,
        RestartUsingSecantStiffness = 3
    }
    public enum LCNonLinear_SubType
    {
        Nonlinear = 1,
        StagedConstruction = 2
    }
    public enum LCStatus
    {
        NotRun = 1,
        CouldNotStart = 2,
        NotFinished = 3,
        Finished = 4
    }
    public enum LCNonLinear_StagedSaveOption
    {
        EndOfFinalStage = 0,
        EndOfEachStage = 1,
        StartAndEndOfEachStage = 2,
        TwoOrMoreTimesInEachStage = 3
    }
    public enum SAP2000HotKeyInstruction
    {
        RemoveSelectionFromView,
        ShowAll,
        SelectAll
    }
    public enum PointRestraintType
    {
        FullyFixed, SimplySupported, HasAtLeastOne, HasNone
    }
    public enum PointAdvancedAxes_Direction
    {
        Plus_X = 1,
        Minus_X = -1,
        Plus_Y = 2,
        Minus_Y = -2,
        Plus_Z = 3,
        Minus_Z = -3,
        Plus_CR = 4,
        Minus_CR = -4,
        Plus_CA = 5,
        Minus_CA = -5,
        Plus_CZ = 6,
        Minus_CZ = -6,
        Plus_SR = 7,
        Minus_SR = -7,
        Plus_SA = 8,
        Minus_SA = -8,
        Plus_SB = 9,
        Minus_SB = -9
    }

    public enum PointPanelZone_PropType
    {
        PropertiesAreElasticFromColumn = 0,
        PropertiesAreElasticFromColumnAndDoublerPlate = 1,
        PropertiesAreFromSpecifiedSpringStiffnesses = 2,
        PropertiesAreFromASpecifiedLinkProperty = 3
    }
    public enum PointPanelZone_Connectivity
    {
        PanelZoneConnectsBeamsToOtherObjects = 0,
        PanelZoneConnectsBracesToOtherObjects = 1
    }
    public enum PointPanelZone_LocalAxisFrom
    {
        PanelZoneLocalAxisAngleIsFromColumn = 0,
        PanelZoneLocalAxisAngleIsUserDefined = 1
    }
    public enum StagedConstructionOperation
    {
        AddStructure = 1,
        RemoveStructure = 2,

        LoadObjectsIfNew = 3,
        LoadObjects = 4,

        ChangeSectionPropertyModifiers_Area = 600,
        ChangeSectionPropertyModifiers_Frame = 601,

        ChangeReleases = 7,
        ChangeSectionPropertiesAndAge = 11,
        AddGuideStructure = 14,

        ChangeSectionProperties_Area = 500,
        ChangeSectionProperties_Frame = 501,
        ChangeSectionProperties_Cable = 502,
        ChangeSectionProperties_Link = 503
    }
    public enum ResponseCombinationType
    {
        LinearAdditive = 0,
        Envelope = 1,
        AbsoluteAdditive = 2,
        SRSS = 3,
        RangeAdditive = 4
    }
    public enum ResponseCombination_AddedCaseOrComb
    {
        LoadCase = 0,
        LoadCombo = 1
    }
    public enum FramePropType
    {
        SECTION_I = 1,
        SECTION_CHANNEL = 2,
        SECTION_T = 3,
        SECTION_ANGLE = 4,
        SECTION_DBLANGLE = 5,
        SECTION_BOX = 6,
        SECTION_PIPE = 7,
        SECTION_RECTANGULAR = 8,
        SECTION_CIRCLE = 9,
        SECTION_GENERAL = 10,
        SECTION_DBCHANNEL = 11,
        SECTION_AUTO = 12,
        SECTION_SD = 13,
        SECTION_VARIABLE = 14,
        SECTION_JOIST = 15,
        SECTION_BRIDGE = 16,
        SECTION_COLD_C = 17,
        SECTION_COLD_2C = 18,
        SECTION_COLD_Z = 19,
        SECTION_COLD_L = 20,
        SECTION_COLD_2L = 21,
        SECTION_COLD_HAT = 22,
        SECTION_BUILTUP_I_COVERPLATE = 23,
        SECTION_PCC_GIRDER_I = 24,
        SECTION_PCC_GIRDER_U = 25,
        SECTION_BUILTUP_I_HYBRID = 26,
        SECTION_BUILTUP_U_HYBRID = 27
    }
    public enum LoadPatternType
    {
        LTYPE_DEAD = 1,
        LTYPE_SUPERDEAD = 2,
        LTYPE_LIVE = 3,
        LTYPE_REDUCELIVE = 4,
        LTYPE_QUAKE = 5,
        LTYPE_WIND = 6,
        LTYPE_SNOW = 7,
        LTYPE_OTHER = 8,
        LTYPE_MOVE = 9,
        LTYPE_TEMPERATURE = 10,
        LTYPE_ROOFLIVE = 11,
        LTYPE_NOTIONAL = 12,
        LTYPE_PATTERNLIVE = 13,
        LTYPE_WAVE = 14,
        LTYPE_BRAKING = 15,
        LTYPE_CENTRIFUGAL = 16,
        LTYPE_FRICTION = 17,
        LTYPE_ICE = 18,
        LTYPE_WINDONLIVELOAD = 19,
        LTYPE_HORIZONTALEARTHPRESSURE = 20,
        LTYPE_VERTICALEARTHPRESSURE = 21,
        LTYPE_EARTHSURCHARGE = 22,
        LTYPE_DOWNDRAG = 23,
        LTYPE_VEHICLECOLLISION = 24,
        LTYPE_VESSELCOLLISION = 25,
        LTYPE_TEMPERATUREGRADIENT = 26,
        LTYPE_SETTLEMENT = 27,
        LTYPE_SHRINKAGE = 28,
        LTYPE_CREEP = 29,
        LTYPE_WATERLOADPRESSURE = 30,
        LTYPE_LIVELOADSURCHARGE = 31,
        LTYPE_LOCKEDINFORCES = 32,
        LTYPE_PEDESTRIANLL = 33,
        LTYPE_PRESTRESS = 34,
        LTYPE_HYPERSTATIC = 35,
        LTYPE_BOUYANCY = 36,
        LTYPE_STREAMFLOW = 37,
        LTYPE_IMPACT = 38,
        LTYPE_CONSTRUCTION = 39
    }
    public enum OptionMultiStepStatic
    {
        Envelopes = 1,
        StepbyStep = 2,
        LastStep = 3
    }

    public enum OptionMultiValuedCombo
    {
        Envelopes = 1,
        MultipleValuesIfPossible = 2,
        Correspondence = 3
    }

    public enum OptionNLStatic
    {
        Envelopes = 1,
        StepbyStep = 2,
        LastStep = 3
    }
    public enum SteelDesignRatioType
    {
        PMM = 1,
        MajorShear = 2,
        MinorShear = 3,
        MajorBeamColumCapacityRatio = 4,
        MinorBeamColumCapacityRatio = 5,
        Other = 6
    }
    public enum LCNonLinear_LoadControl
    {
        FullLoad = 1,
        DisplacementControl = 2
    }
    public enum LCNonLinear_DispType
    {
        ConjugateDisplacement = 1,
        MonitoredDisplacement = 2
    }
    public enum LCNonLinear_Monitor
    {
        DisplacementAtSpecifiedPoint = 1,
        GeneralizedDisplacement = 2
    }
    public enum LCNonLinear_DOF
    {
        U1 = 1,
        U2 = 2,
        U3 = 3,
        R1 = 4,
        R2 = 5,
        R3 = 6
    }
}
