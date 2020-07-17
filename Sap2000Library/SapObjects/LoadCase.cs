using Sap2000Library.Managers;
using SAP2000v1;

namespace Sap2000Library.SapObjects
{
    public class LoadCase
    {
        protected LCManager owner = null;
        internal LoadCase(LCManager lCManager)
        {
            owner = lCManager;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private LCStatus? _lCStatus;
        public LCStatus? Status
        {
            get
            {
                if (!_lCStatus.HasValue)
                {
                    int count = 0;
                    string[] names = null;
                    int[] status = null;

                    int ret = owner.SapApi.Analyze.GetCaseStatus(ref count, ref names, ref status);

                    if (count == 0 || names.Length == 0 || status.Length == 0 || ret != 0)
                    {
                        _lCStatus = null;
                    }
                    else
                    {
                        _lCStatus = null;
                        for (int i = 0; i < count; i++)
                        {
                            if (names[i] == Name)
                            {
                                _lCStatus = (LCStatus)status[i];
                                break;
                            }
                        }
                    }
                }
                return _lCStatus;
            }
            set { _lCStatus = value; }
        }

        private bool? _runFlag;
        public bool? RunFlag
        {
            get
            {
                if (!_runFlag.HasValue)
                {
                    int count = 0;
                    string[] names = null;
                    bool[] flag = null;

                    int ret = owner.SapApi.Analyze.GetRunCaseFlag(ref count, ref names, ref flag);

                    if (count == 0 || names.Length == 0 || flag.Length == 0 || ret != 0)
                    {
                        _runFlag = null;
                    }
                    else
                    {
                        _runFlag = null;
                        for (int i = 0; i < count; i++)
                        {
                            if (names[i] == Name)
                            {
                                _runFlag = flag[i];
                                break;
                            }
                        }
                    }
                }
                return _runFlag;
            }
            set { _runFlag = value; }
        }

        private LoadCaseTypeEnum? _caseType;
        public LoadCaseTypeEnum CaseType
        {
            get
            {
                if (!_caseType.HasValue) GetFromSAP2000();
                return _caseType.Value;
            }
            set { _caseType = value; }
        }

        private int? _subType;
        public int SubType
        {
            get
            {
                if (!_subType.HasValue) GetFromSAP2000();
                return _subType.Value;
            }
            set { _subType = value; }
        }

        private LoadCaseDesignType? _lcDesignType;
        public LoadCaseDesignType DesignType
        {
            get
            {
                if (!_lcDesignType.HasValue) GetFromSAP2000();
                return _lcDesignType.Value;
            }
            set { _lcDesignType = value; }
        }

        private int? _designTypeOption;
        public int DesignTypeOption
        {
            get
            {
                if (!_designTypeOption.HasValue) GetFromSAP2000();
                return _designTypeOption.Value;
            }
            set { _designTypeOption = value; }
        }

        private int? _auto;
        public int Auto
        {
            get
            {
                if (!_auto.HasValue) GetFromSAP2000();
                return _auto.Value;
            }
            set { _auto = value; }
        }

        private void GetFromSAP2000() 
        {
            eLoadCaseType CaseType = default;
            int SubType = default;
            eLoadPatternType DesignType = default;
            int DesignTypeOption = default;
            int Auto = default;

            if (0 != owner.SapApi.LoadCases.GetTypeOAPI_2(Name, ref CaseType, ref SubType, ref DesignType, ref DesignTypeOption, ref Auto))
                throw new S2KHelperException($"Could not get parameters for Load Case called {Name}");

            this.CaseType = (LoadCaseTypeEnum)CaseType;
            this.SubType = SubType;
            this.DesignType = (LoadCaseDesignType)DesignType;
            this.DesignTypeOption = DesignTypeOption;
            this.Auto = Auto;
        }
    }
}
