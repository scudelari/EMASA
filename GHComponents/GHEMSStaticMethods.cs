using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace GHComponents
{
    public static class GHEMSStaticMethods
    {
        public static Color VarNameColor => Color.DarkSlateBlue;

        public static Color OriginalBackgroundColor => Color.LightGray;
        public static Color OriginalBorderColor => Color.Gray;

        public static Color InvalidBackgroundColor => Color.LightSalmon;
        public static Color InvalidBorderColor => Color.Red;

        public static double Tolerance = 1e-9;
    }
}
