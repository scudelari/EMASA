using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmasaSapTools.Monitors
{
    public class PauseMonitorEventArgs
    {
        /// <summary>
        /// The arguments of the Pause Monitor Event.
        /// </summary>
        /// <param name="inSender">Sending object, usually "this".</param>
        /// <param name="inMonitorName">If given, will filter to pause only the given monitor.</param>
        public PauseMonitorEventArgs(object inSender, string inMonitorName = null)
        {
            Sender = inSender;
            MonitorName = inMonitorName;
        }

        public object Sender { get; }
        public string MonitorName { get; }
    }
}
