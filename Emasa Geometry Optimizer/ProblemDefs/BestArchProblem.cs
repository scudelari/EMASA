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

            // Setting the default loads
            Load_Gravity.IsActive = true;
            Load_Gravity.Factor = 1.4d;
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
            IntermediateDefs.Add(new LineList_Output_ParamDef("ArchLines_1", inDefaultRestraint: FeRestraint.YOnlyRestraint));
            IntermediateDefs.Add(new LineList_Output_ParamDef("ArchLines_2", inDefaultRestraint: FeRestraint.YOnlyRestraint));
            IntermediateDefs.Add(new PointList_Output_ParamDef("FixedSupportJoint_1", FeRestraint.PinnedRestraint));
            IntermediateDefs.Add(new PointList_Output_ParamDef("FixedSupportJoint_2", FeRestraint.PinnedRestraint));

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

        protected override double Function_Override()
        {
            Rhino_SendInputAndGetOutput();

            // Gets the grasshopper vars in the right cast
            List<double> bowLength = CurrentSolution.GetIntermediateValueByName<List<double>>("BowLength");

            List<Point3d> fixedSupport_1 = FeModel.AddJoints_IntermediateParameter("FixedSupportJoint_1");
            List<Point3d> fixedSupport_2 = FeModel.AddJoints_IntermediateParameter("FixedSupportJoint_2");

            List<Line> archLines_1 = FeModel.AddFrames_IntermediateParameter("ArchLines_1"); // The section is defined in the interface
            List<Line> archLines_2 = FeModel.AddFrames_IntermediateParameter("ArchLines_2"); // The section is defined in the interface

            // Loading of the model is set in the Problem object by the interface

            // The chosen result output given is only valid for the model runs that are *not* for section analysis
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
