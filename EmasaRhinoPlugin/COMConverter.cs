using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace EmasaRhinoPlugin
{
    public static class COMConverter
    {

        public static Point3d COMConvert_FromArrayToPoint3d(this double[] inCoords)
        {
            if (inCoords == null || inCoords.Length != 3)
                throw new InvalidOperationException("The coordinate array must contain 3 values.");

            return new Point3d(inCoords[0], inCoords[1], inCoords[2]);
        }
        public static double[] COMConvert_FromPoint3dToArray(this Point3d inPnt)
        {
            return new double[] { inPnt.X, inPnt.Y, inPnt.Z };
        }

        public static Vector3d COMConvert_FromArrayToVector3d(this double[] inCoords)
        {
            if (inCoords == null || inCoords.Length != 3)
                throw new InvalidOperationException("The coordinate array must contain 3 values.");

            return new Vector3d(inCoords[0], inCoords[1], inCoords[2]);
        }
        public static double[] COMConvert_FromVector3dToArray(this Vector3d inVector)
        {
            return new double[] { inVector.X, inVector.Y, inVector.Z };
        }
    }
}
