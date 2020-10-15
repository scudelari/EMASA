extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.FEA.Results;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeMeshNode : IEquatable<FeMeshNode>, IFeEntity
    {
        public FeMeshNode(int inId, Point3d inPoint, FeJoint inMatchingJoint = null)
        {
            Id = inId;
            Point = inPoint;
            if (inMatchingJoint != null) MatchingJoint = inMatchingJoint;
        }

        public int Id { get; set; }

        private Point3d _point;
        public Point3d Point
        {
            get => _point;
            set => _point = value;
        }

        public FeJoint MatchingJoint { get; set; }

        public HashSet<FeMeshBeamElement> LinkedElements { get; set; } = new HashSet<FeMeshBeamElement>();

        public string LinkedElementsString
        {
            get
            {
                string s = "";
                LinkedElements.Aggregate(s, (inS, inElement) => s += inElement + " | ");
                s = s.Substring(0, s.Length - 3);
                return s;
            }
        }

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
                sn = new FeMeshNode_SectionNode(inId)
                        {OwnerMeshNode = this};

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
            return (GetType(),Id).GetHashCode();
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

        public override string ToString()
        {
            string s = "";
            LinkedElements.Aggregate(s, (inS, inElement) => s += inElement + " | ");
            s = s.Substring(0, s.Length - 3);
            return $"MeshNode {Id}: Linked BeamElements: [ {LinkedElementsString} ]";
        }

        public string WpfName => $"{GetType().Name} - {Id}";
    }
}
