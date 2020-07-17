using System;

namespace Sap2000Library.DataClasses
{
    public class PointRestraintDef
    {
        public bool U1 { get; set; }
        public bool U2 { get; set; }
        public bool U3 { get; set; }
        public bool R1 { get; set; }
        public bool R2 { get; set; }
        public bool R3 { get; set; }

        public bool[] Values
        {
            get
            {
                return new bool[] { U1, U2, U3, R1, R2, R3 };
            }
        }

        public PointRestraintDef()
        {
            U1 = false;
            U2 = false;
            U3 = false;
            R1 = false;
            R2 = false;
            R3 = false;
        }

        public PointRestraintDef(bool u1, bool u2, bool u3, bool r1, bool r2, bool r3)
        {
            U1 = u1;
            U2 = u2;
            U3 = u3;
            R1 = r1;
            R2 = r2;
            R3 = r3;
        }
        public PointRestraintDef(bool[] values)
        {
            U1 = values[0];
            U2 = values[1];
            U3 = values[2];
            R1 = values[3];
            R2 = values[4];
            R3 = values[5];
        }
        public PointRestraintDef(PointRestraintType pointRestraintType)
        {
            switch (pointRestraintType)
            {
                case PointRestraintType.FullyFixed:
                    U1 = U2 = U3 = true;
                    R1 = R2 = R3 = true;
                    break;
                case PointRestraintType.SimplySupported:
                    U1 = U2 = U3 = true;
                    R1 = R2 = R3 = false;
                    break;
                case PointRestraintType.HasAtLeastOne:
                    throw new InvalidOperationException($"You cannot create a new point restraint definition using the {pointRestraintType} type.");
                case PointRestraintType.HasNone:
                    U1 = U2 = U3 = false;
                    R1 = R2 = R3 = false;
                    break;
                default:
                    break;
            }
        }

        public PointRestraintType RestraintType
        {
            get
            {
                if (U1 == false && U2 == false && U3 == false && R1 == false && R2 == false && R3 == false) return PointRestraintType.HasNone;

                if (U1 == true && U2 == true && U3 == true)
                {
                    if (R1 == true && R2 == true && R3 == true) return PointRestraintType.FullyFixed;
                    if (R1 == false && R2 == false && R3 == false) return PointRestraintType.SimplySupported;
                }
                return PointRestraintType.HasAtLeastOne;
            }
        }
    }

}