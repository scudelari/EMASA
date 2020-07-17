using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Items
{
    public class FeRestraint : IEquatable<FeRestraint>
    {
        public FeRestraint(bool[] inDoF)
        {
            if (inDoF == null || inDoF.Length != 6) throw new Exception("DoF must have 6 booleans <X,Y,Z,Rx,Ry,Rz>.");
            _doF = inDoF;
        }

        private bool[] _doF;
        public bool[] DoF
        {
            get => _doF;
            set
            {
                if (value == null || value.Length != 6) throw new Exception("DoF must have 6 booleans <X,Y,Z,Rx,Ry,Rz>.");
                _doF = value;
            }
        }

        public bool IsAll
        {
            get => DoF.All(a => a == true);
        }

        public bool Equals(FeRestraint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _doF.Equals(other._doF);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeRestraint) obj);
        }

        public override int GetHashCode()
        {
            return _doF.GetHashCode();
        }

        public static bool operator ==(FeRestraint left, FeRestraint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FeRestraint left, FeRestraint right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            if (IsAll) return "DOF: ALL";

            return $"DOF: <X:{_doF[0]}> <Y:{_doF[1]}> <Z:{_doF[2]}> <Rx:{_doF[3]}> <Ry:{_doF[4]}> <Rz:{_doF[5]}>";
        }
    }
}
