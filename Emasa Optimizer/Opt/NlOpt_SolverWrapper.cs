extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.WpfResources;
using NLoptNet;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt
{
    public class NlOpt_SolverWrapper : BindableBase
    {
        [NotNull] private readonly ProblemConfig _owner;
        public NlOpt_SolverWrapper([NotNull] ProblemConfig inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            // Starts with a message
            OptimizeTerminationException = new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.NotStarted, "Optimization not started.");
        }

        private NLoptSolver _nLOptMethod;
        public NLoptSolver NLOptMethod
        {
            get => _nLOptMethod;
            private set => SetProperty(ref _nLOptMethod, value);
        }
        
        #region Time Control
        private TimeSpan _nlOpt_TotalSolveTimeSpan = TimeSpan.Zero;
        public TimeSpan NlOpt_TotalSolveTimeSpan
        {
            get => _nlOpt_TotalSolveTimeSpan;
            set => SetProperty(ref _nlOpt_TotalSolveTimeSpan, value);
        }

        private DateTime _nlOpt_OptimizationStartTime;
        public DateTime NlOpt_OptimizationStartTime
        {
            get => _nlOpt_OptimizationStartTime;
            set => SetProperty(ref _nlOpt_OptimizationStartTime, value);
        }
        #endregion

        #region Termination message and status
        private NlOpt_OptimizeTerminationException _optimizeTerminationException;
        public NlOpt_OptimizeTerminationException OptimizeTerminationException
        {
            get => _optimizeTerminationException;
            set
            {
                SetProperty(ref _optimizeTerminationException, value); 
                RaisePropertyChanged("Wpf_TerminationCodeDescription");
                RaisePropertyChanged("Wpf_TerminationCodeLongDescription");
            }
        }

        public string Wpf_TerminationCodeDescription => ListDescSH.I.NlOpt_TerminationCodeEnumDescriptions[OptimizeTerminationException.OptimizeTerminationCode];
        public string Wpf_TerminationCodeLongDescription 
        {
            get
            {
                switch (OptimizeTerminationException.OptimizeTerminationCode)
                {
                    case NlOpt_OptimizeTerminationCodeEnum.Forced_Stop:
                    case NlOpt_OptimizeTerminationCodeEnum.Success:
                    case NlOpt_OptimizeTerminationCodeEnum.Converged:
                    case NlOpt_OptimizeTerminationCodeEnum.NotStarted:
                    case NlOpt_OptimizeTerminationCodeEnum.Optimizing:
                        return OptimizeTerminationException.Message;

                    case NlOpt_OptimizeTerminationCodeEnum.Failed:
                        return OptimizeTerminationException.CompleteMessageWithStack;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endregion

        #region Retry if FEA did not converge

        private int _retryLimit = 10;
        private Visibility _feaNotConvergedMessage_Visibility = Visibility.Collapsed;
        public Visibility FeaNotConvergedMessage_Visibility
        {
            get => _feaNotConvergedMessage_Visibility;
            set => SetProperty(ref _feaNotConvergedMessage_Visibility, value);
        }
        private string _feaNotConvergedMessage;
        public string FeaNotConvergedMessage
        {
            get => _feaNotConvergedMessage;
            set
            {
                SetProperty(ref _feaNotConvergedMessage, value);
                if (_feaNotConvergedMessage == null) FeaNotConvergedMessage_Visibility = Visibility.Collapsed;
                else FeaNotConvergedMessage_Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Actions!
        public void RunOptimization()
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Starts with the default boundaries and start positions
            _owner.LowerBounds = AppSS.I.Gh_Alg.InputDefs_LowerBounds;
            _owner.UpperBounds = AppSS.I.Gh_Alg.InputDefs_UpperBounds;
            double[] startValues = AppSS.I.Gh_Alg.GetInputStartPosition();

            void lf_RunOptimizationWithStartAndBoundaries()
            {
                try
                {
                    // Sets the message
                    OptimizeTerminationException = new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Optimizing, "Optimizing.");

                    // Checks if we have equality of inequality constraints
                    bool hasEqualConstraint = false;
                    bool hasUnequalConstraint = false;
                    NlOpt_OptimizationStartTime = DateTime.Now;

                    // Sets up the constraints
                    foreach (ProblemQuantity constraintQuantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
                    {
                        // Regardless of the constraint type, it will always point to the same function
                        switch (constraintQuantity.ConstraintObjective)
                        {
                            case Quantity_ConstraintObjectiveEnum.EqualTo:
                                hasEqualConstraint = true;
                                break;

                            case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                            case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                                hasUnequalConstraint = true;
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    using (NLOptMethod = new NLoptSolver(AppSS.I.NlOptOpt.NlOptSolverType,
                        (uint)AppSS.I.Gh_Alg.InputDefs_VarCount,
                        AppSS.I.NlOptOpt.UseLagrangian, hasEqualConstraint, hasUnequalConstraint))
                    {
                        #region Setting the population size for algorithms that require populations
                        if (AppSS.I.NlOptOpt.SolverNeedsPopulationSize == Visibility.Visible && AppSS.I.NlOptOpt.IsOn_PopulationSize) NLOptMethod.SetPopulationSize((uint)AppSS.I.NlOptOpt.PopulationSize);
                        #endregion

                        #region Setting up the problem itself
                        // Boundaries of the input variables
                        NLOptMethod.SetLowerBounds(_owner.LowerBounds);
                        NLOptMethod.SetUpperBounds(_owner.UpperBounds);

                        // The objective function is given by the wrapper
                        NLOptMethod.SetMinObjective(AppSS.I.NlOptObjFunc.NlOptEntryPoint_ObjectiveFunction);
                        #endregion

                        // Sets the constraints as given by the quantity selections
                        foreach (ProblemQuantity constraintQuantity in AppSS.I.ProbQuantMgn.WpfProblemQuantities_Constraint.OfType<ProblemQuantity>())
                        {
                            // Regardless of the constraint type, it will always point to the same function
                            switch (constraintQuantity.ConstraintObjective)
                            {
                                case Quantity_ConstraintObjectiveEnum.EqualTo:
                                    NLOptMethod.AddEqualZeroConstraint(constraintQuantity.NlOptEntryPoint_ConstraintFunction, constraintQuantity.ConstraintTolerance);
                                    break;

                                case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                                case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                                    NLOptMethod.AddLessOrEqualZeroConstraint(constraintQuantity.NlOptEntryPoint_ConstraintFunction, constraintQuantity.ConstraintTolerance);
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        // Gets the start value array
                        // double[] startValues = AppSS.I.Gh_Alg.GetInputStartPosition();

                        // Runs the optimization - The idea is that it will be stopped by the exceptions that are thrown
                        NloptResult tempResult = NLOptMethod.Optimize(startValues, out double? bestEval);

                        // Treats the exceptionless termination of the optimization
                        switch (tempResult)
                        {
                            case NloptResult.FAILURE:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "Unknown general NlOpt failure.");

                            case NloptResult.INVALID_ARGS:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "NlOpt failure - Invalid arguments.");

                            case NloptResult.OUT_OF_MEMORY:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "NlOpt failure - Out of memory.");

                            case NloptResult.ROUNDOFF_LIMITED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "NlOpt failure - Round off limited.");

                            case NloptResult.FORCED_STOP:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Forced_Stop, "User stopped the solver.");

                            case NloptResult.SUCCESS:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Success, "NlOpt general success.");

                            case NloptResult.STOPVAL_REACHED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Stop value of the objective function has been reached.");

                            case NloptResult.FTOL_REACHED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum absolute delta of the objective function value has been reached.");

                            case NloptResult.XTOL_REACHED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Minimum absolute delta of the input parameter value has been reached.");

                            case NloptResult.MAXEVAL_REACHED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Maximum total number of evaluations for the optimization has been reached.");

                            case NloptResult.MAXTIME_REACHED:
                                throw new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Converged, $"Maximum total time for the optimization has been reached.");

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                catch (NlOpt_OptimizeTerminationException nlTerm)
                {
                    // This is reached when the optimization finishes for an expected reason
                    OptimizeTerminationException = nlTerm;
                }
                catch (Exception e) // Any other exception - errors
                {
                    // Just saves a wrapped version of the general exception
                    OptimizeTerminationException = new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.Failed, "Unexpected failure.", e);
                }
            }

            // If we cannot converge the FEA, we will clear the solution and retry by cropping the search space
            try
            {
                int retryCounter = 0;
                while (true)
                {
                    // Attempts an optimization
                    lf_RunOptimizationWithStartAndBoundaries();

                    if (!(OptimizeTerminationException.InnerException is FeSolverException feSolverEx && feSolverEx.SolutionNotConverged)) break;
                    // Continues if it failed because the FeSolver did not converge

                    // Gets new Boundaries and Start Positions
                    double[] newLower = new double[startValues.Length];
                    double[] newUpper = new double[startValues.Length];
                    double[] newStart = new double[startValues.Length];

                    // Note: The failed point is *NOT* added to the list - it is stored in the NlOpt_ObjectiveFunction.CurrentCalc_NlOptPoint
                    if (_owner.TotalPointCount == 0) // Failed in the first point
                    {
                        for (int i = 0; i < startValues.Length; i++)
                        {
                            double distToLower = Math.Abs(startValues[i] - _owner.LowerBounds[i]);
                            double distToUpper = Math.Abs(startValues[i] - _owner.UpperBounds[i]);

                            if (distToLower == distToUpper)
                            {
                                throw new Exception($"Parameter {AppSS.I.Gh_Alg.GetInputParameterNameByIndex(i)}: Cannot retry with different search boundaries because it failed in the first iteration and the current start value is at the middle of the search space.");
                            }

                            if (distToLower > distToUpper)
                            {
                                newLower[i] = _owner.LowerBounds[i];
                                newUpper[i] = startValues[i];
                            }
                            else
                            {
                                newLower[i] = startValues[i];
                                newUpper[i] = _owner.UpperBounds[i];
                            }
                            newStart[i] = ((newUpper[i] - newLower[i]) / 2d) + newLower[i];
                        }
                    }
                    else // Failed in another point
                    {
                        for (int i = 0; i < startValues.Length; i++)
                        {
                            // Gets the range that works
                            double minWorkingRange = _owner.Wpf_FunctionPoints.OfType<NlOpt_Point>().Select(p => p.InputValuesAsDoubleArray[i]).Min();
                            double maxWorkingRange = _owner.Wpf_FunctionPoints.OfType<NlOpt_Point>().Select(p => p.InputValuesAsDoubleArray[i]).Max();

                            // The value of this input in the failed point is within the range that should be valid, so no change in this parameter
                            if (AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] >= minWorkingRange && AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] <= maxWorkingRange)
                            {
                                newLower[i] = _owner.LowerBounds[i];
                                newUpper[i] = _owner.UpperBounds[i];
                                newStart[i] = startValues[i];
                            }
                            else
                            {
                                // The attempted value is really close to the lower boundary
                                if (AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] < (_owner.LowerBounds[i] + Math.Abs(_owner.LowerBounds[i] * 0.05d)))
                                {
                                    newLower[i] = (_owner.LowerBounds[i] + Math.Abs(_owner.LowerBounds[i] * 0.05d));
                                    newUpper[i] = _owner.UpperBounds[i];
                                }
                                else if (AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] > (_owner.UpperBounds[i] - Math.Abs(_owner.UpperBounds[i] * 0.05d)))
                                { // attempted value is really close to the upper boundary 
                                    newLower[i] = _owner.LowerBounds[i];
                                    newUpper[i] = (_owner.UpperBounds[i] - Math.Abs(_owner.UpperBounds[i] * 0.05d));
                                }
                                else if (AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] > maxWorkingRange)
                                { // Failed is higher than the working limit
                                    newLower[i] = _owner.LowerBounds[i];
                                    newUpper[i] = AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] - Math.Abs(AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] * 0.05d);
                                }
                                else
                                { // Failed is lower than the working limit

                                    newLower[i] = AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] + Math.Abs(AppSS.I.NlOptObjFunc.CurrentCalc_NlOptPoint.InputValuesAsDoubleArray[i] * 0.05d);
                                    newUpper[i] = _owner.UpperBounds[i];
                                }
                                newStart[i] = ((newUpper[i] - newLower[i]) / 2d) + newLower[i];
                            }
                        }
                    }

                    // Updates the limits and start point
                    _owner.LowerBounds = newLower;
                    _owner.UpperBounds = newUpper;
                    startValues = newStart;

                    // Resets the Problem Config
                    FeaNotConvergedMessage = $"FEA did not converge for Point #{_owner.TotalPointCount + 1} of Problem Config #{_owner.Index}.{Environment.NewLine}Retrying with more restrictive Boundaries. Retry {retryCounter + 1} of {_retryLimit}.";
                    _owner.Reset();
                    OptimizeTerminationException = new NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum.NotStarted, "Optimization not started.");


                    retryCounter++;
                    if (retryCounter >= _retryLimit) break;
                }
            }
            catch (Exception e)
            {
                // Adds the message to the error.
                OptimizeTerminationException.AdditionalMessage = e.Message;
                RaisePropertyChanged("Wpf_TerminationCodeLongDescription");
            }
            finally
            {
                sw.Stop();
                NlOpt_TotalSolveTimeSpan = sw.Elapsed;

                //Resets the NlOptMethod to null
                NLOptMethod = null;
            }

        }
        #endregion
    }

    public enum StartPositionTypeEnum
    {
        Given,
        CenterOfRange,
        Random,
        PercentRandomFromCenter,
        PercentRandomFromGiven
    }

    public class NlOpt_OptimizeTerminationException : Exception
    {
        public NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum inCode, string inMessage) : base(inMessage)
        {
            OptimizeTerminationCode = inCode;
        }
        public NlOpt_OptimizeTerminationException(NlOpt_OptimizeTerminationCodeEnum inCode, string inMessage, Exception inInner) : base(inMessage, innerException: inInner)
        {
            OptimizeTerminationCode = inCode;
        }

        private NlOpt_OptimizeTerminationCodeEnum _optimizeTerminationCode;
        public NlOpt_OptimizeTerminationCodeEnum OptimizeTerminationCode
        {
            get => _optimizeTerminationCode;
            set => _optimizeTerminationCode = value;
        }

        public string CompleteMessage => InnerException == null ? Message : Message + Environment.NewLine + InnerException.Message;

        public string AdditionalMessage { get; set; } = null;

        public string CompleteMessageWithStack
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (AdditionalMessage != null)
                {
                    sb.AppendLine(AdditionalMessage);
                    sb.AppendLine("------------------------------------");
                    sb.AppendLine();
                }

                sb.AppendLine(Message);
                sb.AppendLine("------------------------------------");
                sb.AppendLine(StackTrace);

                if (InnerException != null)
                {
                    sb.AppendLine('\t' + InnerException.Message.TrimEnd(new char[] { '\r', '\n' }));
                    sb.AppendLine('\t' + "------------------------------------");
                    sb.AppendLine('\t' + InnerException.StackTrace);
                }

                return sb.ToString();
            }
        }
    }

    public enum NlOpt_OptimizeTerminationCodeEnum
    {
        NotStarted,
        Optimizing,

        Failed,
        //Failure_NaN,
        Forced_Stop,
        //InvalidArgs,

        //OutOfMemory,
        //RoundOffLimit,
        
        Success,

        Converged,

        //Limit_MaxEvalReached,
        //Limit_MaxTimeReached,
        
        //Limit_FunctionStopValue,

        //Limit_FunctionRelativeTolerance,
        //Limit_FunctionAbsoluteTolerance,
        
        //Limit_ParameterRelativeTolerance,
        //Limit_ParameterAbsoluteTolerance,
    }

    public enum ObjectiveFunctionSumTypeEnum
    {
        Simple,
        Squares,
    }
}
