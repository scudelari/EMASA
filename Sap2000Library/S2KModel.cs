using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SAP2000v1;
using Sap2000Library;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.IO;
using Castle.Components.DictionaryAdapter.Xml;
using BaseWPFLibrary;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Sap2000Library.Managers;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;

namespace Sap2000Library
{
    /// <summary>
    /// Encapsulates all SAP2000 Interface. It is a singleton.
    /// </summary>
    public sealed class S2KModel
    {
        #region Singleton Management
        private static S2KModel _mainInstance;
        private static List<S2KModel> _otherInstances = new List<S2KModel>();
        private static readonly object _lockThis = new object();

        /// <summary>
        /// Gets the <b>main</b> sap model helper.
        /// If this is <b>globally</b> the first time, it link the sap model to the running SAP2000 instance.
        /// You can force the singleton to bind to start a new instance by first calling InitSingleton_NewInstance.
        /// See Start functions to start a new sap 2000 process.
        /// </summary>
        /// <exception cref="System.EntryPointNotFoundException">Thrown the <b>main</b> sap model helper hasn't been initialized or if it could not get a running instance of SAP2000.</exception>
        public static S2KModel SM
        {
            get
            {
                lock (_lockThis)
                {
                    // If singleton hasn't been initialized
                    if (_mainInstance == null) _mainInstance = GetRunningInstance();
                }

                return _mainInstance;
            }
        }
        /// <summary>
        /// Will start SAP2000 if it hasn't been already started.
        /// </summary>
        /// <param name="inUnits">The model units</param>
        public static void InitSingleton_NewInstance(UnitsEnum inUnits, bool isVisible = true)
        {
            lock (_lockThis)
            {
                if (_mainInstance == null) _mainInstance = StartNewInstance(inUnits, isVisible);
            }
        }
        /// <summary>
        /// Will either start a new SAP2000 or get the first running instance.
        /// </summary>
        /// <param name="inUnits">The model units</param>
        public static void InitSingleton_RunningOrNew(UnitsEnum inUnits, bool isVisible = true)
        {
            lock (_lockThis)
            {
                if (_mainInstance == null)
                {
                    try
                    {
                        _mainInstance = GetRunningInstance();
                        S2KModel.SM.PresentationUnits = inUnits;
                        S2KModel.SM.WindowVisible = isVisible;
                    }
                    catch
                    {
                        _mainInstance = StartNewInstance(inUnits, isVisible);
                    }
                }
            }
        }
        #endregion

        private S2KModel(cOAPI inOapi) : this()
        {
            SapInstance = inOapi;
        }
        private static S2KModel GetRunningInstance()
        {
            try
            {
                //get the active SapObject
                return new S2KModel((cOAPI)Marshal.GetActiveObject("CSI.SAP2000.API.SapObject"));
            }
            catch (Exception ex)
            {
                throw new EntryPointNotFoundException("Could not get a running instance of SAP2000.", ex);
            }
        }
        private static S2KModel StartNewInstance(UnitsEnum inUnits, bool isVisible = true)
        {
            cHelper myHelper;
            // Create API helper object
            try
            {
                myHelper = new Helper();
            }
            catch (Exception ex)
            {
                throw new EntryPointNotFoundException("Could not open the Sap2000 API helper file.", ex);
            }

            // Create an instance of the SapObject from the latest installed SAP2000
            cOAPI mySapObject;
            try
            {
                // Create SapObject
                mySapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
            }
            catch (Exception ex)
            {
                throw new EntryPointNotFoundException("Could not find the program CSI.SAP2000.API.SapObject in the COM list.", ex);
            }

            try
            {
                // Start SAP2000 application
                if (mySapObject.ApplicationStart((eUnits)(int)inUnits, isVisible) != 0) throw new EntryPointNotFoundException($"Could not start SAP2000 Application");

                return new S2KModel(mySapObject);
            }
            catch (Exception ex)
            {
                throw new EntryPointNotFoundException($"Could not start the SAP2000 Application.", ex);
            }
        }

