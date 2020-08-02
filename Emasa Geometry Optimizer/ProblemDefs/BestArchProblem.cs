extern alias r3dm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using AccordHelper.FEA;
using AccordHelper.FEA.Items;
using AccordHelper.Opt;
using AccordHelper.Opt.ParamDefinitions;
using MathNet.Numerics.Statistics;
using RhinoInterfaceLibrary;
using Line = r3dm::Rhino.Geometry.Line;
using Point3d = r3dm::Rhino.Geometry.Point3d;

namespace Emasa_Geometry_Optimizer.ProblemDefs
{
    [Serializable]
    public class BestArchProblem : ProblemBase
    {
        public override string ProblemFriendlyName => "Finds the best arch";

        public override void SetDefaultScreenShots()
        {
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.AxialDiagramOutput, "Axial Force Plot", ImageCaptureViewDirection.Front_Towards_YPos));
            
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.EquivalentVonMisesStressOutput, "Von-Mises Stress - Front", ImageCaptureViewDirection.Front_Towards_YPos) {LegendAutoScale = false, LegendScale_Min = 0d, LegendScale_Max = FeMaterial.GetMaterialByName("S355").Fy});
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.EquivalentVonMisesStressOutput, "Von-Mises Stress - Top Front Right Corner", ImageCaptureViewDirection.Perspective_TFR_Corner) { LegendAutoScale = false, LegendScale_Min = 0d, LegendScale_Max = FeMaterial.GetMaterialByName("S355").Fy });
            
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.TotalDisplacementPlot, "Total Displacement - Front", ImageCaptureViewDirection.Front_Towards_YPos));
            DesiredScreenShots.Add(new DesiredScreenShotDefinition(ScreenShotType.TotalDisplacementPlot, "Total Displacement - Top Front Right Corner", ImageCaptureViewDirection.Perspective_TFR_Corner));

            // Gets the basic Rhino Screenshots
            base.SetDefaultScreenShots();
        }

        public BestArchProblem() : base(new TestArchObjectiveFunction())
        {
            AddSupportedFeaSoftware(FeaSoftwareEnum.Ansys);
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
            FinalDefs.Add(new Double_Output_ParamDef("MaximumStrainEnergy",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(1e-3,5e-2)
                ));

            // Sets the output variables
            FinalDefs.Add(new Double_Output_ParamDef("AverageStrainEnergy",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(1e-3, 5e-2)
            ));

            // Sets the output variables
            FinalDefs.Add(new Double_Output_ParamDef("StDevStrainEnergy",
                inTargetValue: 0d,
                inExpectedScale: new DoubleValueRange(1e-3, 5e-2)
            ));
        }

        public override double Function_Override(double[] inVariables)
        {
            Rhino_SendInputAndGetOutput();

            // Gets the input vars in the right cast
            List<Line> archLines = CurrentSolution.GetIntermediateValueByName<List<Line>>("ArchLines");
            List<double> bowLength = CurrentSolution.GetIntermediateValueByName<List<double>>("BowLength");
            List<Point3d> fixedSupport = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("FixedSupportJoint");
            List<Point3d> slidingSupport = CurrentSolution.GetIntermediateValueByName<List<Point3d>>("SlidingSupportJoint");

            // Makes a new Ansys document

            FeModel.SlendernessLimit = 50d;
            FeModel.AddGravityLoad();
            FeModel.AddFrameList(archLines, inGroupNames: new List<string>() {"Arch"}, inPossibleSections: (from a in FeSectionPipe.GetAllSections()
                where a.Dimensions["OuterDiameter"] == 0.508
                select a).ToList());
        FeModel.AddPoint3dToGroups(fixedSupport, new List<string>() { "pin" });
            FeModel.AddRestraint("pin", new bool[] { true, true, true, false, false, false });

            FeModel.AddPoint3dToGroups(slidingSupport, new List<string>() {"slide"});
            FeModel.AddRestraint("slide", new bool[] { true, true, true, false, false, false });

            FeModel.AddPoint3dToGroups(archLines.GetAllPoints(), new List<string>() {"apnts"});
            FeModel.AddRestraint("apnts", new bool[] { false, true, false, false, false, false });

            //FeModel.FindBestSections();

            FeModel.RunAnalysisAndGetResults(new List<ResultOutput>()
                {
                ResultOutput.Element_StrainEnergy,
                ResultOutput.SectionNode_Stress,
                });

            CurrentSolution.FillScreenShotData();

            double averageStrainEnergy = (from a in FeModel.MeshBeamElements select a.Value.ElementStrainEnergy.StrainEnergy).Average();
            double maxStrainEnergy = (from a in FeModel.MeshBeamElements select a.Value.ElementStrainEnergy.StrainEnergy).Max();
            double StDevStrainEnergy = (from a in FeModel.MeshBeamElements select a.Value.ElementStrainEnergy.StrainEnergy).StandardDeviation();

            CurrentSolution.SetFinalValueByName("MaximumStrainEnergy", maxStrainEnergy);

            CurrentSolution.Eval = CurrentSolution.GetSquareSumOfList(new[]
                {
                // Stores the solution result values

                CurrentSolution.SetFinalValueByName("AverageStrainEnergy", averageStrainEnergy),
                CurrentSolution.SetFinalValueByName("StDevStrainEnergy", StDevStrainEnergy),
                });

            return CurrentSolution.Eval;
        }
    }
}
