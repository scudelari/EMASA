using System;

namespace Sap2000Library.Managers
{
    public class AnalysisManager : SapManagerBase
    {
        internal AnalysisManager(S2KModel model) : base(model) { }

        public bool ModelLocked
        {
            get
            {
                try
                {
                    return SapApi.GetModelIsLocked();
                }
                catch (Exception e)
                {

                    throw new S2KHelperException("Coudl not get the [model locked] flag.", e);
                }
            }
            set
            {
                int ret = SapApi.SetModelIsLocked(value);
                if (ret != 0) throw new S2KHelperException("Could not set the [model locked] flag.");
            }
        }

        public void DeleteAllResults()
        {
            if (SapApi.Analyze.DeleteResults("", true) != 0) throw new S2KHelperException($"Could not delete the results of all load cases.");
        }

        public void DeleteResultsOfLoadCase(string inLoadCaseName)
        {
            if (SapApi.Analyze.DeleteResults(inLoadCaseName, false) != 0) throw new S2KHelperException($"Could not delete the results of the {inLoadCaseName} load case.");
        }
        public void SetCaseRunFlag(string inCaseName, bool run)
        {
            if (SapApi.Analyze.SetRunCaseFlag(inCaseName, run, false) != 0) throw new S2KHelperException($"Could not set the run flag of load case {inCaseName} to {run}.");
        }
        public void SetAllToRun()
        {
            if (SapApi.Analyze.SetRunCaseFlag("", true, true) != 0) throw new S2KHelperException($"Could not set the run flag all load cases to true.");
        }
        public void SetAllNotToRun()
        {
            if (SapApi.Analyze.SetRunCaseFlag("", false, true) != 0) throw new S2KHelperException($"Could not set the run flag all load cases to false.");
        }

        /// <summary>
        /// This function will run the analysis in SAP2000 and lock the current thread. Do not call from the UI thread.
        /// </summary>
        public void RunAnalysis()
        {
            if (SapApi.Analyze.RunAnalysis() != 0) throw new S2KHelperException($"Could not run the analysis.");
        }

        public void SetActiveDoF(bool[] inDoF)
        {
            if (0 != SapApi.Analyze.SetActiveDOF(ref inDoF))
                throw new Exception($"Could not set the active DoF's of the Analysis");
        }
    }
}
