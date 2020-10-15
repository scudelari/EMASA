using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeMeshNode_SectionNode : IFeEntity, IEquatable<FeMeshNode_SectionNode>
    {
        public FeMeshNode_SectionNode(int inId)
        {
            Id = inId;
        }

        public int Id { get; set; }

        public FeMeshNode OwnerMeshNode { get; set; }

        #region Equality - Based on ID and on Owner Mesh Node
        public bool Equals(FeMeshNode_SectionNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Equals(OwnerMeshNode, other.OwnerMeshNode);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeMeshNode_SectionNode)obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (OwnerMeshNode != null ? OwnerMeshNode.GetHashCode() : 0);
            }
        }
        public static bool operator ==(FeMeshNode_SectionNode left, FeMeshNode_SectionNode right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeMeshNode_SectionNode left, FeMeshNode_SectionNode right)
        {
            return !Equals(left, right);
        }
        #endregion

        public string WpfName => $"{GetType().Name} - {Id}";
    }
}
