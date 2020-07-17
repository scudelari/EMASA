extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using r3dm::Rhino.Geometry;

namespace AccordHelper.FEA.Items
{
    [Serializable]
    public class FeJoint : IEquatable<FeJoint>
    {
        public FeJoint(int inId, Point3d inPoint)
        {
            _id = inId;
            _point = inPoint;
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        private Point3d _point;
        public Point3d Point
        {
            get => _point;
            set => _point = value;
        }

        public bool Equals(FeJoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _point.Equals(other._point);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeJoint) obj);
        }

        public override int GetHashCode()
        {
            return _point.GetHashCode();
        }

        public static bool operator ==(FeJoint left, FeJoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FeJoint left, FeJoint right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"{Id}: {Point}";
        }
    }
}
