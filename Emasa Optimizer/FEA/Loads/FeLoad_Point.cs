extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.Opt.ParamDefinitions;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA.Loads
{
    public class FeLoad_Point : FeLoad
    {
        public FeLoad_Point(GhGeom_ParamDefBase inRelatedGeometry, Vector3d inNominal, double inMultiplier = 1d) : base(inMultiplier)
        {
            GhGeom = inRelatedGeometry;
            Nominal = inNominal;
        }

        public GhGeom_ParamDefBase GhGeom { get; }
        public Vector3d Nominal { get; }

        public void WpfCommand_Delete()
        {
            // Removes itself from the list
            AppSS.I.FeOpt.PointLoads.Remove(this);
        }
    }
}
