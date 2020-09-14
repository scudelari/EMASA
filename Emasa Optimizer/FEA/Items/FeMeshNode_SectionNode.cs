using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeMeshNode_SectionNode : IFeEntity
    {
        public FeMeshNode_SectionNode(int inId)
        {
            Id = inId;
        }

        public int Id { get; set; }
    }
}
