extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Accord.Genetic;
using Accord.Math.Optimization;
using AccordHelper.FEA;
using AccordHelper.Opt;
using r3dm::Rhino;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;
using Rhino;

namespace OptimizationWithAccord
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Assembly.Load("");

                RhinoModel.Initialize();

                RhinoModel.RM.RhinoVisible = true;

                string bla = RhinoModel.RM.GrasshopperFullFileName;

                // Starts the solver
                TestArchProblem prob = new TestArchProblem(SolverType.Cobyla, FeaSoftwareEnum.Ansys);
                prob.ResetSolver();

                while (!prob.DoIterations(10000))
                {
                    switch (prob.SolverType)
                    {
                        case SolverType.AugmentedLagrangian:
                            Console.WriteLine("--- STATUS");
                            Console.WriteLine($" FunctionHitCount: {prob.ObjectiveFunction.FunctionHitCount}");
                            Console.WriteLine($" Input: {prob.OptMethod.Solution}");
                            Console.WriteLine($" Status: {((AugmentedLagrangian)prob.OptMethod).Status}");
                            Console.WriteLine($" Value: {prob.OptMethod.Value:F4}");
                            for (int index = 0; index < prob.PossibleSolutions.Count; index++)
                            {
                                PossibleSolution probPossibleSolution = prob.PossibleSolutions[index];
                                Console.WriteLine($"    Poss Solution: {index} : {probPossibleSolution.Eval:F4}");
                            }

                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("--- CURRENT SOLVER SOLUTION");
                            Console.WriteLine(prob.CurrentSolverSolution.FriendlyReport);
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("--- BEST SOLUTION");
                            Console.WriteLine(prob.BestSolutionSoFar.FriendlyReport);
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            break;

                        case SolverType.Cobyla:
                            Console.WriteLine("--- STATUS");
                            Console.WriteLine($" FunctionHitCount: {prob.ObjectiveFunction.FunctionHitCount}");
                            Console.WriteLine($" Input: {prob.OptMethod.Solution}");
                            Console.WriteLine($" Status: {((Cobyla) prob.OptMethod).Status}");
                            Console.WriteLine($" Value: {prob.OptMethod.Value:F4}");
                            for (int index = 0; index < prob.PossibleSolutions.Count; index++)
                            {
                                PossibleSolution probPossibleSolution = prob.PossibleSolutions[index];
                                Console.WriteLine($"    Poss Solution: {index} : {probPossibleSolution.Eval:F4}");
                            }

                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("--- CURRENT SOLVER SOLUTION");
                            Console.WriteLine(prob.CurrentSolverSolution.FriendlyReport);
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine("--- BEST SOLUTION");
                            Console.WriteLine(prob.BestSolutionSoFar.FriendlyReport);
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            Console.WriteLine();
                            break;

                        case SolverType.Genetic:
                            Console.WriteLine("--- STATUS");
                            Console.WriteLine($" FunctionHitCount: {prob.ObjectiveFunction.FunctionHitCount}");
                            Console.WriteLine($" Input: {((InputParamChromosome)prob.GenAlgPopulation.BestChromosome).Value}");
                            //Console.WriteLine($" Status: {((Cobyla)prob.OptMethod).Status}");
                            Console.WriteLine($" Value: {prob.GenAlgPopulation.BestChromosome.Fitness:F4}");
                            //for (int index = 0; index < prob.PossibleSolutions.Count; index++)
                            //{
                            //    PossibleSolution probPossibleSolution = prob.PossibleSolutions[index];
                            //    Console.WriteLine($"    Poss Solution: {index} : {probPossibleSolution.Eval:F4}");
                            //}

                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine("--- CURRENT SOLVER SOLUTION");
                            //Console.WriteLine(prob.CurrentSolverSolution.FriendlyReport);
                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine("--- BEST SOLUTION");
                            //Console.WriteLine(prob.BestSolutionSoFar.FriendlyReport);
                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                RhinoModel.RM.RhinoVisible = true;

                RhinoModel.DisposeAll();

                Console.ReadLine();
            }
        }
    }
}
