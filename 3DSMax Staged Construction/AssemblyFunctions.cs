using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;

namespace _3DSMax_Staged_Construction
{
    public static class AssemblyFunctions
    {
        public static void AssemblyMain()
	    {
            CosturaUtility.Initialize();

            MaxHelper.MaxGlobal = GlobalInterface.Instance;
            MaxHelper.MaxInterface = MaxHelper.MaxGlobal.COREInterface;
            MaxHelper.MaxInterface.AddClass(new PluginDescriptor(MaxHelper.MaxGlobal, MaxHelper.MaxInterface));
        }
    }
}
