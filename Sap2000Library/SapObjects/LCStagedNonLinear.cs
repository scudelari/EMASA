using System.Collections.Generic;
using Sap2000Library.DataClasses;
using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class LCStagedNonLinear : LCNonLinear
    {
        private List<LoadCaseNLStagedStageData> stageList;
        internal List<LoadCaseNLStagedStageData> StageList
        {
            get { return stageList; }
            set { stageList = value; }
        }

        public LCStagedNonLinear(LCManager lCManager) : base(lCManager)
        {
            // Sets the type to Staged Construction MultiLinear
            NLSubType = LCNonLinear_SubType.StagedConstruction;
        }
    }
}
