using System;
using System.Collections.Generic;
using System.Linq;
using LibOptimization.Optimization;
using LibOptimization.Util;

namespace EmasaSapTools.Optimization
{
    /// <summary>
    /// The search will be towards 0, so the objective function must be made in a way that its optimization is towards 0.
    /// </summary>
    public class clsOpdSimpleSearch : absOptimization
    {
        public clsOpdSimpleSearch(absObjectiveFunction ai_func)
        {
            m_func = ai_func;
        }

        private clsPoint _currentBestSolution = null;
        public override clsPoint Result => _currentBestSolution;

        private List<clsPoint> _solutions = new List<clsPoint>();
        public override List<clsPoint> Results => _solutions;

        private double CurrentPosition { get; set; }

        //public double InitialValueRangeLower { get; set; }
        //public double InitialValueRangeUpper { get; set; }
        public double StepSize { get; set; }

        private double _iterationChangeLimit = 1e-2d;

        public double IterationChangeLimit
        {
            get => _iterationChangeLimit;
            set => _iterationChangeLimit = value;
        }

        private double _inflateStepFactor = 2d;

        public double InflateStepFactor
        {
            get => _inflateStepFactor;
            set => _inflateStepFactor = value;
        }

        private double _shrinkStepFactor = 0.5d;

        public double ShrinkStepFactor
        {
            get => _shrinkStepFactor;
            set => _shrinkStepFactor = value;
        }

        private int _iteration = 10000;

        // Defines, it seems, the maximum number of iterations
        public override int Iteration
        {
            get => _iteration;
            set => _iteration = value;
        }

        private Direction IterationDirection = Direction.Positive;

        public void SetInitialDirectionToPositive()
        {
            IterationDirection = Direction.Positive;
        }

        public void SetInitialDirectionToNegative()
        {
            IterationDirection = Direction.Negative;
        }

        private NextIteration NextIterationAction = NextIteration.ApplyStep;
        private int RepeatedSuccessCounter = 0;

        private bool _initialized = false;

        /// <summary>
        /// Performs an iteration. Use it in a while loop comparing it to false (while false it didn't reach the end).
        /// </summary>
        /// <param name="ai_iteration">Ignored</param>
        /// <returns></returns>
        public override bool DoIteration(int ai_iteration = 0)
        {
            if (!_initialized) throw new Exception("You must initialize the optimizer before performing iterations.");

            if (_currentBestSolution == null) // This is the first iteration
            {
                // This evaluates the objective function for this point.
                clsPoint pnt = new clsPoint(m_func, InitialPosition);

                Results.Add(pnt);
                _currentBestSolution = pnt;
            }
            else
            {
                double nextVal;

                switch (NextIterationAction)
                {
                    case NextIteration.ApplyStep:
                        break;

                    case NextIteration.ShrinkMultiplier:
                        StepSize *= ShrinkStepFactor;
                        break;

                    case NextIteration.InflateMultiplier:
                        StepSize *= InflateStepFactor;
                        break;

                    case NextIteration.FlipDirection:
                        StepSize *= ShrinkStepFactor;
                        if (IterationDirection == Direction.Negative)
                            IterationDirection = Direction.Positive;
                        else
                            IterationDirection = Direction.Negative;
                        break;

                    default:
                        throw new Exception("Invalid NextIterationAction Value.");
                }


                switch (IterationDirection)
                {
                    case Direction.Positive:
                        nextVal = _currentBestSolution[0] + StepSize;
                        break;
                    case Direction.Negative:
                        nextVal = _currentBestSolution[0] - StepSize;
                        break;
                    default:
                        throw new Exception("Invalid IterationDirection Value.");
                }

                // It is trying to reanalyse somethign that has already been checked.
                if (Results.Any(a => a.Any(b => b == nextVal)))
                {
                    NextIterationAction = NextIteration.ShrinkMultiplier;
                    return false;
                }

                clsPoint pnt = new clsPoint(m_func, new double[] {nextVal});

                Results.Add(pnt);
                Results.Sort();

                if (Results[0] != _currentBestSolution) // The solution is better so we are on the right path
                {
                    bool finished = Math.Abs(Results[0].Eval - _currentBestSolution.Eval) < IterationChangeLimit;
                    _currentBestSolution = Results[0];
                    if (finished) return true;

                    RepeatedSuccessCounter++;
                    if (RepeatedSuccessCounter > 2)
                    {
                        NextIterationAction = NextIteration.InflateMultiplier;
                        RepeatedSuccessCounter = 0;
                    }
                    else
                    {
                        NextIterationAction = NextIteration.ApplyStep;
                    }
                }
                else
                {
                    // Failed to improve, decide what to do next
                    RepeatedSuccessCounter = 0;

                    switch (NextIterationAction)
                    {
                        case NextIteration.ApplyStep:
                            NextIterationAction = NextIteration.ShrinkMultiplier;
                            break;

                        case NextIteration.InflateMultiplier:
                            NextIterationAction = NextIteration.ShrinkMultiplier;
                            break;

                        case NextIteration.ShrinkMultiplier:
                            NextIterationAction = NextIteration.FlipDirection;
                            break;

                        case NextIteration.FlipDirection:
                            NextIterationAction = NextIteration.ApplyStep;
                            break;

                        default:
                            throw new Exception("Invalid NextIterationAction Value.");
                    }
                }
            }

            // Increases the iteration counter
            m_iteration++;

            if (m_iteration > Iteration) return true;

            return false;
        }

        public override void Init()
        {
            try
            {
                if (m_func.NumberOfVariable() != 1)
                    throw new Exception(
                        "Only one variable is supported, thus the NumberOfVariables of the Objective Function must be 1.");
                if (InitialPosition.Length != 1)
                    throw new Exception(
                        "Only one variable is supported, thus the InitialPosition must contain only one value.");

                if (InitialPosition[0] > InitialValueRangeUpper ||
                    InitialPosition[0] < InitialValueRangeLower ||
                    InitialValueRangeUpper == InitialValueRangeLower)
                    throw new Exception("The bounds are inconsistent.");

                m_iteration = 0;
                m_error.Clear();
                _initialized = true;
            }
            catch (Exception)
            {
                m_error.SetError(true, clsError.ErrorType.ERR_INIT);
            }
        }

        public override bool IsRecentError()
        {
            return m_error.IsError();
        }

        private enum Direction
        {
            Positive,
            Negative
        }

        private enum NextIteration
        {
            ApplyStep,
            InflateMultiplier,
            ShrinkMultiplier,
            FlipDirection
        }
    }
}