extern alias r3dm; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA.Loads
{
    public class FeLoad_Inertial : FeLoad
    {
        public FeLoad_Inertial(double inValue, double inMultiplier) : base(inMultiplier)
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

        public static FeLoad_Inertial GetStandardGravity(double inMultiplier)
        {
            FeLoad_Inertial g = new FeLoad_Inertial(9.80665, inMultiplier);
            return g;
        }

        #region Specific to Ansys
        public string AnsysInertialLoadLine
        {
            get
            {
                Vector3d acc = Direction * (Value * Multiplier);
                return $"ACEL,{acc.X},{acc.Y},{acc.Z}";
            }
        }
        #endregion

        public override string ToString()
        {
            Vector3d acc = Direction * (Value * Multiplier);
            return $"Inertial Load: {acc}";
        }
    }
}
