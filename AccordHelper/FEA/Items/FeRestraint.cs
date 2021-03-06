﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace AccordHelper.FEA.Items
{
    public class FeRestraint : BindableBase, IEquatable<FeRestraint>
    {
        public FeRestraint(bool[] inDoF = null)
        {
            if (inDoF == null) inDoF = new []{false, false, false, false, false, false, };
            if (inDoF.Length != 6) throw new Exception("DoF must have 6 booleans <X,Y,Z,Rx,Ry,Rz>.");
            _doF = inDoF;
        }

        private bool[] _doF;
        public bool[] DoF
        {
            get => _doF;
            set
            {
                if (value == null) _doF = new[] { false, false, false, false, false, false, };
                if (value.Length != 6) throw new Exception("DoF must have 6 booleans <X,Y,Z,Rx,Ry,Rz>.");
                _doF = value;

                RaisePropertyChanged("IsAll");
                RaisePropertyChanged("U1");
                RaisePropertyChanged("U2");
                RaisePropertyChanged("U3");
                RaisePropertyChanged("R1");
                RaisePropertyChanged("R2");
                RaisePropertyChanged("R3");
            }
        }

        public bool U1
        {
            get => _doF[0];
            set
            {
                SetProperty(ref _doF[0], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
        public bool U2
        {
            get => _doF[1];
            set
            {
                SetProperty(ref _doF[1], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
        public bool U3
        {
            get => _doF[2];
            set
            {
                SetProperty(ref _doF[2], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
        public bool R1
        {
            get => _doF[3];
            set
            {
                SetProperty(ref _doF[3], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
        public bool R2
        {
            get => _doF[4];
            set
            {
                SetProperty(ref _doF[4], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
        public bool R3
        {
            get => _doF[5];
            set
            {
                SetProperty(ref _doF[5], value);

                RaisePropertyChanged("DoF");
                RaisePropertyChanged("IsAll");
            }
        }
         
        public bool IsAll => DoF.All(a => a == true);
        public bool IsPinned => U1 && U2 && U3;
        public bool ExistAny => DoF.All(a => a == false);

        public void IncorporateRestraint(FeRestraint inOtherRestraint)
        {
            if (!U1 && inOtherRestraint.U1) U1 = true;
            if (!U2 && inOtherRestraint.U2) U2 = true;
            if (!U3 && inOtherRestraint.U1) U3 = true;
            if (!R1 && inOtherRestraint.U1) R1 = true;
            if (!R2 && inOtherRestraint.U1) R2 = true;
            if (!R3 && inOtherRestraint.U1) R3 = true;
        }

        public bool Equals(FeRestraint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _doF.SequenceEqual(other._doF);
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
            unchecked
            {
                int hash = 17;
                foreach (bool t in _doF) hash = hash * 23 + t.GetHashCode();
                return hash;
            }
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

        public string SimpleName => $"{U1}_{U2}_{U3}_{R1}_{R2}_{R3}";

        public static FeRestraint FixedRestraint => new FeRestraint(new []{true, true, true, true, true, true, });
        public static FeRestraint PinnedRestraint => new FeRestraint(new[] { true, true, true, false, false, false, });

        public static FeRestraint XOnlyRestraint => new FeRestraint(new[] { true, false, false, false, false, false, });
        public static FeRestraint YOnlyRestraint => new FeRestraint(new[] { false, true, false, false, false, false, });
        public static FeRestraint ZOnlyRestraint => new FeRestraint(new[] { false, false, true, false, false, false, });

    }
}
