using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmasaSapTools.Monitors
{
    public abstract class MonitorStatus
    {
        public MonitorStatus(MainWindow inOwner, string inName, string inDisplayName = null)
        {
            Name = inName;

            if (!string.IsNullOrEmpty(inDisplayName)) DisplayName = inDisplayName;
            else DisplayName = Name;

            Owner = inOwner;
        }

        public string Name { get; private set; }
        public string DisplayName { get; private set; }

        public MainWindow Owner { get; set; }

        public bool IsRunning => MonitorTask != null;
        public bool AutomaticCancel { get; set; }

        public Task MonitorTask { get; protected set; }
        protected CancellationTokenSource MonitorCancelToken { get; set; }

        public virtual Task StartMonitor()
        {
            return Task.FromResult(default(object));
        }

        public virtual void StopMonitor()
        {
            if (IsRunning)
            {
                MonitorCancelToken.Cancel();
                MonitorTask.Wait();
            }

            // The task is finished
            MonitorCancelToken = null;
            MonitorTask = null;

            //Owner.UpdateMonitorStatusBarText();
        }
    }
}