using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using LibOptimization.Optimization;
using Sap2000Library;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Optimization
{
    public class DisplacementWithUnitRatioFunction : absObjectiveFunction
    {
        private S2KModel sModel;
        private LCNonLinear loadCase;
        private double targetRatio;

        private BusyOverlay busyOverlay;

        //private string readLoadCombo;
        public DisplacementWithUnitRatioFunction(S2KModel sModel, LCNonLinear loadCase, double targetRatio,
            BusyOverlay busyOverlay)
        {
            this.sModel = sModel;
            this.loadCase = loadCase;
            //this.readLoadCombo = readLoadCombo
            this.targetRatio = targetRatio;
            this.busyOverlay = busyOverlay;
        }

        /// <summary>
        /// The objective function must have a maximization target
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public override double F(List<double> x)
        {
            // Clears the previous run
            busyOverlay.SetIndeterminate("Clearing the results of the last iteration.");
            sModel.SteelDesignMan.DeleteResults();
            sModel.AnalysisMan.DeleteAllResults();
            sModel.Locked = false;

            // Sets the load case
            LCNonLinear_LoadApplicationOptions laOptions = loadCase.LoadApplicationOptions;
            laOptions.Displacement = x[0];
            loadCase.Status = null;

            sModel.LCMan.UpdateNLLoadApplication(loadCase, laOptions);

            // Runs the analysis
            busyOverlay.SetIndeterminate($"Solving the analysis for iteration. Displacement = {x[0]}.");
            sModel.AnalysisMan.RunAnalysis();

            // If the analysis failed
            loadCase.Status = null;
            if (loadCase.Status != LCStatus.Finished) return double.MaxValue;

            // Runs the code check
            busyOverlay.SetIndeterminate($"Performing the steel design. Displacement = {x[0]}.");
            sModel.SteelDesignMan.StartDesign();

            // Runs the steel design
            var results = sModel.SteelDesignMan.GetSummary();

            double maxRatio = results.Max(a => a.Ratio);
            double unbalance = Math.Abs(targetRatio - maxRatio);

            busyOverlay.LongReport_AddLine(
                $"{laOptions.Displacement,-20:0.0000}{maxRatio,-20:0.0000}{unbalance,-20:0.0000}");

            return unbalance;
        }

        public override List<double> Gradient(List<double> aa)
        {
            return null;
        }

        public override List<List<double>> Hessian(List<double> aa)
        {
            return null;
        }

        public override int NumberOfVariable()
        {
            return 1;
        }
    }
}