using System;
using System.Collections.Generic;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeFrame : IEquatable<FeFrame>, IFeEntity
    {
        public FeFrame(int inId, FeSection inSection, FeJoint inIJoint, FeJoint inJJoint)
        {
            _id = inId;
            _section = inSection;
            _iJoint = inIJoint;
            _jJoint = inJJoint;
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => _id = value;
        }
        
        private FeSection _section;
        public FeSection Section
        {
            get => _section;
            set => _section = value;
        }

        private FeJoint _iJoint;
        public FeJoint IJoint
        {
            get => _iJoint;
            set => _iJoint = value;
        }

        private FeJoint _jJoint;
        public FeJoint JJoint
        {
            get => _jJoint;
            set => _jJoint = value;
        }

        public List<FeJoint> Joints => new List<FeJoint>() {IJoint, JJoint};

        public List<FeMeshBeamElement> MeshBeamElements { get; } = new List<FeMeshBeamElement>();

        #region Equality - Based on ID
        public bool Equals(FeFrame other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _id == other._id;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeFrame)obj);
        }
        public override int GetHashCode()
        {
            return (GetType(), Id).GetHashCode();
        }
        public static bool operator ==(FeFrame left, FeFrame right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeFrame left, FeFrame right)
        {
            return !Equals(left, right);
        } 
        #endregion


        public override string ToString()
        {
            return $"Frame {Id}<{Section}>: From [{IJoint}] To [{JJoint}]";
        }

        public string WpfName => $"{GetType().Name} - {Id}";
    }
}
