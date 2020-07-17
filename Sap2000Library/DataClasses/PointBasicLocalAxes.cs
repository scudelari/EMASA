namespace Sap2000Library.DataClasses
{
    /// <summary>
    ///a, b, c
    ///The local axes of the point are defined by first setting the positive local 1, 2 and 3 axes the same as the positive global X, Y and Z axes and then doing the following: [deg]
    ///1.    Rotate about the 3 axis by angle a.
    ///2.    Rotate about the resulting 2 axis by angle b.
    ///3.    Rotate about the resulting 1 axis by angle c.
    /// </summary>
    public class PointBasicLocalAxesDef
    {
        /// <summary>
        /// 1. Rotate about the 3 axis by angle a. [DEG]
        /// </summary>
        public double A;
        /// <summary>
        /// 2. Rotate about the resulting 2 axis by angle b. [DEG]
        /// </summary>
        public double B;
        /// <summary>
        /// 3. Rotate about the resulting 1 axis by angle c. [DEG]
        /// </summary>
        public double C;
        public bool Advanced;

        public static PointBasicLocalAxesDef Default
        {
            get
            {
                return new PointBasicLocalAxesDef() { A = 0, B=0, C=0, Advanced = false };
            }
        }
        public static PointBasicLocalAxesDef NoRotationWithAdvanced
        {
            get
            {
                return new PointBasicLocalAxesDef() { A = 0, B = 0, C = 0, Advanced = true };
            }
        }
    }
}
