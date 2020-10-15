using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA
{
    public abstract class FeSolver : BindableBase, IDisposable
    {
        [NotNull] protected readonly SolveManager _owner;
        protected FeSolver(string inFeWorkSolverFolder, [NotNull] SolveManager inOwner)
        {
            _owner = inOwner ?? throw new ArgumentNullException(nameof(inOwner));

            FeWorkFolder = inFeWorkSolverFolder;

            Stopwatch sw = Stopwatch.StartNew();

            // Starts the software
            InitializeSoftware();

            sw.Stop();
            InitializingSoftwareTimeSpan = sw.Elapsed;
        }

        private string _feWorkFolder;
        public string FeWorkFolder
        {
            get => _feWorkFolder;
            set => SetProperty(ref _feWorkFolder, value);
        }

        public abstract void CleanUpDirectory();
        
        public abstract void InitializeSoftware();
        public abstract void ResetSoftwareData();
        public abstract void CloseApplication();

        //public abstract void RunAnalysisAndGetResults(List<ResultOutput> inDesiredResults, int inEigenvalueBucklingMode = 0, double inEigenvalueBucklingScaleFactor = double.NaN);
        public abstract void RunAnalysisAndCollectResults(FeModel inModel);

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
        ~FeSolver()
        {
            Dispose(false);
        }
        #endregion

        #region TimeSpans
        private TimeSpan _initializingSoftwareTimeSpan;
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
    }
}
