using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ProbQuantity;
using Prism.Mvvm;
using MathNet.Numerics;
using MathNet.Numerics.Differentiation;
using MathNet.Numerics.LinearAlgebra;

namespace Emasa_Optimizer
{
    public class NlOpt_ObjectiveFunction : BindableBase
    {
        public NlOpt_ObjectiveFunction() { }

        public void Reset()
        {
            _currentCalc_NlOptPoint = null;
            _currentCalc_GradientPoint = null;
            SolPointCalcType = NlOpt_Point_CalcTypeEnum.ObjectiveFunction;
        }

        public int NumberOfVariables => AppSS.I.Gh_Alg.InputDefs_VarCount;

        #region Current NlOpt_Point Calculation Buffer
        private NlOpt_Point_CalcTypeEnum _solPointCalcType;
        public NlOpt_Point_CalcTypeEnum SolPointCalcType
        {
            get => _solPointCalcType;
            set => SetProperty(ref _solPointCalcType, value);
        }

        private NlOpt_Point _currentCalc_NlOptPoint;
        public NlOpt_Point CurrentCalc_NlOptPoint
        {
            get => _currentCalc_NlOptPoint;
            set
            {
                // Resets the gradient
                _currentCalc_GradientPoint = null;

                // Saves the current
                _currentCalc_NlOptPoint = value;

                // Alerts about changes
                RaisePropertyChanged("CurrentCalc_NlOptPoint");
                RaisePropertyChanged("CurrentCalc_GradientPoint");
            }
        }

        private NlOpt_Point _currentCalc_GradientPoint;
        public NlOpt_Point CurrentCalc_GradientPoint
        {
            get => _currentCalc_GradientPoint;
            set
            {
                _currentCalc_GradientPoint = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        private int _gradientIteratorIndex = 0;
        private double CalculateFunctionPoint(double[] inPointVars)
        {
            // Starts the stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            // Creates a new solution point
            if (SolPointCalcType == NlOpt_Point_CalcTypeEnum.Gradient) AppSS.I.UpdateOverlayTopMessage($"Initializing Function Point.", $" [Gradient {_gradientIteratorIndex + 1}/{CurrentCalc_NlOptPoint.ObjectiveFunctionGradient.Length}]");
            else AppSS.I.UpdateOverlayTopMessage($"Initializing Function Point.");

            NlOpt_Point local_solPoint = new NlOpt_Point(AppSS.I.SolveMgr.CurrentCalculatingProblemConfig, inPointVars, SolPointCalcType);

            switch (SolPointCalcType)
            {
                // What is the point we are currently calculating
                case NlOpt_Point_CalcTypeEnum.ObjectiveFunction:
                    CurrentCalc_NlOptPoint = local_solPoint;
                    break;

                case NlOpt_Point_CalcTypeEnum.Gradient:
                default:
                    CurrentCalc_GradientPoint = local_solPoint;
                    break;
            }

            // Changes the status of the NlOpt_Point
            local_solPoint.Phase = NlOpt_Point_PhaseEnum.Grasshopper_Updating;

            // Updates the grasshopper geometry of the NlOpt_Point
            AppSS.I.UpdateOverlayTopMessage("Updating Grasshopper and Acquiring Geometry Data.", "");
            AppSS.I.Gh_Alg.UpdateGrasshopperGeometryAndGetResults(local_solPoint);

            // If it is a Fe Problem, runs the model
            if (AppSS.I.FeOpt.FeSolverType_Selected != FeSolverTypeEnum.NotFeProblem)
            {
                Stopwatch sw_Fe = Stopwatch.StartNew();

                // Changes the status of the solution point
                local_solPoint.Phase = NlOpt_Point_PhaseEnum.FiniteElement_Running;

                // Generates the Abstraction of the Finite Element Model
                AppSS.I.UpdateOverlayTopMessage("Generating Abstract Finite Element Definition.", "");
                local_solPoint.FeModel = new FeModel(local_solPoint);

                // Runs the FeModel and acquires the results, putting them inside the FeModel parameter of the NlOpt_Point class
                // ***** User interface messages are made within the function
                AppSS.I.FeSolver.RunAnalysisAndCollectResults(local_solPoint.FeModel);

                sw_Fe.Stop();
                local_solPoint.FeInputCalcOutputTimeSpan = sw_Fe.Elapsed;
            }

            // We have all the raw outputs, so we can initialize the Quantity Value's Outputs
            AppSS.I.UpdateOverlayTopMessage("Initializing the Problem Quantities.", "");
            local_solPoint.InitializeProblemQuantityOutputs();

            // Calculates the final value of the objective function based on the selected quantity values
            AppSS.I.UpdateOverlayTopMessage("Calculating the Objective Function.", "");
            local_solPoint.CalculateObjectiveFunctionResult();
            
            // Outputs and Constraints have already been initialized, so they should be readily available.

            // Stops the watch and saves the timespan used to handle the timespan
            sw.Stop();
            local_solPoint.CalculateTimeSpan = sw.Elapsed;

            return local_solPoint.ObjectiveFunctionEval;
        }

        public double NlOptEntryPoint_ObjectiveFunction(double[] inPointVars, double[] inGradient = null)
        {
            // IMPORTANT: Errors are reported by the Unhandled Exceptions that "escape" this function

            #region Abort optimization before the calculation of the solution point
            // There was an error in the last evaluation - breaks
            if (inPointVars.Any(double.IsNaN))
            {
                AppSS.I.SolveMgr.CurrentCalculatingProblemConfig.NlOptSolverWrapper.NLOptMethod.ForceStop();
                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "NlOpt Solver gave Not A Number (NaN) as input to the objective function.");
            }
            // Should we cancel the execution - Forces the NlOpt to Stop
            if (AppSS.I.SolveMgr.CancelSource.IsCancellationRequested)
            {
                AppSS.I.SolveMgr.CurrentCalculatingProblemConfig.NlOptSolverWrapper.NLOptMethod.ForceStop();
                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Forced_Stop, "User stopped the solver.");
            } 
            #endregion

            // Sends the input array to the currently calculating problem config!
            AppSS.I.SolveMgr.CurrentCalculatingProblemConfig.CurrentlyCalculatingInput = inPointVars.Clone() as double[];

            Stopwatch swPointTotalEval = Stopwatch.StartNew();
            try
            {
                // Creates and calculates a new solution point -> putting it in the given reference location
                SolPointCalcType = NlOpt_Point_CalcTypeEnum.ObjectiveFunction;
                CalculateFunctionPoint(inPointVars);

                // Gradient Necessary ?
                if (inGradient != null)
                {
                    Stopwatch gradSw = Stopwatch.StartNew();

                    CurrentCalc_NlOptPoint.Phase = NlOpt_Point_PhaseEnum.Gradients_Running;

                    // Initializes the gradient variables
                    CurrentCalc_NlOptPoint.HasGradient = true;

                    // Creates a new FiniteDifferences Manager
                    NumericalDerivative nd = new NumericalDerivative(AppSS.I.NlOptOpt.FiniteDiff_PointsPerPartialDerivative, AppSS.I.NlOptOpt.FiniteDiff_PointsPerPartialDerivativeCenter);

                    // Fills the gradient information 
                    SolPointCalcType = NlOpt_Point_CalcTypeEnum.Gradient;
                    for (_gradientIteratorIndex = 0; _gradientIteratorIndex < CurrentCalc_NlOptPoint.ObjectiveFunctionGradient.Length; _gradientIteratorIndex++)
                    {
                        // Note: it will call the  CalculateFunctionPoint, which will assign the solution point calculated to the CurrentCalc_GradientPoint parameter
                        CurrentCalc_NlOptPoint.ObjectiveFunctionGradient[_gradientIteratorIndex] = nd.EvaluatePartialDerivative(CalculateFunctionPoint, CurrentCalc_NlOptPoint.InputValuesAsDoubleArray.Clone() as double[], _gradientIteratorIndex, 1, CurrentCalc_NlOptPoint.ObjectiveFunctionEval);

                        // Saves the solution point that was calculated to get the partial derivative
                        // Used when the constraint function is called
                        CurrentCalc_NlOptPoint.GradientSolutionPoints[_gradientIteratorIndex] = CurrentCalc_GradientPoint;
                    }

                    // Saves the copy to the return parameter
                    double[] gradArray = CurrentCalc_NlOptPoint.ObjectiveFunctionGradient.Clone() as double[];
                    gradArray.CopyTo(inGradient, 0);

                    gradSw.Stop();
                    CurrentCalc_NlOptPoint.TotalGradientTimeSpan = gradSw.Elapsed;
                }
            }
            catch (Exception e)
            {
                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "Failed in the calculation of the Function Point.", e);
            }

