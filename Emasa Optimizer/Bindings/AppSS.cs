using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.Opt;
using Prism.Commands;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Bindings
{
    /// <summary>
    /// Application-Wide Singleton Static Class
    /// </summary>
    public class AppSS : BindableSingleton<AppSS>
    {
        private AppSS() { }

        private ProblemQuantityManager _probQuantMgn;
        public ProblemQuantityManager ProbQuantMgn { get => _probQuantMgn; }

        private FeOptions _feOpt;
        public FeOptions FeOpt { get => _feOpt; }

        private ScreenShotOptions _screenShotOpt;
        public ScreenShotOptions ScreenShotOpt
        {
            get => _screenShotOpt;
            set => SetProperty(ref _screenShotOpt, value);
        }


        private GhAlgorithm _gh_alg;
        public GhAlgorithm Gh_Alg { get => _gh_alg; }

        private NlOpt_Options _nlOptOpt;
        public NlOpt_Options NlOptOpt { get => _nlOptOpt; }

        private SolveManager _solveMgr;
        public SolveManager SolveMgr
        {
            get => _solveMgr;
            set => SetProperty(ref _solveMgr, value);
        }

        private NlOpt_ObjectiveFunction _nlOptObjFunc;
        public NlOpt_ObjectiveFunction NlOptObjFunc { get => _nlOptObjFunc; }

        private ProblemConfig_Options _pcOpt;
        public ProblemConfig_Options PcOpt { get => _pcOpt; }

        /// <summary>
        /// This is the Entry Point Constructor
        /// </summary>
        public override void SetOrReset()
        {
            // Creates the Finite Element Options
            _feOpt = new FeOptions();

            // Creates the screenshot options
            _screenShotOpt = new ScreenShotOptions();

            // Creates the Problem Quantity Manager
            _probQuantMgn = new ProblemQuantityManager();

            // Initializes Grasshopper link
            _gh_alg = new GhAlgorithm();

            // Initializes the solver manager
            _solveMgr = new SolveManager();
            
            // Creates the NlOpt Options
            _nlOptOpt = new NlOpt_Options();

            // Creates the Problem Config Options
            _pcOpt = new ProblemConfig_Options();

            // Creates the Objective Function Handler
            _nlOptObjFunc = new NlOpt_ObjectiveFunction();
        }
        
        private FeSolverBase _feSolver = null;
        public FeSolverBase FeSolver
        {
            get => _feSolver;
            set => SetProperty(ref _feSolver, value);
        }

        #region Wpf Calculating General Exhibitions
        private string _overlay_TopMessage_GradientReportMemory;
        private string _overlay_TopMessage;
        public string Overlay_TopMessage
        {
            get => _overlay_TopMessage;
            set => SetProperty(ref _overlay_TopMessage, value);
        }
        /// <summary>
        /// Updates the message in the top of the overlay.
        /// </summary>
        /// <param name="inCurrentStatus">The current message to display.</param>
        /// <param name="inGradientReport">A gradient message to append to the message. Null resets to nothing. "" means keep last and string updates the message.</param>
        public void UpdateOverlayTopMessage(string inCurrentStatus, string inGradientReport = null)
        {
            if (inGradientReport == null) _overlay_TopMessage_GradientReportMemory = "";
            else if (inGradientReport != "") _overlay_TopMessage_GradientReportMemory = inGradientReport;

            Overlay_TopMessage =
$@"Working with problem configuration #{SolveMgr.CurrentCalculatingProblemConfig.Index:D5}. Evaluating Function Point #{SolveMgr.CurrentCalculatingProblemConfig.TotalPointCount}.
{inCurrentStatus} {_overlay_TopMessage_GradientReportMemory}";
        }

        private Visibility _overlay_ProblemConfigDetailsVisible = Visibility.Collapsed;
        public Visibility Overlay_ProblemConfigDetailsVisible
        {
            get => _overlay_ProblemConfigDetailsVisible;
            set => SetProperty(ref _overlay_ProblemConfigDetailsVisible, value);
        }

        private int _overlay_progressBarMaximum;
        public int Overlay_ProgressBarMaximum
        {
            get => _overlay_progressBarMaximum;
            set => SetProperty(ref _overlay_progressBarMaximum, value);
        }
        private int _overlay_progressBarCurrent;
        public int Overlay_ProgressBarCurrent
        {
            get => _overlay_progressBarCurrent;
            set => SetProperty(ref _overlay_progressBarCurrent, value);
        }
        private bool _overlay_ProgressBarIndeterminate = true;
        public bool Overlay_ProgressBarIndeterminate
        {
            get => _overlay_ProgressBarIndeterminate;
            set => SetProperty(ref _overlay_ProgressBarIndeterminate, value);
        }
        #endregion

    }

}
