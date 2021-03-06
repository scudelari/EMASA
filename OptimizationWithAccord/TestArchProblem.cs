﻿extern alias r3dm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA;
using AccordHelper.Opt;
using AccordHelper.Opt.ParamDefinitions;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace OptimizationWithAccord
{
    [Serializable]
    public class TestArchProblem : ProblemBase
    {
        public TestArchProblem(SolverType inSolverType, FeaSoftwareEnum inFeaSoftware) : base(inSolverType, new TestArchObjectiveFunction())
        {

        }
    }

    [Serializable]
    public class TestArchObjectiveFunction : ObjectiveFunctionBase
    {
        protected override void InitializeVariables()
        {
            // Sets the input parameters
            AddParameterToInputs(new Double_Input_ParamDef("BowFactor", new DoubleValueRange(.0005, .05)){Start = 0.025});

            // Sets the intermediate variables we will receive from Grasshopper
            IntermediateDefs.Add(new DoubleList_Output_ParamDef("BowLength"));
            IntermediateDefs.Add(new LineList_Output_ParamDef("ArchLines"));
            IntermediateDefs.Add(new PointList_Output_ParamDef("FixedSupportJoint"));
            IntermediateDefs.Add(new PointList_Output_ParamDef("SlidingSupportJoint"));

            // Sets the output variables
            FinalDefs.Add(new Double_Output_ParamDef("MaximumVonMisesStress",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(0,400)
                ));
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
            List<Line> archLines = CurrentSolution.GetIntermediateValueByName<List<Line>>("ArchLines");
            List<double> bowLength = CurrentSolution.GetIntermediateValueByName<List<double>>("BowLength");
            List<Point3d> fixedSupport = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("FixedSupportJoint");
            List<Point3d> slidingSupport = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("SlidingSupportJoint");

            // Makes a new Ansys document

            FeModel.SlendernessLimit = 120d;
            FeModel.AddFrameList(archLines, inGroupNames: new List<string>() {"Arch"});

            FeModel.AddPoint3dToGroups(fixedSupport, new List<string>() { "pin" });
            FeModel.AddRestraint("pin", new bool[] { true, true, true, false, false, false });

            FeModel.AddPoint3dToGroups(slidingSupport, new List<string>() {"slide"});
            FeModel.AddRestraint("slide", new bool[] { true, true, true, false, false, false });

            FeModel.InitializeModelAndSoftware();
            FeModel.WriteModelData();

            return 10;
        }
    }
}
