using Sap2000Library.Managers;

namespace Sap2000Library.DataClasses.Results
{
    public class JointReactionData
    {
        private ResultManager owner = null;

        internal JointReactionData(ResultManager owner) { this.owner = owner; }
        internal JointReactionData(string inObj, string inElem, string inLoadCase, string inStepType,
            double inStepNum, double inF1, double inF2, double inF3, double inM1, double inM2, double inM3, ResultManager owner)
        {
            Obj = inObj;
            Element = inElem;
            LoadCase = inLoadCase;
            StepType = inStepType;
            StepNum = inStepNum;
            F1 = inF1;
            F2 = inF2;
            F3 = inF3;
            M1 = inM1;
            M2 = inM2;
            M3 = inM3;

            this.owner = owner;
        }

        public string Obj { get; set; }
        public string Element { get; set; }
        public string LoadCase { get; set; }
        public string StepType { get; set; }
        public double StepNum { get; set; }
        public double F1 { get; set; }
        public double F2 { get; set; }
        public double F3 { get; set; }
        public double M1 { get; set; }
        public double M2 { get; set; }
        public double M3 { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is JointReactionData inObject)
            {
                if (Obj == inObject.Obj &&
                    Element == inObject.Element &&
                    LoadCase == inObject.LoadCase &&
                    StepType == inObject.StepType &&
                    StepNum == inObject.StepNum) return true;
                else return false;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return (Obj, Element, LoadCase, StepType, StepNum).GetHashCode();
        }
    }
}
