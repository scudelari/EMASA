using System.Linq;
using MathNet.Spatial.Euclidean;

namespace Sap2000Library.DataClasses
{
    public class PointAdvancedLocalAxesDef
    {
        public bool Active;

        public AdvancedAxesAngle_Vector AxVectOpt = AdvancedAxesAngle_Vector.UserVector;
        public int AxVectOpt_int { get { return (int)AxVectOpt; } }
        public AdvancedAxesAngle_Vector PlVectOpt = AdvancedAxesAngle_Vector.UserVector;
        public int PlVectOpt_int { get { return (int)PlVectOpt; } }

        public string AxCSys = "GLOBAL";
        public string PlCSys = "GLOBAL";

        public int[] AxDir_int = new int[] { 1, 2 };
        public AdvancedAxesAngle_PlaneReference[] AxDir
        {
            get { return AxDir_int.Cast<AdvancedAxesAngle_PlaneReference>().ToArray(); }
            set { AxDir_int = value.Cast<int>().ToArray(); }
        }

        public int[] PlDir_int = new int[] { 1, 2 };
        public AdvancedAxesAngle_PlaneReference[] PlDir
        {
            get { return PlDir_int.Cast<AdvancedAxesAngle_PlaneReference>().ToArray(); }
            set { PlDir_int = value.Cast<int>().ToArray(); }
        }

        public string[] AxPt = new string[] { "", "" };
        public string[] PlPt = new string[] { "", "" };

        public double[] AxVect = new double[] { 0, 0, 0 };
        public Vector3D AxVect_Vector
        {
            get
            {
                return new Vector3D(AxVect[0], AxVect[1], AxVect[2]);
            }
            set
            {
                AxVect = new double[] { value.X, value.Y, value.Z };
            }
        }
        public double[] PlVect = new double[] { 0, 0, 0 };
        public Vector3D PlVect_Vector
        {
            get
            {
                return new Vector3D(PlVect[0], PlVect[1], PlVect[2]);
            }
            set
            {
                PlVect = new double[] { value.X, value.Y, value.Z };
            }
        }

        public PointAdvancedAxes_Plane2 Plane2 = PointAdvancedAxes_Plane2.Plane12;
        public int Plane2_int { get { return (int)Plane2; } }

        public PointAdvancedLocalAxesDef() { }
        public PointAdvancedLocalAxesDef(Vector3D inZDirection)
        {
            Active = true;
            AxVectOpt = AdvancedAxesAngle_Vector.UserVector;
            PlVectOpt = AdvancedAxesAngle_Vector.UserVector;
            AxCSys = "GLOBAL";
            PlCSys = "GLOBAL";
            AxPt = new string[] { "", "" };
            PlPt = new string[] { "", "" };
            AxVect = new double[] { inZDirection.X, inZDirection.Y, inZDirection.Z };
            PlVect = new double[] { 0d, 0d, 1d };
            Plane2 = PointAdvancedAxes_Plane2.Plane31;
        }

        public static PointAdvancedLocalAxesDef NotSet
        {
            get
            {
                return new PointAdvancedLocalAxesDef() { Active = false };
            }
        }
    }
}
