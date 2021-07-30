using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Extensions;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA
{
    public abstract class FeSolverBase : BindableBase, IDisposable
    {
        protected FeSolverBase()
        {
        }

        public string FeWorkFolder => Path.Combine(AppSS.I.Gh_Alg.GhDataDirPath, "NlOpt", "FeWork");

        public abstract void CleanUpDirectory();
        protected abstract void InitializeSoftware();
        public abstract void ResetSoftwareData();
        protected abstract void CloseApplication();

        public abstract void RunAnalysisAndCollectResults(FeModel inModel);
        public abstract void GeneratePointModel(FeModel inModel, string inSaveFolder, string inFileName);

        #region Disposable Block
        private void ReleaseUnmanagedResources()
        {
            Stopwatch sw = Stopwatch.StartNew();

            CloseApplication();

            sw.Stop();
            FinalizingSoftwareTimeSpan = sw.Elapsed;
        }
        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~FeSolverBase()
        {
            Dispose(false);
        }
        #endregion

        #region TimeSpans
        private TimeSpan _initializingSoftwareTimeSpan = TimeSpan.Zero;
        public TimeSpan InitializingSoftwareTimeSpan
        {
            get => _initializingSoftwareTimeSpan;
            set => SetProperty(ref _initializingSoftwareTimeSpan, value);
        }

        private TimeSpan _finalizingSoftwareTimeSpan;
        public TimeSpan FinalizingSoftwareTimeSpan
        {
            get => _finalizingSoftwareTimeSpan;
            set => SetProperty(ref _finalizingSoftwareTimeSpan, value);
        }
        #endregion

        public abstract List<Process> GetAllRunningProcesses();
        public void KillAllRunningProcesses(List<Process> inToKill = null)
        {
            List<Process> allProcs = inToKill ?? GetAllRunningProcesses();

            foreach (Process process in allProcs)
            {
                process.KillProcessAndChildren();
            }
        }
    }


    public class FeSolverException : Exception
    {
        public FeSolverException()
        {
        }

        public FeSolverException(string message) : base(message)
        {
        }

        public FeSolverException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FeSolverException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public bool SolutionNotConverged { get; set; } = false;
    }
}