            // Checks for errors in the calculation of the objective function criteria
            if (double.IsNaN(CurrentCalc_NlOptPoint.ObjectiveFunctionEval)) throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "The objective function calculated NaN as result.");

            // Calculates every constraint in the function points
            CurrentCalc_NlOptPoint.CalculateAllConstraintResults();

            // Stores it in the list of calculated solutions
            AppSS.I.UpdateOverlayTopMessage("Adding the Function Point to the Configuration's List.");
            AppSS.I.SolveMgr.CurrentCalculatingProblemConfig.AddFunctionPoint(CurrentCalc_NlOptPoint);

            swPointTotalEval.Stop(); 
            CurrentCalc_NlOptPoint.TotalIterationTimeSpan = swPointTotalEval.Elapsed;

            CurrentCalc_NlOptPoint.Phase = NlOpt_Point_PhaseEnum.Ended;

            // One or more of the limits exploded
            AppSS.I.UpdateOverlayTopMessage("Checking the Optimization's Stop Criteria.");
            foreach (StopCriteriaStatus stopStatus in CurrentCalc_NlOptPoint.StopCriteriaStatuses)
            {
                if (stopStatus.IsActive && stopStatus.LimitReached)
                {
                    switch (stopStatus.StopCriteriaType)
                    {
                        case StopCriteriaTypeEnum.Time:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Maximum total time for the optimization has been reached.");

                        case StopCriteriaTypeEnum.Iterations:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Maximum total number of evaluations for the optimization has been reached.");

                        case StopCriteriaTypeEnum.FunctionValue:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Stop value of the objective function has been reached.");

                        case StopCriteriaTypeEnum.FunctionAbsoluteChange:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum absolute delta of the objective function value has been reached.");

                        case StopCriteriaTypeEnum.FunctionRelativeChange:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum relative delta of the objective function value has been reached.");

                        case StopCriteriaTypeEnum.ParameterAbsoluteChange:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum absolute delta of the input parameter value has been reached. Parameter: {stopStatus.Name}");

                        case StopCriteriaTypeEnum.ParameterRelativeChange:
                            throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum relative delta of the input parameter value has been reached. Parameter: {stopStatus.Name}");

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return CurrentCalc_NlOptPoint.ObjectiveFunctionEval;
        }
    }
}
