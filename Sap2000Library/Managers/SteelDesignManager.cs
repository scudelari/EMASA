using System.Collections.Generic;
using Sap2000Library.DataClasses;

namespace Sap2000Library.Managers
{
    public class SteelDesignManager : SapManagerBase
    {
        internal SteelDesignManager(S2KModel model) : base(model) { }

        public void DeleteResults()
        {
            if (0 != SapApi.DesignSteel.DeleteResults()) throw new S2KHelperException("Could not delete the steel design results.");
        }
        public void StartDesign()
        {
            if (0 != SapApi.DesignSteel.StartDesign()) throw new S2KHelperException("Could not start the steel design.");
        }

        public bool ResultsAvailable
        {
            get { return SapApi.DesignSteel.GetResultsAvailable(); }
        }

        public void SetCombForStrengthDesign(string CombName, bool Selected)
        {
            if (0 != SapApi.DesignSteel.SetComboStrength(CombName, Selected)) throw new S2KHelperException($"Could not change the strength steel design selection status of combination {CombName} to {Selected}.");
        }
        public void SetCombForDeflectionDesign(string CombName, bool Selected)
        {
            if (0 != SapApi.DesignSteel.SetComboDeflection(CombName, Selected)) throw new S2KHelperException($"Could not change the deflection steel design selection status of combination {CombName} to {Selected}.");
        }

        public List<string> GetStrengthDesignCombList()
        {
            int numberItems = 0;
            string[] combs = null;

            if (0 != SapApi.DesignSteel.GetComboStrength(ref numberItems, ref combs)) throw new S2KHelperException($"Could not get the list of combinations that are used for steel strength design.");

            if (combs == null || combs.Length == 0) return new List<string>();

            return new List<string>(combs);
        }
        public List<string> GetDeflectionDesignCombList()
        {
            int numberItems = 0;
            string[] combs = null;

            if (0 != SapApi.DesignSteel.GetComboDeflection(ref numberItems, ref combs)) throw new S2KHelperException($"Could not get the list of combinations that are used for steel deflection design.");

            if (combs == null || combs.Length == 0) return new List<string>();

            return new List<string>(combs);
        }

        public void SetCombAutoGenerate(bool AutoGenerate)
        {
            if (0 != SapApi.DesignSteel.SetComboAutoGenerate(AutoGenerate)) throw new S2KHelperException($"Could not change the steel design automatic generate combinations flag to {AutoGenerate}.");
        }
        public void DeselectAllCombinations()
        {
            List<string> strengthCombs = GetStrengthDesignCombList();
            foreach (string item in strengthCombs) SetCombForStrengthDesign(item, false);

            List<string> deflectCombs = GetDeflectionDesignCombList();
            foreach (string item in deflectCombs) SetCombForDeflectionDesign(item, false);
        }

        public List<SteelDesignSummaryResults> GetSummary(string GroupName = "ALL") 
        {
            if (!ResultsAvailable) throw new S2KHelperException("The steel design results are NOT available.");

            int numberItems = 0;
            string[] frameName = null;
            double[] ratio = null;
            int[] ratioType = null;
            double[] location = null;
            string[] combName = null;
            string[] errorSummary = null;
            string[] warningSummary = null;

            List<SteelDesignSummaryResults> toret = new List<SteelDesignSummaryResults>();

            if (0 != SapApi.DesignSteel.GetSummaryResults(GroupName,
                ref numberItems, ref frameName, ref ratio, ref ratioType, ref location, ref combName, ref errorSummary, ref warningSummary,
                SAP2000v1.eItemType.Group)) throw new S2KHelperException("Could not get the steel design results summary.");

            for (int i = 0; i < numberItems; i++)
            {
                SteelDesignSummaryResults newItem = new SteelDesignSummaryResults()
                {
                    FrameName = frameName[i],
                    CombName = combName[i],
                    ErrorSummary = errorSummary[i],
                    WarningSummary = warningSummary[i],
                    Location = location[i],
                    Ratio = ratio[i],
                    RatioType = (SteelDesignRatioType)ratioType[i]
                };

                toret.Add(newItem);
            }

            return toret;
        }
    }
}
