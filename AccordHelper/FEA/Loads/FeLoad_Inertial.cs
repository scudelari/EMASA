extern alias r3dm; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using r3dm::Rhino.Geometry;

namespace AccordHelper.FEA.Loads
{
    public class FeLoad_Inertial : FeLoadBase
    {
        public FeLoad_Inertial(double inValue) : base()
        {
            Value = inValue;
        }

        public double Value { get; set; }

        private Vector3d _direction = Vector3d.ZAxis;
        public Vector3d Direction
        {
            get => _direction;
            set
            {
                value.Unitize();
                _direction = value;
            }

        }

        public override void LoadModel(FeModelBase inModel, double inFactor = 1d)
        {
            switch (inModel)
            {
                case AnsysModel ansysModel:
                    Vector3d acc = Direction * (Value * inFactor);
                    ansysModel.sb.AppendLine($"ACEL,{acc.X},{acc.Y},{acc.Z} ! Sets the Inertial Load");
                    break;

                case S2KModel s2KModel:
                default:
                    throw new ArgumentOutOfRangeException(nameof(inModel));
            }
        }

        public static FeLoad_Inertial StandardGravity => new FeLoad_Inertial(9.80665);
    }
}
