extern alias r3dm;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MathNet.Spatial.Euclidean;
using Rhino.Geometry;
using R3dmGeom = r3dm::Rhino.Geometry;

namespace RhinoInterfaceLibrary
{
    public static class RhinoStaticMethods
    {
        public static string GH_Auto_InputVariableFolder(string inGrasshopperFullFileName)
        {
            try
            {
                // Gets the document
                string targetDir = Path.Combine(GH_Auto_DataFolder(inGrasshopperFullFileName), "Input");

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                return targetDir;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GH_Auto_OutputVariableFolder(string inGrasshopperFullFileName)
        {
            try
            {
                // Gets the document
                string targetDir = Path.Combine(GH_Auto_DataFolder(inGrasshopperFullFileName), "Output");

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                return targetDir;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GH_Auto_DataFolder(string inGrasshopperFullFileName)
        {
            try
            {
                // Gets the document
                string projectFolder = Path.GetDirectoryName(inGrasshopperFullFileName);
                string ghFilename = Path.GetFileName(inGrasshopperFullFileName);

                string targetDir = Path.Combine(projectFolder, ghFilename + "_data");

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                return targetDir;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GH_Auto_SavedStateFileFull()
        {
            return Path.Combine(
                RhinoStaticMethods.GH_Auto_DataFolder(RhinoModel.RM.GrasshopperFullFileName),
                "problem.data");
        }

        public static Regex Point3dRegex = new Regex(@"(?<x>-?\d.*?),(?<y>-?\d.*?),(?<z>-?\d.*)");
        public static bool TryParsePoint3d(string inText, out R3dmGeom.Point3d outPoint)
        {
            Match m = Point3dRegex.Match(inText);

            if (!m.Success || !m.Groups["x"].Success || !m.Groups["y"].Success || !m.Groups["z"].Success)
            {
                outPoint = R3dmGeom.Point3d.Unset;
                return false;
            }

            if (!double.TryParse(m.Groups["x"].Value, out double x) ||
                !double.TryParse(m.Groups["y"].Value, out double y) ||
                !double.TryParse(m.Groups["z"].Value, out double z))
            {
                outPoint = R3dmGeom.Point3d.Unset;
                return false;
            }

            outPoint = new R3dmGeom.Point3d(x,y,z);
            return true;
        }

        public static Regex LineRegex = new Regex(@"(?<x1>-?\d.*?),(?<y1>-?\d.*?),(?<z1>-?\d.*)\t(?<x2>-?\d.*?),(?<y2>-?\d.*?),(?<z2>-?\d.*)");
        public static bool TryParseLine(string inText, out R3dmGeom.Line outLine)
        {
            Match m = LineRegex.Match(inText);

            if (!m.Success || !m.Groups["x1"].Success || !m.Groups["y1"].Success || !m.Groups["z1"].Success
                || !m.Groups["x2"].Success || !m.Groups["y2"].Success || !m.Groups["z2"].Success)
            {
                outLine = R3dmGeom.Line.Unset;
                return false;
            }

            if (!double.TryParse(m.Groups["x1"].Value, out double x1) ||
                !double.TryParse(m.Groups["y1"].Value, out double y1) ||
                !double.TryParse(m.Groups["z1"].Value, out double z1) ||
                !double.TryParse(m.Groups["x2"].Value, out double x2) ||
                !double.TryParse(m.Groups["y2"].Value, out double y2) ||
                !double.TryParse(m.Groups["z2"].Value, out double z2))
            {
                outLine = R3dmGeom.Line.Unset;
                return false;
            }

            outLine = new R3dmGeom.Line(x1, y1, z1, x2, y2, z2);
            return true;
        }

        public static Point3d ToRhinoCommonObject(this R3dmGeom.Point3d inPnt)
        {
            return new Point3d(inPnt.X,inPnt.Y,inPnt.Z);
        }

        public static List<R3dmGeom.Point3d> GetAllPoints(this List<R3dmGeom.Line> inLines)
        {
            HashSet<R3dmGeom.Point3d> points = new HashSet<R3dmGeom.Point3d>();

            foreach (R3dmGeom.Line line in inLines)
            {
                points.Add(line.From);
                points.Add(line.To);
            }

            return points.ToList();
        }
    }
}
