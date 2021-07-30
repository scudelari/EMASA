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
    public class FeMeshBeamElement : IFeEntity, IEquatable<FeMeshBeamElement>
    {
        public FeMeshBeamElement(string inId, FeMeshNode inINode, FeMeshNode inJNode, FeMeshNode inKNode)
        {
            _id = inId;
            _iNode = inINode;
            _jNode = inJNode;
            _kNode = inKNode;
        }

        private string _id;
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public FeFrame OwnerFrame { get; set; }

        private FeMeshNode _iNode;
        public FeMeshNode INode
        {
            get => _iNode;
            set => _iNode = value;
        }

        private FeMeshNode _jNode;
        public FeMeshNode JNode
        {

            get => _jNode;
            set => _jNode = value;
        }

        private FeMeshNode _kNode;
        public FeMeshNode KNode
        {
            get => _kNode;
            set => _kNode = value;
        }

        public double Length => (new Line(INode.Point, JNode.Point)).Length;
        public double Mass => (Length * OwnerFrame.Section.Area * OwnerFrame.Section.Material.Density);

        public List<FeMeshNode> MeshNodes => new List<FeMeshNode>() {INode, KNode, JNode};
        public List<FeMeshNode> MeshNodes_NoK => new List<FeMeshNode>() { INode, JNode };

        public FeMeshNode GetNodeById(string inNodeId)
        {
            if (INode.Id == inNodeId) return INode;
            if (JNode.Id == inNodeId) return JNode;
            if (KNode.Id == inNodeId) return KNode;

            throw new Exception($"None of the nodes of element {this.Id} has the given Id {inNodeId}.");
        }

        #region Equality = Based on the ID
        public bool Equals(FeMeshBeamElement other)
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
            return Equals((FeMeshBeamElement)obj);
        }
        public override int GetHashCode()
        {
            return (GetType(), Id).GetHashCode();
        }
        public static bool operator ==(FeMeshBeamElement left, FeMeshBeamElement right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeMeshBeamElement left, FeMeshBeamElement right)
        {
            return !Equals(left, right);
        }
        #endregion

        public override string ToString()
        {
            return $"MeshBeamElement {Id} - I:{INode.Id} - J:{JNode.Id} - K:{KNode.Id}";
        }

        public string WpfName => $"{GetType().Name} - {Id}";
    }
}
