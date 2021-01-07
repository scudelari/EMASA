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
    public class GHEmsFilter_LinesParallelToLine : GH_Component
    {
        public override Guid ComponentGuid => new Guid("fedc3bd0-777e-4ac3-afce-1070057308ad");

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_Filter_LinesParallelToLine;
        public Image PublicIcon => Icon;

        public GHEmsFilter_LinesParallelToLine() :
            base("Filter Lines Parallel to Line", "FLPL", "Filters all lines that are parallel to given line.", "Emasa", "Filter")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Input", "I", "Lines to Filter.", GH_ParamAccess.list);
            pManager.AddLineParameter("Line", "L", "Line", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Output", "F", "Filtered Lines", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            List<Line> target = new List<Line>();

            // Gets the input parameters
            if (!DA.GetDataList(0, lines) || lines.Count == 0) return;
            if (!DA.GetDataList(1, target) || target.Count == 0) return;

            if (target.Count != 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Only one line may be used as filter target.");
                return;
            }

            List<Line> filtered = new List<Line>(lines.Where(l =>
            {
                double a = Math.Abs(Vector3d.VectorAngle(l.Direction, target[0].Direction));

                // is the angle either 0 deg or 180 deg?
                return a < GHEMSStaticMethods.Tolerance || (a - Math.PI) < GHEMSStaticMethods.Tolerance;
            }));

            DA.SetDataList(0, filtered);
        }
    }
}
