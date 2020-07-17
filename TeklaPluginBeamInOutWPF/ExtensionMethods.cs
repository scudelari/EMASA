using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TSDatatype = Tekla.Structures.Datatype;
using TSModel = Tekla.Structures.Model;
using TSPlugins = Tekla.Structures.Plugins;
using TSGeom = Tekla.Structures.Geometry3d;

namespace TeklaPluginInOutWPF
{
    public static class ExtensionMethods
    {
        public static string ToStringCustom(this TSModel.Offset inOffset)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('(');
            sb.Append(inOffset.Dx);
            sb.Append(',');

            sb.Append(inOffset.Dy);
            sb.Append(',');

            sb.Append(inOffset.Dz);
            sb.Append(')');

            return sb.ToString();
        }
        public static string ToStringCustom(this TSModel.Position inPos)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(inPos.Depth.ToString());
            sb.Append("%%%");

            sb.Append(inPos.DepthOffset);
            sb.Append("%%%");

            sb.Append(inPos.Plane.ToString());
            sb.Append("%%%");

            sb.Append(inPos.PlaneOffset);
            sb.Append("%%%");

            sb.Append(inPos.Rotation.ToString());
            sb.Append("%%%");

            sb.Append(inPos.RotationOffset);
            sb.Append("%%%");

            return sb.ToString();
        }
        public static string ToStringCustom(this TSModel.Contour inContour)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in inContour.ContourPoints)
            {
                TSModel.ContourPoint pnt = item as TSModel.ContourPoint;
                if (pnt == null) throw new InvalidOperationException("All Contour Points must be of type TSModel.ContourPoint.");

                sb.Append(pnt.ToString());
                sb.Append("%%%");
            }

            return sb.ToString();
        }

        public static TSModel.Offset OffsetFromStringCustom(string inString)
        {
            // Breaks the parts of the string
            string[] parts = inString.Split(new string[] { "(", ",", ")" }, StringSplitOptions.RemoveEmptyEntries);

            TSModel.Offset newOffset = new TSModel.Offset();

            newOffset.Dx = Double.Parse(parts[0]);
            newOffset.Dy = Double.Parse(parts[1]);
            newOffset.Dz = Double.Parse(parts[2]);

            return newOffset;
        }
        public static TSGeom.Point PointFromStringCustom(string inString)
        {
            // Breaks the parts of the string
            string[] parts = inString.Split(new string[] { "(", ",", ")" }, StringSplitOptions.RemoveEmptyEntries);

            TSGeom.Point newPoint = new TSGeom.Point(Double.Parse(parts[0]), Double.Parse(parts[1]), Double.Parse(parts[2]));

            return newPoint;
        }
        public static TSModel.Position PositionFromStringCustom(string inString)
        {
            // Breaks the parts of the string
            string[] parts = inString.Split(new string[] { "%%%" }, StringSplitOptions.RemoveEmptyEntries);

            TSModel.Position newPos = new TSModel.Position();

            newPos.Depth = (TSModel.Position.DepthEnum)Enum.Parse(typeof(TSModel.Position.DepthEnum), parts[0]);
            newPos.DepthOffset = Double.Parse(parts[1]);

            newPos.Plane = (TSModel.Position.PlaneEnum)Enum.Parse(typeof(TSModel.Position.PlaneEnum), parts[2]);
            newPos.PlaneOffset = Double.Parse(parts[3]);

            newPos.Rotation = (TSModel.Position.RotationEnum)Enum.Parse(typeof(TSModel.Position.RotationEnum), parts[4]);
            newPos.RotationOffset = Double.Parse(parts[5]);

            return newPos;
        }
        public static TSModel.Contour ContourFromStringCustom(string inString)
        {
            // Breaks the parts of the string
            string[] points = inString.Split(new string[] { "%%%" }, StringSplitOptions.RemoveEmptyEntries);

            TSModel.Contour newContour = new TSModel.Contour();

            foreach (string pointString in points)
            {
                TSModel.ContourPoint cPnt = new TSModel.ContourPoint();
                cPnt.SetPoint(PointFromStringCustom(pointString));

                newContour.AddContourPoint(cPnt);
            }

            return newContour;
        }
    }
}

