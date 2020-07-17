namespace Sap2000Library.DataClasses
{
    public class PointForceLoad
    {
        public string LoadPattern;
        public int LCStep;
        public string CSys = "Global";
        public double F1 = 0;
        public double F2 = 0;
        public double F3 = 0;
        public double M1 = 0;
        public double M2 = 0;
        public double M3 = 0;

        public double[] Values
        {
            get
            {
                return new double[] { F1, F2, F3, M1, M2, M3 };
            }
        }
    }
}
