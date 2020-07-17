using System.Linq;
using MathNet.Spatial.Euclidean;

namespace Sap2000Library.DataClasses
{
    public class FrameAdvancedLocalAxesDef
    {
        public bool Active;

        public FrameAdvancedAxes_Plane2 Plane2 = FrameAdvancedAxes_Plane2.Plane12;
        public int Plane2_int { get { return (int)Plane2; } }

        public AdvancedAxesAngle_Vector PlVectOpt = AdvancedAxesAngle_Vector.UserVector;
        public int PlVectOpt_int { get { return (int)PlVectOpt; } }

        public string PlCSys = "GLOBAL";

        public int[] PlDir_int = new int[] { 1, 2 };
        public AdvancedAxesAngle_PlaneReference[] PlDir
        {
            get { return PlDir_int.Cast<AdvancedAxesAngle_PlaneReference>().ToArray(); }
            set { PlDir_int = value.Cast<int>().ToArray(); }
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

        public string[] PlPt = new string[] { "", "" };

        public FrameAdvancedLocalAxesDef() { }
        public FrameAdvancedLocalAxesDef(Vector3D inZDirection, FrameAdvancedAxes_Plane2 inPlane = FrameAdvancedAxes_Plane2.Plane12)
        {
            Active = true;
            PlVectOpt = AdvancedAxesAngle_Vector.UserVector;
            PlCSys = "GLOBAL";
            PlVect = new double[] { inZDirection.X, inZDirection.Y, inZDirection.Z };
            Plane2 = inPlane;
        }

        public static FrameAdvancedLocalAxesDef NotSet
        {
            get
            {
                return new FrameAdvancedLocalAxesDef()
                {
                    Active = false
                };
            }
        }
    }
}
