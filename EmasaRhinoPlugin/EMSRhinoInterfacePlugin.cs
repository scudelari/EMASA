using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.PlugIns;
using System.Reflection;

namespace EmasaRhinoPlugin
{
    public class EMSRhinoInterfacePlugin : PlugIn
    {
        public EMSRhinoInterfacePlugin()
        {
            Instance = this;
        }

        public static EMSRhinoInterfacePlugin Instance
        {
            get;
            private set;
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            string app_name = Assembly.GetExecutingAssembly().GetName().Name;
            string app_version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            RhinoApp.WriteLine($"{app_name} {app_version} loaded.");
            return LoadReturnCode.Success;
        }

        public override PlugInLoadTime LoadTime
        {
            get
            {
                return PlugInLoadTime.AtStartup;
            }
        }

        public override object GetPlugInObject()
        {
            return new EMSRhinoCOMObject();
        }
    }
}
