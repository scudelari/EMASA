extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA.Items
{
    [Serializable]
    public class FeJoint : IEquatable<FeJoint>, IFeEntity
    {
        public FeJoint(string inId, Point3d inPoint)
        {
            _id = inId;
            _point = inPoint;
        }

        private string _id;
        public string Id
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

        private FeRestraint _restraint = new FeRestraint();
        public FeRestraint Restraint
        {
            get => _restraint;
            set => _restraint = value;
        }
        
        public void IncorporateRestraint(FeRestraint inRestraint)
        {
            if (inRestraint.ExistAny)
            {

            }
        }

        #region Equality - Based on the Point (which is based on coordinates, not reference)
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
            return Equals((FeJoint)obj);
        }
        public override int GetHashCode()
        {
            return (GetType(), _point).GetHashCode();
        }
        public static bool operator ==(FeJoint left, FeJoint right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeJoint left, FeJoint right)
        {
            return !Equals(left, right);
        } 
        #endregion

        public override string ToString()
        {
            return $"Joint {Id}: {Point}";
        }

        public string WpfName => $"{GetType().Name} - {Id}";
    }
}