        /// <summary>
        /// Gets one of the <b>secondary</b> sap model helpers. This is used when handling two models at the same time.
        /// Make sure to initialize the secondary sap model helpers using one of the Start functions.
        /// </summary>
        /// <param name="position">The position of the requested secondary model. If -1 is given, it will return the main sap model helper instance. This is to facilitate writting codes that may or may not have the main SAP2000 handler instantiated.</param>
        /// <exception cref="S2KHelperException">Thrown when the requested position is not available.</exception>
        /// <returns>A reference to the secondary sap model helper.</returns>
        public static S2KModel GetSMOther(int position)
        {
            lock (_lockThis)
            {
                if (position == -1) return SM;
                try
                {
                    return _otherInstances[position];
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException($"Could not get sap model helper instance at the requested position.{Environment.NewLine}You must initialize the secondary models using one of the Start functions.{Environment.NewLine}To access the main sap model helper, use the SM property.", ex);
                }
            }
        }
        public static int InitSMOther_NewInstance(UnitsEnum inUnits, bool isVisible = true)
        {
            S2KModel newInstance = StartNewInstance(inUnits, isVisible);
            _otherInstances.Add(newInstance);
            return _otherInstances.Count - 1;
        }


        private cOAPI _sapInstance;
        private cOAPI SapInstance
        {
            get => _sapInstance;
            set
            {
                _sapInstance = value;
                if (_sapInstance != null) _isCurrentlyVisible = _sapInstance.Visible();
            }
        }
        internal cSapModel SapApi
        {
            get
            {
                try
                {
                    return SapInstance.SapModel;
                }
                catch (Exception ex)
                {
                    ExceptionViewer.Show(new Exception(
                        $"Could not get an instance to the Sap2000 model.{Environment.NewLine}Probably Sap2000 was closed.{Environment.NewLine}This application will also close.",
                        ex));
                    Application.Current.Shutdown(1);
                    return null;
                }
            }
        }
        private eUnits PresentUnits
        {
            get => SapApi.GetPresentUnits();
            set => SapApi.SetPresentUnits(value);
        }


        /// <summary>
        /// Basic constructor that initializes the managers
        /// </summary>
        private S2KModel()
        {
            InterAuto = new SapInterfaceAutomator(this);
            FrameMan = new FrameManager(this);
            MaterialMan = new MaterialManager(this);
            FrameSecMan = new FrameSectionManager(this);
            PointMan = new PointManager(this);
            JointConstraintMan = new JointConstraintManager(this);
            AreaMan = new AreaManager(this);
            CableMan = new CableManager(this);
            LinkMan = new LinkManager(this);
            LCMan = new LCManager(this);
            GroupMan = new GroupManager(this);
            CombMan = new CombManager(this);
            GridMan = new GridManager(this);
            CableSecMan = new CableSectionManager(this);
            LPMan = new LPManager(this);
            AnalysisMan = new AnalysisManager(this);
            ResultMan = new ResultManager(this);
            SteelDesignMan = new SteelDesignManager(this);
        }
        public SapInterfaceAutomator InterAuto { get; }
        public FrameManager FrameMan { get; }
        public MaterialManager MaterialMan { get; }
        public FrameSectionManager FrameSecMan { get; }
        public PointManager PointMan { get; }
        public JointConstraintManager JointConstraintMan { get; }
        public AreaManager AreaMan { get; }
        public CableManager CableMan { get; }
        public LinkManager LinkMan { get; }
        public LCManager LCMan { get; }
        public GroupManager GroupMan { get; }
        public CombManager CombMan { get; }
        public GridManager GridMan { get; }
        public CableSectionManager CableSecMan { get; }
        public LPManager LPMan { get; }
        public AnalysisManager AnalysisMan { get; }
        public ResultManager ResultMan { get; }
        public SteelDesignManager SteelDesignMan { get; }

        

        private bool _isCurrentlyVisible = true;
        private object visibleLocker = new object();

