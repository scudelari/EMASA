using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.FEA.Results;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeMeshNode : IEquatable<FeMeshNode>, IFeEntity
    {
        public FeMeshNode(int inId, double inX, double inY, double inZ)
        {
            Id = inId;
            X = inX;
            Y = inY;
            Z = inZ;
        }

        public int Id { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public List<FeMeshNode_SectionNode> SectionNodes { get; } = new List<FeMeshNode_SectionNode>();
        public FeMeshNode_SectionNode GetSectionNodebyId(int inId)
        {
            return SectionNodes.FirstOrDefault(a => a.Id == inId);
        }
        public FeMeshNode_SectionNode SectionNodes_AddNewOrGet(int inId)
        {
            FeMeshNode_SectionNode sn = SectionNodes.FirstOrDefault(a => a.Id == inId);

            if (sn == null)
            {
                sn = new FeMeshNode_SectionNode(inId);
                SectionNodes.Add(sn);
            }

            return sn;
        }

        #region Equality - Based on ID
        public bool Equals(FeMeshNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeMeshNode)obj);
        }
        public override int GetHashCode()
        {
            return Id;
        }
        public static bool operator ==(FeMeshNode left, FeMeshNode right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeMeshNode left, FeMeshNode right)
        {
            return !Equals(left, right);
        } 
        #endregion
    }
}
