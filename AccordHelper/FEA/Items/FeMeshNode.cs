using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Results;

namespace AccordHelper.FEA.Items
{
    public class FeMeshNode : IEquatable<FeMeshNode>
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

        public FeResult_NodalReactions Result_NodalReactions { get; set; } = null;
        public FeResult_ElementNodalBendingStrain Result_ElementNodalBendingStrains { get; set; } = null;
        public FeResult_ElementNodalStress Result_ElementNodalStress { get; set; } = null;
        public FeResult_ElementNodalForces Result_ElementNodalForces { get; set; } = null;
        public FeResult_ElementNodalStrain Result_ElementNodalStrains { get; set; } = null;
        public FeResult_NodalDisplacements Result_NodalDisplacements { get; set; } = null;
        public Dictionary<int, FeResult_SectionNode> Result_SectionNodes { get; private set; } = new Dictionary<int, FeResult_SectionNode>();
        public FeResult_ElementNodalCodeCheck Result_ElementNodalCodeCheck { get; set; } = null;

        public double Result_SectionNodes_AverageEqvStress()
        {
            return Result_SectionNodes.Values.Average(a => a.SEQV.Value);
        }
        public double Result_SectionNodes_MaxEqvStress()
        {
            return Result_SectionNodes.Values.Max(a => a.SEQV.Value);
        }
        public double Result_SectionNodes_MinEqvStress()
        {
            return Result_SectionNodes.Values.Min(a => a.SEQV.Value);
        }

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
            return Equals((FeMeshNode) obj);
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
    }
}
