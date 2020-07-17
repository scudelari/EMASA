using System;
using System.Collections.Generic;
using Sap2000Library.DataClasses;
using Sap2000Library.Other;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class LPManager : SapManagerBase
    {
        internal LPManager(S2KModel model) : base(model) { }

        public List<string> GetAllNames()
        {
            int count = 0;
            string[] names = null;

            int ret = SapApi.LoadPatterns.GetNameList(ref count, ref names);
            if (ret != 0)
            {
                throw new S2KHelperException($"Could not get the list of load pattern names.", this);
            }

            if (count > 0) return new List<string>(names);
            else return new List<string>();
        }
        public List<LoadPatternData> GetAll()
        {
            List<string> names = GetAllNames();

            List<LoadPatternData> toRet = new List<LoadPatternData>();

            foreach (var item in names)
            {
                eLoadPatternType s2kPatType = eLoadPatternType.Dead;
                int ret = SapApi.LoadPatterns.GetLoadType(item, ref s2kPatType);
                if (ret != 0) throw new S2KHelperException($"Could not get the load pattern type for load pattern named {item}.");

                double swMult = 0;
                ret = SapApi.LoadPatterns.GetSelfWTMultiplier(item, ref swMult);
                if (ret != 0) throw new S2KHelperException($"Could not get the self-weight multiplier for load pattern named {item}.");

                toRet.Add(new LoadPatternData { Name = item, PatternType = (LoadPatternType)s2kPatType, SelfWeightMultiplier = swMult });
            }

            return toRet;
        }

        public void Add(LoadPatternData inLPData, bool AddAnalysisCase = false)
        {
            int ret = SapApi.LoadPatterns.Add(inLPData.Name, (eLoadPatternType)inLPData.PatternType, inLPData.SelfWeightMultiplier, AddAnalysisCase);
            if (ret != 0) throw new S2KHelperException($"Could not add load pattern named {inLPData.Name}.");
        }

        public void Delete(LoadPatternData inLPData)
        {
            Delete(inLPData.Name);
        }
        public bool Delete(string inName)
        {
            if (SapApi.LoadPatterns.Delete(inName) != 0)
                throw new S2KHelperException($"Could not delete load pattern called {inName}.", this);

            return true;
        }
        public bool DeleteAll()
        {
            return DeleteAll(null, null);
        }
        public bool DeleteAll(IProgress<ProgressData> ReportProgres)
        {
            return DeleteAll(ReportProgres, null);
        }
        public bool DeleteAll(LoadPatternType? type)
        {
            return DeleteAll(null, type);
        }
        public bool DeleteAll(IProgress<ProgressData> ReportProgress, LoadPatternType? type)
        {
            List<LoadPatternData> allPatterns = GetAll();
            
            for (int i = 0; i < allPatterns.Count; i++)
            {
                LoadPatternData item = allPatterns[i];

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i, allPatterns.Count));

                if (type.HasValue && type == item.PatternType) Delete(item);
            }

            // Must add a dummy one
            Add(new LoadPatternData { Name = "DEAD", PatternType = LoadPatternType.LTYPE_DEAD, SelfWeightMultiplier = 1 });

            return true;
        }
    }
}