        public async void ShowSapAsync()
        {
            void work()
            {
                lock (visibleLocker)
                {
                    WindowVisible = true;
                }
            }

            // Runs the job async
            Task task = new Task(() => work());
            task.Start();
            await task;
        }
        public async void HideSapAsync()
        {
            void work()
            {
                lock (visibleLocker)
                {
                    WindowVisible = false;
                }
            }

            // Runs the job async
            Task task = new Task(() => work());
            task.Start();
            await task;
        }

        public bool WindowVisible
        {
            get => SapInstance.Visible();
            set
            {
                if (value) // Shall make it visible
                {
                    if (!_isCurrentlyVisible) // It is currently hidden
                    {
                        if (!SapInstance.Visible())
                            if (SapInstance.Unhide() != 0) throw new S2KHelperException("It was requested that SAP2000 be shown, but the request failed.");
                        _isCurrentlyVisible = true;
                    }
                }

                else // Shall hide
                {
                    if (_isCurrentlyVisible) // It is currently shown
                    {
                        if (SapInstance.Visible())
                            if (SapInstance.Hide() != 0) throw new S2KHelperException("It was requested that SAP2000 be hidden, but the request failed.");
                        _isCurrentlyVisible = false;
                    }
                }
            }
        }

        public bool ActivateSap()
        {
            if (WindowVisible == false)
            {
                throw new S2KHelperException("Cannot activate the SAP2000 window as it is not visible.");
            }

            // Gets the process of the model
            return 0 != PInvokeWrappers.SetForegroundWindow(InterAuto.SapProcess.MainWindowHandle);
        }

