using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Sap2000Library
{
    internal class RunningObject
    {
        public string name;
        public object o;

        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        // Returns the contents of the Running Object Table (ROT), where
        // open Microsoft applications and their documents are registered.
        internal static List<RunningObject> GetRunningObjects()
        {
            // Get the table.
            List<RunningObject> res = new List<RunningObject>();
            CreateBindCtx(0, out IBindCtx bc);
            bc.GetRunningObjectTable(out IRunningObjectTable runningObjectTable);
            runningObjectTable.EnumRunning(out IEnumMoniker monikerEnumerator);
            monikerEnumerator.Reset();

            // Enumerate and fill our nice dictionary.
            IMoniker[] monikers = new IMoniker[1];
            IntPtr numFetched = IntPtr.Zero;
            List<string> names = new List<string>();
            //List books = new List();
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                RunningObject running = new RunningObject();
                monikers[0].GetDisplayName(bc, null, out running.name);
                runningObjectTable.GetObject(monikers[0], out running.o);
                res.Add(running);
            }
            return res;
        }
    }
}
