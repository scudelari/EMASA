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
    public class GHEmsFilter_LinesParallelToPlane : GH_Component
    {
        public override Guid ComponentGuid => new Guid("3472a778-ad5f-4713-b6c1-2db5f469c037");

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_Filter_LinesParallelToPlane;
        public Image PublicIcon => Icon;

        public GHEmsFilter_LinesParallelToPlane() :
            base("Filter Lines Parallel to Plane", "FLPP", "Filters all lines that are parallel to given planes.", "Emasa", "Filter")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Input", "I", "Lines to Filter.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "Planes", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Output", "F", "Filtered Lines", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            List<Plane> planes = new List<Plane>();

            // Gets the input parameters
            if (!DA.GetDataList(0, lines) || lines.Count == 0) return;
            if (!DA.GetDataList(1, planes) || planes.Count == 0) return;

            List<Line> filtered = new List<Line>(lines.Where(l => planes.All(p => Math.Abs(p.DistanceTo(l.From) - p.DistanceTo(l.To)) < GHEMSStaticMethods.Tolerance)));

            DA.SetDataList(0, filtered);
        }
    }
}
