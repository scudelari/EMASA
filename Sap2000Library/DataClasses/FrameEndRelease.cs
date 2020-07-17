using System;

namespace Sap2000Library.DataClasses
{
    public class FrameEndRelease
    {
        public bool iU1 = false;
        public double iU1ParFix = 0;
        public bool iU2 = false;
        public double iU2ParFix = 0;
        public bool iU3 = false;
        public double iU3ParFix = 0;
        public bool iR1 = false;
        public double iR1ParFix = 0;
        public bool iR2 = false;
        public double iR2ParFix = 0;
        public bool iR3 = false;
        public double iR3ParFix = 0;

        public bool jU1 = false;
        public double jU1ParFix = 0;
        public bool jU2 = false;
        public double jU2ParFix = 0;
        public bool jU3 = false;
        public double jU3ParFix = 0;
        public bool jR1 = false;
        public double jR1ParFix = 0;
        public bool jR2 = false;
        public double jR2ParFix = 0;
        public bool jR3 = false;
        public double jR3ParFix = 0;

        public FrameEndRelease(bool[] ii, bool[] jj, double[] iParFix, double[] jParFix)
        {
            iU1 = ii[0];
            iU2 = ii[1];
            iU3 = ii[2];
            iR1 = ii[3];
            iR2 = ii[4];
            iR3 = ii[5];
            iU1ParFix = iParFix[0];
            iU2ParFix = iParFix[1];
            iU3ParFix = iParFix[2];
            iR1ParFix = iParFix[3];
            iR2ParFix = iParFix[4];
            iR3ParFix = iParFix[5];

            jU1 = jj[0];
            jU2 = jj[1];
            jU3 = jj[2];
            jR1 = jj[3];
            jR2 = jj[4];
            jR3 = jj[5];
            jU1ParFix = jParFix[0];
            jU2ParFix = jParFix[1];
            jU3ParFix = jParFix[2];
            jR1ParFix = jParFix[3];
            jR2ParFix = jParFix[4];
            jR3ParFix = jParFix[5];
        }
        public FrameEndRelease(FrameEndReleaseDef_Types releaseType)
        {
            switch (releaseType)
            {
                case FrameEndReleaseDef_Types.NoReleaseBoth:
                        // Nothing, it is the default
                    break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseBoth:
                        iR2 = true;
                        iR3 = true;
                        jR2 = true;
                        jR3 = true;
                    break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionI:
                        iR2 = true;
                        iR3 = true;
                        jR2 = true;
                        jR3 = true;
                        iR1 = true;
                        break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionJ:
                        iR2 = true;
                        iR3 = true;
                        jR2 = true;
                        jR3 = true;
                        jR1 = true;
                        break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseI:
                        iR2 = true;
                        iR3 = true;
                        break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseJ:
                        jR2 = true;
                        jR3 = true;
                        break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseWithTorsionI:
                        iR1 = true;
                        iR2 = true;
                        iR3 = true;
                        break;
                case FrameEndReleaseDef_Types.FULL_MomentReleaseWithTorsionJ:
                        jR1 = true;
                        jR2 = true;
                        jR3 = true;
                        break;
                case FrameEndReleaseDef_Types.Other:
                default:
                    throw new InvalidOperationException("Use the other constructor");
            }
        }

        public FrameEndReleaseDef_Types Type
        {
            get
            {
                if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == false && iR2 == false && iR3 == false
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == false && jR2 == false && jR3 == false)
                    return FrameEndReleaseDef_Types.NoReleaseBoth;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == true && iR2 == true && iR3 == true
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == false && jR2 == true && jR3 == true)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionI;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == false && iR2 == true && iR3 == true
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == true && jR2 == true && jR3 == true)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseBoth_TorsionJ;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == false && iR2 == true && iR3 == true
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == false && jR2 == false && jR3 == false)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseI;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == false && iR2 == false && iR3 == false
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == false && jR2 == true && jR3 == true)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseJ;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == true && iR2 == true && iR3 == true
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == false && jR2 == false && jR3 == false)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseWithTorsionI;

                else if (iU1 == false && iU2 == false && iU3 == false
                    && iR1 == false && iR2 == false && iR3 == false
                    && jU1 == false && jU2 == false && jU3 == false
                    && jR1 == true && jR2 == true && jR3 == true)
                    return FrameEndReleaseDef_Types.FULL_MomentReleaseWithTorsionJ;

                else return FrameEndReleaseDef_Types.Other;
            }
        }

        public bool HasIDualMomentRelease
        {
            get
            {
                if (iR2 == true && iR3 == true) return true;
                else return false;
            }
        }
        public bool HasJDualMomentRelease
        {
            get
            {
                if (jR2 == true && jR3 == true) return true;
                else return false;
            }
        }

    }
    public enum FrameEndReleaseDef_Types
    {
        NoReleaseBoth,
        FULL_MomentReleaseBoth,
        FULL_MomentReleaseBoth_TorsionI,
        FULL_MomentReleaseBoth_TorsionJ,
        FULL_MomentReleaseI,
        FULL_MomentReleaseJ,
        FULL_MomentReleaseWithTorsionI,
        FULL_MomentReleaseWithTorsionJ,
        Other
    }
}
