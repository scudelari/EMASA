namespace Sap2000Library.DataClasses
{
    public class PointGroundDisplacementLoad
    {
        public string LoadPattern;
        public int LCStep;
        public string CSys;
        public double U1;
        public double U2;
        public double U3;
        /// <summary>
        /// In Radians! [RAD]
        /// </summary>
        public double R1;
        /// <summary>
        /// In Radians! [RAD]
        /// </summary>
        public double R2;
        /// <summary>
        /// In Radians! [RAD]
        /// </summary>
        public double R3;
    }
}
