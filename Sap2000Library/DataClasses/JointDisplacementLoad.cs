namespace Sap2000Library.DataClasses
{
    public class JointDisplacementLoad
    {
        public string LoadPatternName;
        public int LCStep;
        public double U1 = 0;
        public double U2 = 0;
        public double U3 = 0;
        public double R1 = 0;
        public double R2 = 0;
        public double R3 = 0;
        public string CSys = "Global";

        public double[] Values
        {
            get
            {
                return new double[] { U1, U2, U3, R1, R2, R3 };
            }
        }
    }
}
