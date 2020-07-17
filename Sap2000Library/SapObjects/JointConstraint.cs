using System.Collections.Generic;

namespace Sap2000Library.SapObjects
{
    public class JointConstraintBase
    {
        

        public string Name 
        { 
            get; 
            private set; 
        }
        public ConstraintTypeEnum ConstraintType 
        { 
            get;
            private set; 
        }
        public ConstraintAxisEnum ConstraintAxis { get; private set; }

        private bool[] _dof = new bool[6] { false, false, false, false, false, false };
        public bool[] DOFs
        {
            get => _dof;
            private set => _dof = value;
        }
        public string cSys 
        { 
            get;
            private set; 
        }
        public double Tolerance 
        { 
            get;
            private set; 
        }

        public override bool Equals(object obj)
        {
            return obj is JointConstraintBase @base &&
                   Name == @base.Name &&
                   ConstraintType == @base.ConstraintType;
        }

        public override int GetHashCode()
        {
            int hashCode = 507257616;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + ConstraintType.GetHashCode();
            return hashCode;
        }
    }
}
