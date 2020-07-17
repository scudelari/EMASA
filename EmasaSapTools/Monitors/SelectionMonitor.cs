using Sap2000Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmasaSapTools.Bindings;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Monitors
{
    internal class SelectionMonitor : MonitorStatus
    {
        public SelectionMonitor(MainWindow owner) : base(owner, "S", null)
        {
        }

        public override Task StartMonitor()
        {
            // Creates a new cancel request listeners
            MonitorCancelToken = new CancellationTokenSource();

            // To report progress - Constructor gets the current thread so will run in UI
            var progress = new Progress<List<SapObject>>(selObjects =>
            {
                SelectionInfoBindings.I.SelectedObjects = selObjects;
            });

            // The async body
            Action<IProgress<List<SapObject>>, CancellationToken> work = (prog, token) =>
            {
                do
                {
                    if (token.IsCancellationRequested) return;

                    // Gets the selected points
                    var selObjects = S2KModel.SM.GetSelected();

                    prog.Report(selObjects);

                    Thread.Sleep(Properties.Settings.Default.MonitorSleep);
                } while (true);
            };

            // Runs the job async
            MonitorTask = new Task(() => work(progress, MonitorCancelToken.Token), MonitorCancelToken.Token);
            MonitorTask.Start();
            //Owner.UpdateMonitorStatusBarText();

            return MonitorTask;
        }

        public override void StopMonitor()
        {
            base.StopMonitor();
            SelectionInfoBindings.I.SelMonitor_GroupsDataGridItems = null;
        }
    }
}