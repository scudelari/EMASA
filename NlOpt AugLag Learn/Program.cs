extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Differentiation;
using NLoptNet;
using r3dm::Rhino.Geometry;


namespace NlOpt_AugLag_Learn
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Regular
            {
                DataClass_Regular d = new DataClass_Regular();

                using (var solver = new NLoptSolver(NLoptAlgorithm.AUGLAG_EQ, 9, childAlgorithm: NLoptAlgorithm.LN_COBYLA))
                {
                    solver.SetLowerBounds(new[] {-100d, -100d, -100d, -100d, -100d, -100d, -100d, -100d, -100d,});
                    solver.SetUpperBounds(new[] {100d, 100d, 100d, 100d, 100d, 100d, 100d, 100d, 100d,});

                    solver.AddEqualZeroConstraint((vals, grads) => d.Constraint(vals,grads), 0.0001d);
                    solver.AddEqualZeroConstraint((vals, grads) => d.Constraint(vals, grads), 0.0001d);
                    solver.AddEqualZeroConstraint((vals, grads) => d.Constraint(vals, grads), 0.0001d);

                    solver.SetMinObjective(d.Function);

                    double? finalScore;
                    var initialValue = new[] {0d, 0d, 0d, 10d, 10d, 10d, 20d, 20d, 20d,};
                    var result = solver.Optimize(initialValue, out finalScore);
                }
            }
            #endregion
        }
    }

    public class DataClass_Regular
    {
        public bool isFunction = true;
        public int fcount = 0;
        public int gradcount = 0;
        

        private int gradientIndexer = 0;
        public double target_lenght = 20d;

        FunctionEval CurrentFunctionEval { get; set; }
        FunctionEval BaseForCurrentGradientCalculations;
        private FunctionEval[] GradientCalculations;

        public int contraint_index = 0;
        public int maxConstraints = 3;
        public double Constraint(double[] vars, double[] grad)
        {
            double retval = Double.NaN;

            string ctename = $"l{contraint_index}_length";
            retval = tempEvalQuantities[ctename] - target_lenght;

            Console.WriteLine($"Constraint: {contraint_index} - Value {retval}");

            if (grad != null)
            {
                // Calculates the gradient of the current constraint
                for (int i = 0; i < grad.Length; i++)
                {
                    double a = BaseForCurrentGradientCalculations.InputVariables[i];
                    double xa = BaseForCurrentGradientCalculations.SolutionQuantities[ctename];

                    double b = GradientCalculations[i].InputVariables[i];
                    double xb = GradientCalculations[i].SolutionQuantities[ctename];

                    grad[i] = (xb - xa) / (b - a);
                }
            }
            
            contraint_index++;
            if (contraint_index == maxConstraints) contraint_index = 0;
            
            return retval;
        }

        // Used to store the current eval because - maybe - they will be requested by the gradient evals later on
        public Dictionary<string, double> tempEvalQuantities;
        public double ObjectiveFunctionRaw(double[] vars)
        {
            tempEvalQuantities = new Dictionary<string, double>();

            Console.WriteLine($"Function Called: Input: {vars.Aggregate(new StringBuilder("{"), (inBuilder, inD) => inBuilder.Append(inD).Append(" , ")).Append("}").ToString()}");

            if (isFunction) fcount++;
            else gradcount++;

            Point3d p1 = new Point3d(vars[0], vars[1], vars[2]);
            Point3d p2 = new Point3d(vars[3], vars[4], vars[5]);
            Point3d p3 = new Point3d(vars[6], vars[7], vars[8]);

            Line l0 = new Line(p1, p2);
            Line l1 = new Line(p2, p3);
            Line l2 = new Line(p3, p1);

            double val = 0d;

            val += p1.Z * p1.Z;
            val += p2.Z * p2.Z;
            val += p3.Z * p3.Z;

            tempEvalQuantities.Add("l0_length", l0.Length);
            tempEvalQuantities.Add("l1_length", l1.Length);
            tempEvalQuantities.Add("l2_length", l2.Length);

            Console.WriteLine($"P1: {p1}");
            Console.WriteLine($"P2: {p2}");
            Console.WriteLine($"P3: {p3}");

            CurrentFunctionEval = new FunctionEval(vars.Clone() as double[], tempEvalQuantities);

            // Square sum
            return val;
        }

        public double Function(double[] vars, double[] grad)
        {
            isFunction = true;
            double eval = ObjectiveFunctionRaw(vars);

            if (grad != null)
            {
                BaseForCurrentGradientCalculations = CurrentFunctionEval;
                GradientCalculations = new FunctionEval[grad.Length];
                isFunction = false;

                // Creates a new FiniteDifferences Manager
                NumericalDerivative nd = new NumericalDerivative(2, 0);

                // Fills the gradient information 
                for (gradientIndexer = 0; gradientIndexer < grad.Length; gradientIndexer++)
                {
                    grad[gradientIndexer] = nd.EvaluatePartialDerivative(ObjectiveFunctionRaw, vars, gradientIndexer, 1, eval);
                    GradientCalculations[gradientIndexer] = CurrentFunctionEval;
                }

            }

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine($"Eval: {eval}");

            return eval;
        }
    }

    public class FunctionEval
    {
        public FunctionEval(double[] inInputVariables, Dictionary<string, double> inSolutionQuantities)
        {
            InputVariables = inInputVariables ?? throw new ArgumentNullException(nameof(inInputVariables));
            SolutionQuantities = inSolutionQuantities ?? throw new ArgumentNullException(nameof(inSolutionQuantities));
        }

        public double[] InputVariables { get; private set; }
        public Dictionary<string, double> SolutionQuantities { get; private set; }
    }
}
