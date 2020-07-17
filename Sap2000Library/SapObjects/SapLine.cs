using System;
using System.Collections.Generic;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapLine : SapObject
    {
        internal SapLine(string name, SapObjectType type, SapPoint iendpoint, SapPoint jendpoint, SapManagerBase inManBase) : base(name, type, inManBase)
        {
            if (type != SapObjectType.Frame && type != SapObjectType.Link && type != SapObjectType.Cable)
            {
                throw new S2KHelperException("Invalid SapLine SapObjectType.");
            }

            iEndPoint = iendpoint;
            jEndPoint = jendpoint;
        }

        public SapPoint iEndPoint;
        public SapPoint jEndPoint;
        public List<SapPoint> BothPoints { get { return new List<SapPoint>() { iEndPoint, jEndPoint }; } }

        public SapPoint PointWithHighestZ
        {
            get
            {
                if (iEndPoint.Z > jEndPoint.Z) return iEndPoint;
                if (iEndPoint.Z < jEndPoint.Z) return jEndPoint;

                // If equal
                return null;
            }
        }
        public SapPoint PointWithLowestZ
        {
            get
            {
                if (iEndPoint.Z < jEndPoint.Z) return iEndPoint;
                if (iEndPoint.Z > jEndPoint.Z) return jEndPoint;
                
                // If equal
                return null;
            }
        }

        public Vector3D Vector
        {
            get
            {
                return iEndPoint.Point.VectorTo(jEndPoint.Point);
            }
        }
        public Vector3D VectorToHighestZ
        {
            get
            {
                return PointWithLowestZ.Point.VectorTo(PointWithHighestZ.Point);
            }
        }
        public Line3D Line
        {
            get
            {
                return new Line3D(iEndPoint.Point, jEndPoint.Point);
            }
        }

        public SapPoint OtherPoint(SapPoint point)
        {
            if (iEndPoint != point && jEndPoint != point)
                throw new S2KHelperException("The input point is not part of the line. It must be either the I or J point of the line so that you can get the other point.");

            if (iEndPoint == point) return jEndPoint;
            else return iEndPoint;
        }
        public bool IsPointIJ(SapPoint point)
        {
            try
            {
                if (iEndPoint.Name == point.Name || jEndPoint.Name == point.Name) return true;
                else return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool IsPointIJ(Point3D inPnt)
        {
            try
            {
                if (iEndPoint.Point == inPnt || jEndPoint.Point == inPnt) return true;
                else return false;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public SapPoint GetIOrJClosestTo(Point3D inPnt)
        {
            double distanceI = iEndPoint.Point.DistanceTo(inPnt);
            double distanceJ = jEndPoint.Point.DistanceTo(inPnt);

            if (distanceI == distanceJ) throw new S2KHelperException($"The point is equally distant from I and J.");

            if (distanceI < distanceJ) return iEndPoint;
            else return jEndPoint;
        }
        public bool IsColinearTo(SapLine inSapLine, Angle? angleTolerance = null, double inDistanceSlack = 0.05)
        {
            if (angleTolerance == null) angleTolerance = Angle.FromDegrees(0.5d);

            if (!Line.IsParallelTo(inSapLine.Line, angleTolerance.Value )) return false;

            Tuple<Point3D, Point3D> closestPair = Line.ClosestPointsBetween(inSapLine.Line);
            if (closestPair.Item1 != closestPair.Item2)
            {
                // Gives a slack
                if (closestPair.Item1.DistanceTo(closestPair.Item2) > inDistanceSlack) return false;
            }
            return true;
        }
        public Point3D Centroid { get { return Point3D.Centroid(iEndPoint.Point, jEndPoint.Point); } }

        public double Length
        {
            get { return Line.Length; }
        }

        public override bool Equals(object obj)
        {
            if (obj is SapLine inLine)
            {
                if (Name == inLine.Name && SapType == inLine.SapType) return true;
                else return false;
            }

            return false;
        }
        public static bool operator ==(SapLine lhs, SapLine rhs)
        {

            // If left hand side is null...
            if (ReferenceEquals(lhs, null))
            {
                // ...and right hand side is null...
                if (ReferenceEquals(rhs, null))
                {
                    //...both are null and are Equal.
                    return true;
                }

                // ...right hand side is not null, therefore not Equal.
                return false;
            }

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SapLine lhs, SapLine rhs)
        {
            return !(lhs == rhs);
        }
        public override int GetHashCode()
        {
            return (Name, iEndPoint.Name, jEndPoint.Name, SapType.ToString()).GetHashCode();
        }

        public override bool ChangeConnectivity(SapPoint oldPoint, SapPoint newPoint)
        {
            if (!IsPointIJ(oldPoint)) throw new S2KHelperException($"The old point must belong to the line!{Environment.NewLine}Point {oldPoint.Name} does not belong to the line {Name}.");

            switch (SapType)
            {
                case SapObjectType.Frame:
                    int ret = -1;
                    if (iEndPoint == oldPoint) ret = b_owner.SapApi.EditFrame.ChangeConnectivity(Name, newPoint.Name, jEndPoint.Name);
                    if (jEndPoint == oldPoint) ret = b_owner.SapApi.EditFrame.ChangeConnectivity(Name, iEndPoint.Name, newPoint.Name);
                    return ret == 0;
                case SapObjectType.Link:
                    throw new S2KHelperException($"Link {Name} cannot be changed yet. The logic must recreate the link as there is no change connectivity for links.");
                case SapObjectType.Cable:
                    throw new S2KHelperException($"Cables cannot have their connectivity changed.");
            }
            return false;
        }
    }
}
