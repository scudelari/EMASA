using System;
using System.Drawing;
using GHComponents.Properties;
using Grasshopper.Kernel;

namespace GHComponents
{
    public class GHDataOutputInfo : GH_AssemblyInfo
    {
        public override string Name => "EMASA";

        public override Bitmap Icon =>
            //Return a 24x24 pixel bitmap to represent this GHA library.
            Resources.GH_Icon_Base;

        public override string Description =>
            //Return a short string describing the purpose of this GHA library.
            "Library of EMASA's custom functions.";

        public override Guid Id => new Guid("c0f8f6c5-056f-4cb3-a019-210449ec918d");

        public override string AuthorName =>
            //Return a string identifying you or your company.
            "EMASA Engineering VOF";

        public override string AuthorContact =>
            //Return a string representing your preferred contact details.
            "Rafael Scudelari de Macedo";
    }
}
