extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Forms;
using r3dm::Rhino;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class Point_Input_ParamDef : Input_ParamDefBase
    {
        public override int VarCount => 3;
        public override string TypeName => "Point";

        public Point_Input_ParamDef(string inName, PointValueRange inRange) : base(inName)
        {
            SearchRange = inRange;
            Start = inRange.MidPoint;
        }

        private PointValueRange _searchRange;
        public PointValueRange SearchRange
        {
            get => _searchRange;
            set => SetProperty(ref _searchRange, value);
        }

        private Point3d _Start;
        public Point3d Start
        {
            get => _Start;
            set
            {
                if (!SearchRange.IsInside(value)) throw new Exception("Given start value must be within the search range.");
                SetProperty(ref _Start, value);

                RaisePropertyChanged("Start_X");
                RaisePropertyChanged("Start_Y");
                RaisePropertyChanged("Start_Z");
            }
        }

        public double Start_X
        {
            get => Start.X;
            set => Start = new Point3d(value, _Start.Y, _Start.Z);
        }
        public double Start_Y
        {
            get => Start.Y;
            set => Start = new Point3d(_Start.X, value, _Start.Z);
        }
        public double Start_Z
        {
            get => Start.Z;
            set => Start = new Point3d(_Start.X, _Start.Y, value);
        }
        #region UI Helpers
        #endregion
    }
}
