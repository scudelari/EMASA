using System.Collections.Generic;
using Sap2000Library.DataClasses.Results;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class ResultManager : SapManagerBase
    {
        internal ResultManager(S2KModel model) : base(model) { }

        public bool DeselectAllCasesAndCombosForOutput()
        {
            return 0 != SapApi.Results.Setup.DeselectAllCasesAndCombosForOutput();
        }

        public bool SetCaseSelectedForOutput(string LCName, bool selected = true)
        {
            return 0 != SapApi.Results.Setup.SetCaseSelectedForOutput(LCName, selected);
        }
        public bool SetCaseSelectedForOutput(LoadCase LCName, bool selected = true)
        {
            return SetCaseSelectedForOutput(LCName.Name, selected);
        }

        public bool SetOptionMultiStepStatic(OptionMultiStepStatic option)
        {
            return 0 != SapApi.Results.Setup.SetOptionMultiStepStatic((int)option);
        }
        public bool SetOptionNLStatic(OptionNLStatic option)
        {
            return 0 != SapApi.Results.Setup.SetOptionNLStatic((int)option);
        }
        public bool SetOptionMultiValuedCombo(OptionMultiValuedCombo option)
        {
            return 0 != SapApi.Results.Setup.SetOptionMultiValuedCombo((int)option);
        }

        public List<JointReactionData> GetJointReaction(string JointName, ItemTypeElmEnum ItemTypeElm = ItemTypeElmEnum.Element)
        {
            int count = 0;
            string[] obj = null;
            string[] elm = null;
            string[] lc = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] F1 = null;
            double[] F2 = null;
            double[] F3 = null;
            double[] M1 = null;
            double[] M2 = null;
            double[] M3 = null;

            int ret = SapApi.Results.JointDispl(JointName, (eItemTypeElm)ItemTypeElm,
                ref count, ref obj, ref elm,
                ref lc, ref stepType, ref stepNum,
                ref F1, ref F2, ref F3, ref M1, ref M2, ref M3);

            if (ret != 0 || count == 0)
            {
                switch (ItemTypeElm)
                {
                    case ItemTypeElmEnum.ObjectElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for joint #{JointName}. Check if the joint exists and if the model is run.");
                    case ItemTypeElmEnum.Element:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for element #{JointName}. Check if the joint element exists and if the model is run.");
                    case ItemTypeElmEnum.GroupElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for group #{JointName}. Check if the group exists and if the model is run.");
                    case ItemTypeElmEnum.SelectionElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for current selection. Check if the joint exists and if the model is run.");
                    default:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for joint #{JointName}. Invalid Enum.");
                }
            }

            List<JointReactionData> toRet = new List<JointReactionData>();

            for (int i = 0; i < count; i++)
            {
                toRet.Add(new JointReactionData(this)
                {
                    Obj = obj[i],
                    Element = elm[i],
                    StepType = stepType[i],
                    LoadCase = lc[i],
                    StepNum = stepNum[i],
                    F1 = F1[i],
                    F2 = F2[i],
                    F3 = F3[i],
                    M1 = M1[i],
                    M2 = M2[i],
                    M3 = M3[i]
                });
            }

            return toRet;
        }

        public List<JointDisplacementData> GetJointDisplacement(string JointName, ItemTypeElmEnum ItemTypeElm = ItemTypeElmEnum.Element)
        {
            int count = 0;
            string[] obj = null;
            string[] elm = null;
            string[] lc = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] U1 = null;
            double[] U2 = null;
            double[] U3 = null;
            double[] R1 = null;
            double[] R2 = null;
            double[] R3 = null;

            int ret = SapApi.Results.JointDispl(JointName, (eItemTypeElm)ItemTypeElm,
                ref count, ref obj, ref elm,
                ref lc, ref stepType, ref stepNum,
                ref U1, ref U2, ref U3, ref R1, ref R2, ref R3);

            if (ret != 0 || count == 0)
            {
                switch (ItemTypeElm)
                {
                    case ItemTypeElmEnum.ObjectElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for joint #{JointName}. Check if the joint exists and if the model is run.");
                    case ItemTypeElmEnum.Element:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for element #{JointName}. Check if the joint element exists and if the model is run.");
                    case ItemTypeElmEnum.GroupElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for group #{JointName}. Check if the group exists and if the model is run.");
                    case ItemTypeElmEnum.SelectionElm:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for current selection. Check if the joint exists and if the model is run.");
                    default:
                        throw new S2KHelperException($"Could not get the Joint Displacement results for joint #{JointName}. Invalid Enum.");
                }
            }

            List<JointDisplacementData> toRet = new List<JointDisplacementData>();

            for (int i = 0; i < count; i++)
            {
                toRet.Add(new JointDisplacementData(this)
                {
                    Obj = obj[i],
                    Element = elm[i],
                    StepType = stepType[i],
                    LoadCase = lc[i],
                    StepNum = stepNum[i],
                    U1 = U1[i],
                    U2 = U2[i],
                    U3 = U3[i],
                    R1 = R1[i],
                    R2 = R2[i],
                    R3 = R3[i]
                });
            }

            return toRet;
        }

        // Attempts reading through the SAP2000 Automation

    }
}
