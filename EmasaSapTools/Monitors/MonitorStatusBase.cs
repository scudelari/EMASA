using System;
using System.Threading;
using System.Threading.Tasks;
using BaseWPFLibrary.Events;
using Prism.Events;

namespace EmasaSapTools.Monitors
{
    public abstract class MonitorStatusBase
    {
        protected MonitorStatusBase(string inShortName, string inName = null)
        {
            ShortName = inShortName;

            if (!string.IsNullOrEmpty(inName)) Name = inName;
            else Name = ShortName;
        }

        public string ShortName { get; }
        public string Name { get; }

        public bool IsRunning => MonitorTask != null;
        public bool ShouldRestart { get; set; } = false;

        /// <summary>
        /// This is the action that will run in the BackGround that gets the monitor data and updates the interface
        /// </summary>
        public virtual Action MonitorAction =>
            throw new NotImplementedException($"[{GetType().Name}] did not implement the MonitorAction property!");

        private Task MonitorTask { get; set; }
        protected CancellationTokenSource StopToken { get; set; } = new CancellationTokenSource();

        private object _currentMonitorData = null;
        /// <summary>
        /// Contains the data that will be filled on each monitor refresh
        /// </summary>
        public object CurrentMonitorData
        {
            get => _currentMonitorData;
            protected set
            {
                _currentMonitorData = value;
                MonitorDataChanged?.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler MonitorDataChanged;

        public virtual void StartMonitor(bool inOnlyIfAutoStopped = false)
        {
            if (inOnlyIfAutoStopped)
                if (!ShouldRestart)
                    return;

            // Launches a new monitor job
            if (MonitorTask != null)
                throw new ThreadStateException(
                    $"[{GetType().Name}] Tried to launch a new monitor while the previous monitor was active.");

            MonitorTask = new Task(() => MonitorAction());
            MonitorTask.Start();
        }
        public virtual void StopMonitor(bool inAutoStopped = false)
        {
            ShouldRestart = IsRunning && inAutoStopped;

            if (IsRunning)
            {
                StopToken.Cancel();
                MonitorTask.Wait();
            }

            // The task is finished
            StopToken = new CancellationTokenSource();
            MonitorTask = null;
            CurrentMonitorData = null;
        }
    }
}