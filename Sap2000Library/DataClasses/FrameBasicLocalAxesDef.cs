namespace Sap2000Library.DataClasses
{
    public class FrameBasicLocalAxesDef
    {
        /// <summary>
        /// Rotation around local axes 1
        /// </summary>
        public double Ang;
        public bool Advanced;

        public static FrameBasicLocalAxesDef Default
        {
            get
            {
                return new FrameBasicLocalAxesDef() { Ang = 0, Advanced = false };
            }
        }
    }
}