        public bool ActivateCurrentApplication()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                return 0 != PInvokeWrappers.SetForegroundWindow(currentProcess.MainWindowHandle);
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("Could not focus on the current application.", ex);
            }
        }

        public bool RefreshView()
        {
            return 0 == SapApi.View.RefreshView();
        }

        public bool Locked
        {
            get => SapApi.GetModelIsLocked();
            set => SapApi.SetModelIsLocked(value);
        }

        public string FullFileName
        {
            get { return SapApi.GetModelFilename(true) ?? ""; }
        }

        public string ModelDir
        {
            get { return Path.GetDirectoryName(FullFileName); }
        }

        public string FileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(FullFileName); }
        }

        public bool SaveFile(string inFileName = null)
        {
            if (string.IsNullOrWhiteSpace(inFileName))
            {
                return SapApi.File.Save() == 0;
            }
            else
            {
                return SapApi.File.Save(inFileName) == 0;
            }
        }

        public bool OpenFile(string inFileName)
        {
            if (Path.GetExtension(inFileName) != ".sdb")
            {
                return false;
            }

            return 0 == SapApi.File.OpenFile(inFileName);
        }

        public bool CloseApplication(bool SaveAsExit)
        {
            bool ret = SapInstance.ApplicationExit(SaveAsExit) == 0;

            if (ret)
            {
                SapInstance = null;
            }
            else
            {
                throw new InvalidOperationException("Could not close SAP2000!");
            }

            return ret;
        }

        public void NewModelBlank(bool savePrevious = false, UnitsEnum? inModelUnits = null)
        {
            if (savePrevious)
                if (!SaveFile())
                    throw new S2KHelperException("Could not save model.");

            UnitsEnum modelUnits;
            if (inModelUnits.HasValue) modelUnits = inModelUnits.Value;
            else // No definition given
            {
                try // to get the current units
                {
                    modelUnits = PresentationUnits;
                }
                catch (Exception e)
                {
                    modelUnits = UnitsEnum.N_mm_C; // Fallback to a default
                }
            }

            if (0 != SapApi.InitializeNewModel((eUnits)(int)modelUnits)) throw new S2KHelperException("Could not initialize a new model.");

            if (0 != SapApi.File.NewBlank()) throw new S2KHelperException("Could not create a new blank model.");
        }

        Dictionary<string, List<SapObject>> selectionCaches = new Dictionary<string, List<SapObject>>();
        public bool SaveSelectionCache(string inCacheName = null)
        {
            if (!string.IsNullOrWhiteSpace(inCacheName))
            {
                // Deletes if the cache already exists
                if (selectionCaches.ContainsKey(inCacheName)) selectionCaches.Remove(inCacheName);

                int numberItems = 0;
                int[] types = null;
                string[] names = null;

                int ret = SapApi.SelectObj.GetSelected(ref numberItems, ref types, ref names);
                if (ret != 0) throw new S2KHelperException("Could not get the current selection.");

                List<SapObject> selCache = new List<SapObject>();

                for (int i = 0; i < numberItems; i++)
                {
                    switch ((SapObjectType) types[i])
                    {
                        case SapObjectType.Point:
                            selCache.Add(new SapObject(names[i], SapObjectType.Point, PointMan));
                            break;
                        case SapObjectType.Frame:
                            selCache.Add(new SapObject(names[i], SapObjectType.Frame, FrameMan));
                            break;
                        case SapObjectType.Link:
                            selCache.Add(new SapObject(names[i], SapObjectType.Link, LinkMan));
                            break;
                        case SapObjectType.Cable:
                            selCache.Add(new SapObject(names[i], SapObjectType.Cable, CableMan));
                            break;
                        case SapObjectType.Area:
                            selCache.Add(new SapObject(names[i], SapObjectType.Area, AreaMan));
                            break;
                        case SapObjectType.Solid:
                            throw new S2KHelperException(
                                $"Type {((SapObjectType) types[0]).ToString()} is still not supported in this method. Please write the code.");
                        case SapObjectType.Tendon:
                            throw new S2KHelperException(
                                $"Type {((SapObjectType) types[0]).ToString()} is still not supported in this method. Please write the code.");
                    }
                }

                // saves the cache
                selectionCaches.Add(inCacheName, selCache);
            }

            return true;
        }

        public bool ReselectFromCache(string inCacheName, IProgress<ProgressData> ReportProgress = null)
        {
            if (string.IsNullOrWhiteSpace(inCacheName))
                throw new S2KHelperException("Please give the name of the selection cache.");

            if (!selectionCaches.ContainsKey(inCacheName))
                throw new S2KHelperException("Could not find the requested selection cache.");

            List<SapObject> cache = selectionCaches[inCacheName];

            return Select(cache);
        }

        /// <summary>
        /// Selects or Deselects ALL elements in the model.
        /// </summary>
        /// <param name="inSelect">True to select, False to deselect.</param>
        /// <returns></returns>
        public bool SelectAll(bool inSelect)
        {
            return 0 == SapApi.SelectObj.All(!inSelect);
        }

        public List<SapObject> GetSelected(IProgress<ProgressData> ReportProgress = null)
        {
            int numberItems = 0;
            int[] types = null;
            string[] names = null;

            int ret = SapApi.SelectObj.GetSelected(ref numberItems, ref types, ref names);
            if (ret != 0) return null;

            List<SapObject> toRet = new List<SapObject>();

            for (int i = 0; i < numberItems; i++)
            {
                switch ((SapObjectType) types[i])
                {
                    case SapObjectType.Point:
                        toRet.Add(PointMan.GetByName(names[i]));
                        break;
                    case SapObjectType.Frame:
                        toRet.Add(FrameMan.GetByName(names[i]));
                        break;
                    case SapObjectType.Link:
                        toRet.Add(LinkMan.GetByName(names[i]));
                        break;
                    case SapObjectType.Cable:
                        toRet.Add(CableMan.GetByName(names[i]));
                        break;
                    case SapObjectType.Area:
                        toRet.Add(AreaMan.GetByName(names[i]));
                        break;
                    case SapObjectType.Solid:
                        throw new S2KHelperException(
                            $"Type {((SapObjectType) types[0]).ToString()} is still not supported in this method. Please write the code.");
                    case SapObjectType.Tendon:
                        throw new S2KHelperException(
                            $"Type {((SapObjectType) types[0]).ToString()} is still not supported in this method. Please write the code.");
                }

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i, numberItems));
            }

            return toRet;
        }

        public bool Select(List<SapObject> toSelect, IProgress<ProgressData> ReportProgress = null)
        {
            bool success = true;
            for (int i = 0; i < toSelect.Count; i++)
            {
                SapObject item = toSelect[i];

                if (!item.Select()) success = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i, toSelect.Count));
            }

            return success;
        }

        public bool Select(string elementName, SapObjectType objType)
        {
            switch (objType)
            {
                case SapObjectType.Point:
                    return 0 == SapApi.PointObj.SetSelected(elementName, true);
                case SapObjectType.Frame:
                    return 0 == SapApi.FrameObj.SetSelected(elementName, true);
                case SapObjectType.Link:
                    return 0 == SapApi.LinkObj.SetSelected(elementName, true);
                case SapObjectType.Cable:
                    return 0 == SapApi.CableObj.SetSelected(elementName, true);
                case SapObjectType.Area:
                    return 0 == SapApi.AreaObj.SetSelected(elementName, true);
                case SapObjectType.Solid:
                    return 0 == SapApi.SolidObj.SetSelected(elementName, true);
                case SapObjectType.Tendon:
                    return 0 == SapApi.TendonObj.SetSelected(elementName, true);
                default:
                    return false;
            }
        }

        public bool ClearSelection()
        {
            return SapApi.SelectObj.ClearSelection() == 0;
        }

        public bool InvertSelection()
        {
            return SapApi.SelectObj.InvertSelection() == 0;
        }

        public void DeleteSelected()
        {
            InterAuto.Action_SendDeleteKey();
        }

        public List<string> GetAllJointPatterns()
        {
            int count = 0;
            string[] names = null;

            int ret = SapApi.PatternDef.GetNameList(ref count, ref names);
            if (ret != 0) return null;

            List<string> toRet = new List<string>();

            foreach (var item in names)
            {
                toRet.Add(item);
            }

            return toRet;
        }

        private double? _mergeTol;
        public double MergeTolerance
        {
            get
            {
                if (_mergeTol.HasValue) return _mergeTol.Value;

                double mergeTol = 0;
                int ret = SapApi.GetMergeTol(ref mergeTol);
                if (ret != 0) throw new S2KHelperException("Could not get the model's merge tolerance.");

                _mergeTol = mergeTol;

                return _mergeTol.Value;
            }
            set
            {
                if (0 != SapApi.SetMergeTol(value)) throw new S2KHelperException("Could not set the Merge Tolerance.");

                _mergeTol = value;
            }
        }

        private double? _versionNumber = null;
        private string _versionText = null;
        public double VersionNumber
        {
            get
            {
                if (_versionNumber.HasValue) return _versionNumber.Value;

                double dver = 0d;
                string sver = "";

                if (0 != SapApi.GetVersion(ref sver, ref dver)) throw new S2KHelperException("Could not get the SAP2000 version.");

                _versionNumber = dver;
                _versionText = sver;

                return _versionNumber.Value;
            }
        }
        public string VersionText
        {
            get
            {
                if (!string.IsNullOrEmpty(_versionText)) return _versionText;

                double dver = 0d;
                string sver = "";

                if (0 != SapApi.GetVersion(ref sver, ref dver)) throw new S2KHelperException("Could not get the SAP2000 version.");

                _versionNumber = dver;
                _versionText = sver;

                return _versionText;
            }
        }


        UnitsEnum? _presentationUnits = null;
        public UnitsEnum PresentationUnits
        {
            get
            {
                if (_presentationUnits.HasValue) return _presentationUnits.Value;

                try
                {
                    UnitsEnum unit = (UnitsEnum) SapApi.GetPresentUnits();

                    _presentationUnits = unit;

                    return _presentationUnits.Value;
                }
                catch
                {
                    throw new S2KHelperException("Could not get current presentation units.");
                }
            }
            set
            {
                try
                {
                    if (0 != SapApi.SetPresentUnits((eUnits) value))
                        throw new S2KHelperException("Could not set current presentation units.");

                    _presentationUnits = value;
                }
                catch
                {
                    throw new S2KHelperException("Could not set current presentation units.");
                }
            }
        }

        public string PresentationUnitsStringFormat
        {
            get
            {
                string str = PresentationUnits.ToString();
                str = str[0].ToString().ToUpper() + str.Substring(1);
                str = str.Replace("_", ", ");
                return str;
            }
        }

    }
}