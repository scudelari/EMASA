using System;
using System.Collections.Generic;
using Sap2000Library.Other;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class CombManager : SapManagerBase
    {
        internal CombManager(S2KModel model) : base(model) { }

        public List<string> GetAllNames()
        {
            int count = 0;
            string[] names = null;

            int ret = SapApi.RespCombo.GetNameList(ref count, ref names);
            if (ret != 0)
            {
                throw new S2KHelperException($"Could not get the list of comnination names.", this);
            }
            
            if (count > 0) return new List<string>(names);
            else return new List<string>();
        }

        public bool AddComb(string inCombName, List<(string CaseName, int ScaleFactor)> inCaseList, ResponseCombinationType? inCombType = null)
        {
            int ret = SapApi.RespCombo.Add(inCombName, (int)(inCombType ?? ResponseCombinationType.LinearAdditive));
            if (ret != 0) throw new S2KHelperException($"Could not add combination named {inCombName}.");

            if (inCaseList != null && inCaseList.Count > 0)
            {
                foreach (var (CaseName, ScaleFactor) in inCaseList)
                {
                    eCNameType tempType = eCNameType.LoadCase;
                    ret = SapApi.RespCombo.SetCaseList(inCombName, ref tempType, CaseName, ScaleFactor);
                    if (ret != 0) throw new S2KHelperException($"Could not add item {CaseName} with the scale factor {ScaleFactor} to combination named {inCombName}.", this);
                }
            }

            return true;
        }

        public bool Delete(string inCombName)
        {
            int ret = SapApi.RespCombo.Delete(inCombName);
            if (ret != 0)
            {
                throw new S2KHelperException($"Could not delete the combination named {inCombName}.", this);
            }
            return true;
        }
        public bool DeleteAll(IProgress<ProgressData> ReportProgress = null)
        {
            List<string> names = GetAllNames();

            for (int i = 0; i < names.Count; i++)
            {
                string item = names[i];

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,names.Count));

                Delete(item);
            }

            return true;
        }

        public bool Steel_SetCombAutoGenerate(bool inOption)
        {
            if (SapApi.DesignSteel.SetComboAutoGenerate(inOption) != 0)
            {
                throw new S2KHelperException($"Could not set the automatic generation of combinations for STEEL to {inOption}");
            }
            return true;
        }

    }
}
