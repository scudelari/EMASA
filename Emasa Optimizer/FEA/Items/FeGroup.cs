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
            return StringComparer.InvariantCulture.GetHashCode(Name);
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
    }
}
