extern alias r3dm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

        #region Actions!
        public void RunOptimization()
        {
            Stopwatch sw = Stopwatch.StartNew();

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
                    NLOptMethod.SetLowerBounds(AppSS.I.Gh_Alg.InputDefs_LowerBounds);
                    NLOptMethod.SetUpperBounds(AppSS.I.Gh_Alg.InputDefs_UpperBounds);

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
                    double[] startValues = AppSS.I.Gh_Alg.GetInputStartPosition();

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
            finally
            {
                sw.Stop();
                NlOpt_TotalSolveTimeSpan = sw.Elapsed;
            }

            //Resets the NlOptMethod to null
            NLOptMethod = null;
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

        public string CompleteMessageWithStack => InnerException == null
            ? Message + Environment.NewLine + "------------------------------------" + Environment.NewLine + StackTrace
            : Message + Environment.NewLine + "------------------------------------" + Environment.NewLine + StackTrace + Environment.NewLine + Environment.NewLine + Environment.NewLine + InnerException.Message + Environment.NewLine + "------------------------------------" + Environment.NewLine + InnerException.StackTrace;
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
