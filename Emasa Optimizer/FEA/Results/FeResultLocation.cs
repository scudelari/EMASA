using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.FEA.Results
{
    public class FeResultLocation
    {
        private FeResultLocation() { }

        public static FeResultLocation CreateModelLocation([NotNull] FeModel inModel)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));

            return new FeResultLocation()
                {
                Model = inModel,
                };
        }
        public static FeResultLocation CreateMeshNodeLocation([NotNull] FeModel inModel, [NotNull] FeMeshNode inNode)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));
            if (inNode == null) throw new ArgumentNullException(nameof(inNode));

            return new FeResultLocation()
                {
                Model = inModel,
                MeshNode = inNode
                };
        }
        public static FeResultLocation CreateSectionNodeLocation([NotNull] FeModel inModel, [NotNull] FeMeshBeamElement inBeam, [NotNull] FeMeshNode inNode, [NotNull] FeMeshNode_SectionNode inSectionNode)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));
            if (inNode == null) throw new ArgumentNullException(nameof(inNode));
            if (inBeam == null) throw new ArgumentNullException(nameof(inBeam));
            if (inSectionNode == null) throw new ArgumentNullException(nameof(inSectionNode));

            return new FeResultLocation()
                {
                Model = inModel,
                MeshNode = inNode,
                MeshBeam = inBeam,
                SectionNode = inSectionNode
                };
        }
        public static FeResultLocation CreateElementNodeLocation([NotNull] FeModel inModel, [NotNull] FeMeshBeamElement inBeam, [NotNull] FeMeshNode inNode)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));
            if (inNode == null) throw new ArgumentNullException(nameof(inNode));
            if (inBeam == null) throw new ArgumentNullException(nameof(inBeam));

            return new FeResultLocation()
                {
                Model = inModel,
                MeshNode = inNode,
                MeshBeam = inBeam,
                };
        }
        public static FeResultLocation CreateElementLocation([NotNull] FeModel inModel, [NotNull] FeMeshBeamElement inBeam)
        {
            if (inModel == null) throw new ArgumentNullException(nameof(inModel));
            if (inBeam == null) throw new ArgumentNullException(nameof(inBeam));

            return new FeResultLocation()
                {
                Model = inModel,
                MeshBeam = inBeam,
                };
        }

        public FeModel Model { get; set; }
        public FeFrame Frame { get; set; }
        public FeJoint Joint { get; set; }
        public FeMeshNode MeshNode { get; set; }
        public FeMeshBeamElement MeshBeam { get; set; }
        public FeMeshNode_SectionNode SectionNode { get; set; }

        #region Wpf
        public bool IsINode => MeshBeam != null && MeshNode != null && MeshBeam.INode == MeshNode;
        public bool IsJNode => MeshBeam != null && MeshNode != null && MeshBeam.JNode == MeshNode;
        public bool IsKNode => MeshBeam != null && MeshNode != null && MeshBeam.KNode == MeshNode;

        public string BeamNodeString
        {
            get
            {
                if (IsINode) return "I";
                if (IsJNode) return "J";
                if (IsKNode) return "K";
                return string.Empty;
            }
        }
        #endregion
    }
}
