extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.Helpers.Accord;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    [Serializable]
    public class PointValueRange : ValueRangeBase
    {
        public PointValueRange(Point3d inMin, Point3d inMax) : this(new DoubleRange(inMin.X, inMax.X), new DoubleRange(inMin.Y, inMax.Y), new DoubleRange(inMin.Z, inMax.Z))
        {
        }

        public PointValueRange(DoubleRange inX, DoubleRange inY, DoubleRange inZ)
        {
            _rangeX = inX;
            _rangeY = inY;
            _rangeZ = inZ;
        }

        private DoubleRange _rangeX;
        public DoubleRange RangeX
        {
            get => _rangeX;
            set
            {
                SetProperty(ref _rangeX, value);
                DisplayVariablesChanged();
            }
        }
        private DoubleRange _rangeY;
        public DoubleRange RangeY
        {
            get => _rangeY;
            set
            {
                SetProperty(ref _rangeY, value);
                DisplayVariablesChanged();
            }
        }
        private DoubleRange _rangeZ;
        public DoubleRange RangeZ
        {
            get => _rangeZ;
            set
            {
                SetProperty(ref _rangeZ, value);
                DisplayVariablesChanged();
            }
        }

        public Point3d MinPoint => new Point3d(RangeX.Min, RangeY.Min, RangeZ.Min);
        public Point3d MaxPoint => new Point3d(RangeX.Max, RangeY.Max, RangeZ.Max);
        public Point3d MidPoint => new Point3d(
            (RangeX.Max - RangeX.Min) / 2d + RangeX.Min,
            (RangeY.Max - RangeY.Min) / 2d + RangeY.Min,
            (RangeZ.Max - RangeZ.Min) / 2d + RangeZ.Min);

        public override string WpfMinString
        {
            get => $"{MinPoint}";
            set
            {
                if (RhinoStaticMethods.TryParsePoint3d(value, out Point3d val))
                {
                    RangeX = new DoubleRange(val.X, _rangeX.Max);
                    RangeY = new DoubleRange(val.Y, _rangeY.Max);
                    RangeZ = new DoubleRange(val.Z, _rangeZ.Max);
                }
                else throw new Exception($"Min value {value} is not valid for {GetType()}.");
            }
        }
        public override string WpfMaxString
        {
            get => $"{MaxPoint}";
            set
            {
                if (RhinoStaticMethods.TryParsePoint3d(value, out Point3d val))
                {
                    RangeX = new DoubleRange(_rangeX.Min, val.X);
                    RangeY = new DoubleRange(_rangeY.Min, val.Y);
                    RangeZ = new DoubleRange(_rangeZ.Min, val.Z);
                }
                else throw new Exception($"Max value {value} is not valid for {GetType()}.");
            }
        }

        public bool IsInside(Point3d inValue)
        {
            return RangeX.IsInside(inValue.X) && RangeY.IsInside(inValue.Y) && RangeZ.IsInside(inValue.Z);
        }

        public Point3d Scale(Point3d inValue, PointValueRange inToRange)
        {
            return new Point3d(
                inValue.X.Scale(RangeX, inToRange.RangeX),
                inValue.Y.Scale(RangeY, inToRange.RangeY),
                inValue.Z.Scale(RangeZ, inToRange.RangeZ)
                );
        }

        public double Scale_X(double inValue, DoubleRange inToRange)
        {
            return inValue.Scale(RangeX, inToRange);
        }
        public double Scale_Y(double inValue, DoubleRange inToRange)
        {
            return inValue.Scale(RangeY, inToRange);
        }
        public double Scale_Z(double inValue, DoubleRange inToRange)
        {
            return inValue.Scale(RangeZ, inToRange);
        }

        public double DistanceFrom(Point3d inValue)
        {
            // Defines the Box
            Box box = new Box(new BoundingBox(MinPoint, MaxPoint));
            
            // Gets the closest point to the box
            Point3d closest = box.ClosestPoint(inValue);

            return inValue.DistanceTo(closest);
        }

        public double DistanceFrom_X(double inValue)
        {
            if (RangeX.IsInside(inValue)) return 0d;
            return Math.Max(inValue - RangeX.Max, RangeX.Min - inValue);
        }
        public double DistanceFrom_Y(double inValue)
        {
            if (RangeY.IsInside(inValue)) return 0d;
            return Math.Max(inValue - RangeY.Max, RangeY.Min - inValue);
        }
        public double DistanceFrom_Z(double inValue)
        {
            if (RangeZ.IsInside(inValue)) return 0d;
            return Math.Max(inValue - RangeZ.Max, RangeZ.Min - inValue);
        }
    }
}
