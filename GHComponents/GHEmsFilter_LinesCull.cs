using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GHComponents
{
    public class GHEmsFilter_LinesCull : GH_Component
    {
        public override Guid ComponentGuid => new Guid("5b740275-7a8f-4f00-aff2-e633a62cbeee");

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_Filter_LinesParallelToLine;
        public Image PublicIcon => Icon;

        public GHEmsFilter_LinesCull() :
            base("Cull Lines from Set", "CL", "Filters all lines from input set that exist in filter set.", "Emasa", "Filter")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Input", "I", "Lines to Filter.", GH_ParamAccess.list);
            pManager.AddLineParameter("ToCull", "C", "Lines To Remove", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Output", "F", "Filtered Lines", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            List<Line> cull = new List<Line>();

            // Gets the input parameters
            if (!DA.GetDataList(0, lines) || lines.Count == 0) return;
            if (!DA.GetDataList(1, cull) || cull.Count == 0) return;

            List<Line> filtered = new List<Line>(lines.Where(l => !cull.Any(c => l.From.DistanceTo(c.From) < GHEMSStaticMethods.Tolerance && l.To.DistanceTo(c.To) < GHEMSStaticMethods.Tolerance)));

            DA.SetDataList(0, filtered);
        }
    }
}
