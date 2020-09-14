using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.Opt;
using Prism.Mvvm;
using MathNet.Numerics;
using MathNet.Numerics.Differentiation;
using MathNet.Numerics.LinearAlgebra;

namespace Emasa_Optimizer.ProblemDefs
{
    public class GeneralProblem : BindableBase
    {
        [NotNull] protected readonly SolveManager _owner;
        public GeneralProblem([NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));
        }

        public virtual string WpfFriendlyName => "FEA";
        public virtual string WpfProblemDescription => "Optimization based on the results of Finite Element Analysis";
        public virtual bool IsFea => true;
        public virtual bool TargetsOpenGhAlgorithm => true;
        public virtual bool OverridesGeneralProblem => false;

        public virtual int NumberOfVariables => _owner.Gh_Alg.InputDefs_VarCount;

        #region Current Eval Buffer
        private EvalTypeEnum _currentEvalType;
        public EvalTypeEnum CurrentEvalType
        {
            get => _currentEvalType;
            set => SetProperty(ref _currentEvalType, value);
        }

        private SolutionPoint _tempSolutionPoint;

        private SolutionPoint _currentSolutionPoint;
        public SolutionPoint CurrentSolutionPoint
        {
            get => _currentSolutionPoint;
            set => SetProperty(ref _currentSolutionPoint, value);
        }
        #endregion

        Random rnd = new Random();
        /// <summary>
        /// Override in other problems to calculate the objective function.
        /// </summary>
        /// <param name="inPoint">The solution point of this function. The Grasshopper variables have already been updated. You *MUST NOT* set the FunctionEval parameter in this function.</param>
        /// <returns>The function evaluation result.</returns>
        public virtual double Function(SolutionPoint inPoint)
        {
            // Builds the FeModel Class and links it to this point
            inPoint.FeModel = new FeModel(inPoint);

            return 0d;
        }

        private double FunctionWithArrayInputWrapper(double[] inPointVars)
        {
            // Starts the stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            // Creates a new solution point
            _tempSolutionPoint = new SolutionPoint(_owner, inPointVars, CurrentEvalType);
            // Stores it in the list of calculated solutions
            _owner.AddSolutionPoint(_tempSolutionPoint);

            // Updates the grasshopper geometry of the SolutionPoint
            _owner.Gh_Alg.UpdateGrasshopperGeometry(_tempSolutionPoint);

            // Evaluates the function at the given point and saves
            _tempSolutionPoint.FunctionEval = Function(_tempSolutionPoint);

            // Stops the watch and saves the timespan
            sw.Stop();
            _tempSolutionPoint.EvalTimeSpan = sw.Elapsed;

            // Returns the eval
            return _tempSolutionPoint.FunctionEval;
        }
        public double Function_NLOptFunctionWrapper(double[] inPointVars, double[] inGradient = null)
        {
            // First, evaluates the function at the given point
            CurrentEvalType = EvalTypeEnum.ObjectiveFunction;
            FunctionWithArrayInputWrapper(inPointVars);

            // Saves the created function point
            CurrentSolutionPoint = _tempSolutionPoint;
            
            // We must also calculate the gradient of this point
            if (inGradient != null)
            {
                Stopwatch gradSw = Stopwatch.StartNew();

                // Creates a new FiniteDifferences Manager
                NumericalDerivative nd = new NumericalDerivative(_owner.NlOptManager.FiniteDiff_PointsPerPartialDerivative, _owner.NlOptManager.FiniteDiff_PointsPerPartialDerivativeCenter);

                // Fills the gradient information 
                CurrentEvalType = EvalTypeEnum.Gradient;
                for (int i = 0; i < CurrentSolutionPoint.GradientAtThisPoint.Length; i++)
                {
                    CurrentSolutionPoint.GradientAtThisPoint[i] = nd.EvaluatePartialDerivative(FunctionWithArrayInputWrapper, CurrentSolutionPoint.InputValuesAsDoubleArray, i, 1, CurrentSolutionPoint.FunctionEval);
                }

                // Saves the copy to the return parameter
                double[] gradArray = new double[CurrentSolutionPoint.GradientAtThisPoint.Length];
                CurrentSolutionPoint.GradientAtThisPoint.CopyTo(gradArray, 0);
                inGradient = gradArray;

                gradSw.Stop();
                CurrentSolutionPoint.TotalGradientTimeSpan = gradSw.Elapsed;
            }

            // Should we cancel the execution - Forces the NlOpt to Stop
            if (_owner.NlOptManager.CancelSource.IsCancellationRequested) _owner.NlOptManager.NLOptMethod.ForceStop();

            return CurrentSolutionPoint.FunctionEval;
        }
    }
}
