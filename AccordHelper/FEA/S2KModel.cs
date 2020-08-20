using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Items;
using AccordHelper.Opt;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.SapObjects;

namespace AccordHelper.FEA
{
    public class S2KModel : FeModelBase
    {
        public override string SubDir { get; } = "Sap2000";

        /// <summary>
        /// Defines a Sap2000 Model.
        /// </summary>
        /// <param name="inModelFolder">The target folder for the analysis</param>
        public S2KModel(string inModelFolder, ProblemBase inProblem) : base(inModelFolder, "model.s2k", inProblem)
        {

        }

        public override void InitializeSoftware(bool inIsSectionSelection = false)
        {
            // Starts a new SAP2000 instance if it isn't available in the Singleton
            Sap2000Library.S2KModel.InitSingleton_RunningOrNew(UnitsEnum.N_m_C);

            // Opens a new Blank Model
            Sap2000Library.S2KModel.SM.NewModelBlank(inModelUnits: UnitsEnum.N_m_C);
        }

        public override void ResetSoftwareData()
        {
            throw new NotImplementedException();
        }

        public override void CloseApplication()
        {
            throw new NotImplementedException();
        }

        public override void RunAnalysisAndGetResults(List<ResultOutput> inDesiredResults, int inEigenvalueBucklingMode = 0, double inEigenvalueBucklingScaleFactor = Double.NaN)
        {
            // Sets the run options
            Sap2000Library.S2KModel.SM.AnalysisMan.SetAllNotToRun();
            Sap2000Library.S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);
            Sap2000Library.S2KModel.SM.AnalysisMan.RunAnalysis();
        }

    }
}
