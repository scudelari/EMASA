using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Items;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.SapObjects;

namespace AccordHelper.FEA
{
    public class S2KModel : FeModelBase
    {
        public override string SubDir { get; } = "Sap2000";

        /// <summary>
        /// Defines a Sap2000 Model.
        /// </summary>
        /// <param name="inModelFolder">The target folder for the analysis</param>
        public S2KModel(string inModelFolder) : base(inModelFolder, "model.s2k")
        {

        }

        public override void InitializeSoftware()
        {
            // Starts a new SAP2000 instance if it isn't available in the Singleton
            Sap2000Library.S2KModel.InitSingleton_RunningOrNew(UnitsEnum.N_m_C);

            // Opens a new Blank Model
            Sap2000Library.S2KModel.SM.NewModelBlank(inModelUnits: UnitsEnum.N_m_C);
        }

        public override void ResetSoftwareData()
        {
            throw new NotImplementedException();
        }

        public override void CloseApplication()
        {
            throw new NotImplementedException();
        }

        public override void WriteModelToSoftware()
        {
            // Sends the materials to SAP2000
            foreach (FeMaterial feMaterial in Materials)
            {
                string sAssignedName = Sap2000Library.S2KModel.SM.MaterialMan.SetMaterial(MatTypeEnum.Steel, feMaterial.Name);
                if (sAssignedName != feMaterial.Name) throw new Exception($"SAP2000 assigned the name {sAssignedName} to material {feMaterial.Name}");

                Sap2000Library.S2KModel.SM.MaterialMan.SetIsotropicMaterialProperties(feMaterial.Name, feMaterial.YoungModulus, feMaterial.Poisson, feMaterial.ThermalCoefficient);
                Sap2000Library.S2KModel.SM.MaterialMan.SetOtherSteelMaterialProperties(feMaterial.Name, feMaterial.Fy, feMaterial.Fu, feMaterial.Fy, feMaterial.Fu);
            }

            // Sends the sections to SAP2000
            foreach (FeSection feSection in Sections)
            {
                Sap2000Library.S2KModel.SM.FrameSecMan.SetOrAddPipe(feSection.Name, feSection.Material.Name, feSection.Dimensions["OuterDiameter"], feSection.Dimensions["Thickness"]);
            }

            // Sends the Points to SAP2000
            foreach (KeyValuePair<int, FeJoint> feJoint in Joints)
            {
                string sAssignedName = Sap2000Library.S2KModel.SM.PointMan.AddByCoord(feJoint.Value.Point.X, feJoint.Value.Point.Y, feJoint.Value.Point.Z, feJoint.Value.Id.ToString());
                
                if (sAssignedName != feJoint.Value.Id.ToString()) throw new Exception($"SAP2000 assigned the name {sAssignedName} to joint {feJoint.Value.Id.ToString()}");
            }

            // Sends the Frames to SAP2000
            foreach (KeyValuePair<int, FeFrame> feFrame in Frames)
            {
                string sAssignedName = Sap2000Library.S2KModel.SM.FrameMan.AddByPoint(feFrame.Value.IJoint.Id.ToString(), feFrame.Value.JJoint.Id.ToString(), feFrame.Value.Section.Name, feFrame.Value.Id.ToString());
                if (sAssignedName != feFrame.Value.Id.ToString()) throw new Exception($"SAP2000 assigned the name {sAssignedName} to frame {feFrame.Value.Id.ToString()}");
            }

            // Groups the elements
            foreach (KeyValuePair<string, FeGroup> feGroup in Groups)
            {
                // Adds the group
                Sap2000Library.S2KModel.SM.GroupMan.AddGroup(feGroup.Value.Name);

                foreach (FeJoint feGroupJoint in feGroup.Value.Joints)
                {
                    Sap2000Library.S2KModel.SM.GroupMan.AddPointToGroup(feGroup.Value.Name, feGroupJoint.Id.ToString());

                    // There is a restraint in the joint
                    if (feGroup.Value.Restraint == null) continue;

                    // Gets the SAP point
                    SapPoint sPnt = Sap2000Library.S2KModel.SM.PointMan.GetByName(feGroupJoint.Id.ToString());
                    // Sets the restraints to the point
                    sPnt.Restraints = new PointRestraintDef(feGroup.Value.Restraint.DoF);
                }
                foreach (FeFrame feGroupFrame in feGroup.Value.Frames)
                {
                    Sap2000Library.S2KModel.SM.GroupMan.AddFrameToGroup(feGroup.Value.Name, feGroupFrame.Id.ToString());
                }
            }


            // Gets the results
        }

        public override void RunAnalysis()
        {
            // Sets the run options
            Sap2000Library.S2KModel.SM.AnalysisMan.SetAllNotToRun();
            Sap2000Library.S2KModel.SM.AnalysisMan.SetCaseRunFlag("DEAD", true);
            Sap2000Library.S2KModel.SM.AnalysisMan.RunAnalysis();
        }

        public override void SaveDataAs(string inFilePath)
        {
            throw new NotImplementedException();
        }


    }
}
