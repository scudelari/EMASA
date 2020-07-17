extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using AccordHelper.Opt;
using AccordHelper.Opt.ParamDefinitions;
using RhinoInterfaceLibrary;
using Line = r3dm::Rhino.Geometry.Line;
using Point3d = r3dm::Rhino.Geometry.Point3d;
using RhinoMath = r3dm::Rhino.RhinoMath;
using Vector3d = r3dm::Rhino.Geometry.Vector3d;

namespace Emasa_Geometry_Optimizer.ProblemDefs
{
    [Serializable]
    public class FindTriangleProblem : ProblemBase
    {
        public override string ProblemFriendlyName => "Find horizontally aligned, equilateral triangle that lies on the X-Y plane.";

        public FindTriangleProblem() : base(new TestTriangleObjectiveFunction())
        {
            
        }
    }

    [Serializable]
    public class TestTriangleObjectiveFunction : ObjectiveFunctionBase
    {
        protected override void InitializeVariables()
        {
            // Sets the variables related to this Objective Function
            AddParameterToInputs(new Point_Input_ParamDef("A", new PointValueRange(new Point3d(-100d, -100d, -100d), new Point3d(100d, 100d, 100d))));
            AddParameterToInputs(new Point_Input_ParamDef("B", new PointValueRange(new Point3d(-100d, -100d, -100d), new Point3d(100d, 100d, 100d))));
            AddParameterToInputs(new Point_Input_ParamDef("C", new PointValueRange(new Point3d(-100d, -100d, -100d), new Point3d(100d, 100d, 100d))));

            //AddParameterToInputs(new Point_Input_ParamDef("A", new PointValueRange(new Point3d(0d, 0d, -100d), new Point3d(100d, 100d, 100d))));
            //AddParameterToInputs(new Point_Input_ParamDef("B", new PointValueRange(new Point3d(-100d, 0d, -100d), new Point3d(0d, 100d, 100d))));
            //AddParameterToInputs(new Point_Input_ParamDef("C", new PointValueRange(new Point3d(0d, -100d, -100d), new Point3d(100d, 0d, 100d))));

            IntermediateDefs.Add(new LineList_Output_ParamDef("InnerLines"));
            IntermediateDefs.Add(new PointList_Output_ParamDef("InnerPoints"));
            IntermediateDefs.Add(new PointList_Output_ParamDef("InnerCentroid"));

            FinalDefs.Add(new Double_Output_ParamDef("L1 Side Length", 
                inTargetValue: 40d, inExpectedScale: new DoubleValueRange(0d,200d)));
            FinalDefs.Add(new Double_Output_ParamDef("L2 Side Length",
                inTargetValue: 40d, inExpectedScale: new DoubleValueRange(0d, 200d)));
            FinalDefs.Add(new Double_Output_ParamDef("L3 Side Length",
                inTargetValue: 40d, inExpectedScale: new DoubleValueRange(0d, 200d)));

            FinalDefs.Add(new Double_Output_ParamDef("Delta Of Centroid to Origin",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(0d, 200d)));
            FinalDefs.Add(new Double_Output_ParamDef("Minimum Angle To X",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(0d, 90d)));

            FinalDefs.Add(new Double_Output_ParamDef("P1 Height", 
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(-100d, 100d)));
            FinalDefs.Add(new Double_Output_ParamDef("P2 Height",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(-100d, 100d)));
            FinalDefs.Add(new Double_Output_ParamDef("P3 Height",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(-100d, 100d)));

        }

        public override double Function_Override(double[] inValues)
        {
            // Writes the points to Grasshopper
            CurrentSolution.WritePointToGrasshopper(RhinoStaticMethods.GH_Auto_InputVariableFolder(RhinoModel.RM.GrasshopperFullFileName));

            // Runs Grasshopper
            RhinoModel.RM.SolveGrasshopper();

            // Reads the output variables from Grasshopper
            CurrentSolution.ReadResultsFromGrasshopper(RhinoStaticMethods.GH_Auto_OutputVariableFolder(RhinoModel.RM.GrasshopperFullFileName));

            // Gets the input vars in the right cast
            List<Line> triangleLines = CurrentSolution.GetIntermediateValueByName<List<Line>>("InnerLines");
            List<Point3d> triangleVertices = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("InnerPoints");
            List<Point3d> centroid = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("InnerCentroid");

            double L1Length = triangleLines[0].Length;
            double L2Length = triangleLines[1].Length;
            double L3Length = triangleLines[2].Length;

            double centroidDelta = centroid[0].DistanceTo(Point3d.Origin);

            double MinAngleToX = (from a in triangleLines
                select RhinoMath.ToDegrees(Vector3d.VectorAngle(a.Direction, Vector3d.XAxis) > 90d ? 180d- Vector3d.VectorAngle(a.Direction, Vector3d.XAxis) : Vector3d.VectorAngle(a.Direction, Vector3d.XAxis))).Min();

            double P1Height = triangleVertices[0].Z;
            double P2Height = triangleVertices[1].Z;
            double P3Height = triangleVertices[2].Z;

            CurrentSolution.Eval = CurrentSolution.GetSquareSumOfList(new []
            {
                // Stores the solution result values
                CurrentSolution.SetFinalValueByName("L1 Side Length", L1Length),
                CurrentSolution.SetFinalValueByName("L2 Side Length", L2Length),
                CurrentSolution.SetFinalValueByName("L3 Side Length", L3Length),
                CurrentSolution.SetFinalValueByName("Delta Of Centroid to Origin", centroidDelta),
                CurrentSolution.SetFinalValueByName("Minimum Angle To X", MinAngleToX),
                CurrentSolution.SetFinalValueByName("P1 Height", P1Height),
                CurrentSolution.SetFinalValueByName("P2 Height", P2Height),
                CurrentSolution.SetFinalValueByName("P3 Height", P3Height),
            });


            return CurrentSolution.Eval;
        }
    }
}
