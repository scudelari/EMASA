using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeGroup : IEquatable<FeGroup>, IFeEntity
    {
        public FeGroup(string inName)
        {
            Name = inName;
        }
        public string Name { get; private set; }

        public HashSet<FeFrame> Frames = new HashSet<FeFrame>();
        public HashSet<FeJoint> Joints = new HashSet<FeJoint>();

        public void AddElement(FeFrame inFrame)
        {
            Frames.Add(inFrame);
        }
        public void AddElement(FeJoint inJoint)
        {
            Joints.Add(inJoint);
        }

        public FeRelease Release { get; set; } = null;

        #region Equality - Based on Group Name
        public bool Equals(FeGroup other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
            }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeGroup)obj);
        }
        public override int GetHashCode()
        {
            return (GetType(),Name).GetHashCode();
        }
        public static bool operator ==(FeGroup left, FeGroup right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeGroup left, FeGroup right)
        {
            return !Equals(left, right);
        }
        #endregion

        public string WpfName => $"{GetType().Name} - {Name}";
    }
}
