using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Results;

namespace AccordHelper.FEA.Items
{
    public class FeMeshBeamElement
    {
        public FeMeshBeamElement(int inId, FeMeshNode inINode, FeMeshNode inJNode, FeMeshNode inKNode)
        {
            _id = inId;
            _iNode = inINode;
            _jNode = inJNode;
            _kNode = inKNode;
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => _id = value;
        }

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

        public FeMeshNode GetNodeById(int inNodeId)
        {
            if (INode.Id == inNodeId) return INode;
            if (JNode.Id == inNodeId) return JNode;
            if (KNode.Id == inNodeId) return KNode;

            throw new Exception($"None of the nodes of element {this.Id} has the given Id {inNodeId}.");
        }

        public FeResult_ElementStrainEnergy ElementStrainEnergy { get; set; } = null;
    }
}
