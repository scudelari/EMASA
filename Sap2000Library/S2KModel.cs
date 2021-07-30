using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAP2000v1;
using Sap2000Library;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BaseWPFLibrary;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Sap2000Library.Managers;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using Application = System.Windows.Application;

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
                if (_mainInstance == null || SM._sapInstance == null) _mainInstance = StartNewInstance(inUnits, isVisible);
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

        public static void CloseSingleton(bool saveBefore = false)
        {
            if (_mainInstance == null) return;

            // Closes the current instance
            SM.CloseApplication(saveBefore);
            
            try
            {
                // Releases the SAP2000 COM object
                Marshal.ReleaseComObject(SM._sapInstance);
            }
            catch 
            {

            }

            // Sets this singleton as null
            SM._sapInstance = null;
            _mainInstance = null;
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
                cHelper helper = new Helper();
                return new S2KModel(helper.GetObject("CSI.SAP2000.API.SapObject"));
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("Could not get a running instance of SAP2000.", ex);
            }
        }

        public static bool IsThereARunningInstance()
        {
            try
            {
                cHelper helper = new Helper();
                cOAPI oapi = helper.GetObject("CSI.SAP2000.API.SapObject");
                return true;
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
                if (mySapObject.ApplicationStart((eUnits) (int) inUnits, isVisible) != 0) throw new EntryPointNotFoundException($"Could not start SAP2000 Application");

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
                            if (SapInstance.Unhide() != 0)
                                throw new S2KHelperException("It was requested that SAP2000 be shown, but the request failed.");
                        _isCurrentlyVisible = true;
                    }
                }

                else // Shall hide
                {
                    if (_isCurrentlyVisible) // It is currently shown
                    {
                        if (SapInstance.Visible())
                            if (SapInstance.Hide() != 0)
                                throw new S2KHelperException("It was requested that SAP2000 be hidden, but the request failed.");
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
            if (Path.GetExtension(inFileName) != ".sdb" && Path.GetExtension(inFileName) != ".s2k") return false;
            return 0 == SapApi.File.OpenFile(inFileName);
        }

        public bool CloseApplication(bool SaveAsExit)
        {
            try
            {
                bool ret = SapInstance.ApplicationExit(SaveAsExit) == 0;
                return ret;
            }
            catch (Exception)
            {
                return false;
            }
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

            if (0 != SapApi.InitializeNewModel((eUnits) (int) modelUnits)) throw new S2KHelperException("Could not initialize a new model.");

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

        public bool IsAlive
        {
            get
            {
                try
                {
                    string vs = "";
                    double vd = 0d;

                    if (0 != SapApi.GetVersion(ref vs, ref vd)) return false;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// Gets a string representation of the given DataTable. The name of the table must be set and must be what SAP2000 Expects.
        /// The name of the columns and their type must be as SAP2000 expects.
        /// </summary>
        /// <param name="inTable">The table to convert to s2k format.</param>
        /// <returns>The s2k string representation.</returns>
        public static string GetS2KTextFileFormat([NotNull] DataTable inTable)
        {
            if (inTable == null) throw new ArgumentNullException(nameof(inTable));
            if (string.IsNullOrWhiteSpace(inTable.TableName)) throw new S2KHelperException($"The table must have a name.");

            StringBuilder sb = new StringBuilder();

            // Strings have ""
            // True and False becomes Yes/No
            // Sometimes there is a stupid line break that is made with _
            // The tables start with TABLE:  "FRAME DESIGN PROCEDURES"
            // And end with space
            sb.AppendLine($"TABLE:  \"{inTable.TableName}\"");
            foreach (DataRow row in inTable.AsEnumerable())
            {
                sb.Append("   ");

                foreach (DataColumn col in inTable.Columns)
                {
                    // Ignored if DBNull
                    if (row[col] == DBNull.Value) continue;

                    TypeCode colTypeCode = Type.GetTypeCode(col.DataType);

                    sb.Append(col.ColumnName);
                    sb.Append("=");

                    switch (colTypeCode)
                    {
                        case TypeCode.Double:
                            sb.Append((double) row[col]);
                            break;

                        case TypeCode.Int32:
                            sb.Append((int) row[col]);
                            break;

                        case TypeCode.Boolean:
                            sb.Append((bool) row[col] ? "Yes" : "No");
                            break;

                        case TypeCode.String:
                            sb.Append("\"");
                            sb.Append((string) row[col]);
                            sb.Append("\"");
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Type {col.DataType} - TypeCode {colTypeCode} is not supported.");
                    }

                    // 3 spaces between columns
                    sb.Append("   ");
                }

                // breaks a line between lines
                sb.AppendLine();
            }

            // A single line with a single space as end of the Table definition
            sb.AppendLine(" ");

            return sb.ToString();
        }

        public static DataTable TableFormat_GetFromStringName([NotNull] string inTableName)
        {
            if (inTableName == null) throw new ArgumentNullException(nameof(inTableName));

            switch (inTableName)
            {
                case "PROGRAM CONTROL": return TableFormat_ProgramControl;
                case "BUCKLING FACTORS": return TableFormat_BucklingFactors;
                case "ELEMENT FORCES - FRAMES": return TableFormat_ElementForcesFrames;
                case "ELEMENT STRESSES - FRAMES": return TableFormat_ElementStressesFrames;
                case "JOINT DISPLACEMENTS": return TableFormat_JointDisplacements;
                case "JOINT REACTIONS": return TableFormat_JointReactions;
                case "OBJECTS AND ELEMENTS - JOINTS": return TableFormat_ObjectsAndElementsJoints;
                case "OBJECTS AND ELEMENTS - FRAMES": return TableFormat_ObjectsAndElementsFrames;
                case "ANALYSIS MESSAGES": return TableFormat_AnalysisMessages;
                case "STEEL DESIGN 1 - SUMMARY DATA - AISC 360-16": return TableFormat_SteelDesign1SummaryDataAisc360_16;
                case "STEEL DESIGN 2 - PMM DETAILS - AISC 360-16": return TableFormat_SteelDesign2PmmDetailsAisc360_16;
                case "STEEL DESIGN 3 - SHEAR DETAILS - AISC 360-16": return TableFormat_SteelDesign3ShearDetailsAisc360_16;
                case "STEEL DESIGN 9 - DECISION PARAMETERS - AISC 360-16": return TableFormat_SteelDesign9DecisionParametersAisc360_16;

                default:
                    throw new S2KHelperException($"A table named {inTableName} could not be found. Has it already been mapped?");
            }
        }

        public static DataTable TableFormat_ProgramControl
        {
            get
            {
                DataTable t = new DataTable("PROGRAM CONTROL");

                t.Columns.Add(new DataColumn("ProgramName", typeof(string)) { DefaultValue = "SAP2000" });
                t.Columns.Add(new DataColumn("Version", typeof(string)) { DefaultValue = "22.0.0" });
                t.Columns.Add(new DataColumn("ProgLevel", typeof(string)) { DefaultValue = "Ultimate" });
                t.Columns.Add(new DataColumn("LicenseNum", typeof(string)) { DefaultValue = "0" });
                t.Columns.Add(new DataColumn("LicenseOS", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("LicenseSC", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("LicenseHT", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("CurrUnits", typeof(string)) { DefaultValue = "N, m, C" });
                t.Columns.Add(new DataColumn("SteelCode", typeof(string)) { DefaultValue = "AISC 360-16" });
                t.Columns.Add(new DataColumn("ConcCode", typeof(string)) { DefaultValue = "ACI 318-14" });
                t.Columns.Add(new DataColumn("AlumCode", typeof(string)) { DefaultValue = "AA-ASD 2000" });
                t.Columns.Add(new DataColumn("ColdCode", typeof(string)) { DefaultValue = "AISI-ASD96" });
                t.Columns.Add(new DataColumn("RegenHinge", typeof(bool)) { DefaultValue = true });

                return t;
            }
        }
        public static DataTable TableFormat_PreferencesDimensional
        {
            get
            {
                DataTable t = new DataTable("PREFERENCES - DIMENSIONAL");

                t.Columns.Add(new DataColumn("MergeTol", typeof(double)) { DefaultValue = 0.001d });
                t.Columns.Add(new DataColumn("FineGrid", typeof(double)) { DefaultValue = 0.25d });
                t.Columns.Add(new DataColumn("Nudge", typeof(double)) { DefaultValue = 0.25d });
                t.Columns.Add(new DataColumn("SelectTol", typeof(double)) { DefaultValue = 3d });
                t.Columns.Add(new DataColumn("SnapTol", typeof(double)) { DefaultValue = 12d });
                t.Columns.Add(new DataColumn("SLineThick", typeof(double)) { DefaultValue = 2d });
                t.Columns.Add(new DataColumn("PLineThick", typeof(double)) { DefaultValue = 4d });
                t.Columns.Add(new DataColumn("MaxFont", typeof(double)) { DefaultValue = 8d });
                t.Columns.Add(new DataColumn("MinFont", typeof(double)) { DefaultValue = 3d });
                t.Columns.Add(new DataColumn("AutoZoom", typeof(double)) { DefaultValue = 10d });
                t.Columns.Add(new DataColumn("ShrinkFact", typeof(double)) { DefaultValue = 70d });
                t.Columns.Add(new DataColumn("TextFileLen", typeof(double)) { DefaultValue = 240d });

                return t;
            }
        }


        public static DataTable TableFormat_ActiveDegreesOfFreedom
        {
            get
            {
                DataTable t = new DataTable("ACTIVE DEGREES OF FREEDOM");

                t.Columns.Add(new DataColumn("UX", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("UY", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("UZ", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("RX", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("RY", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("RZ", typeof(bool)) { DefaultValue = true });

                return t;
            }
        }
        public static DataTable TableFormat_AnalysisOptions
        {
            get
            {
                DataTable t = new DataTable("ANALYSIS OPTIONS");

                t.Columns.Add(new DataColumn("Solver", typeof(string)) { DefaultValue = "Advanced" });
                t.Columns.Add(new DataColumn("SolverProc", typeof(string)) { DefaultValue = "Auto" });
                t.Columns.Add(new DataColumn("Force32Bit", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("StiffCase", typeof(string)) { DefaultValue = "None" });
                t.Columns.Add(new DataColumn("GeomMod", typeof(string)) { DefaultValue = "None" });
                t.Columns.Add(new DataColumn("HingeOpt", typeof(string)) { DefaultValue = "In Elements" });

                return t;
            }
        }
        public static DataTable TableFormat_CoordinateSystems
        {
            get
            {
                DataTable t = new DataTable("COORDINATE SYSTEMS");

                t.Columns.Add(new DataColumn("Name", typeof(string)) { DefaultValue = "GLOBAL" });
                t.Columns.Add(new DataColumn("Cartesian", typeof(string)) { DefaultValue = "Cartesian" });
                t.Columns.Add(new DataColumn("X", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Y", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Z", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("AboutZ", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("AboutY", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("AboutX", typeof(double)) { DefaultValue = 0d });

                return t;
            }
        }
        public static DataTable TableFormat_GridLines
        {
            get
            {
                DataTable t = new DataTable("GRID LINES");

                t.Columns.Add(new DataColumn("CoordSys", typeof(string)) { DefaultValue = "GLOBAL" });
                t.Columns.Add(new DataColumn("AxisDir", typeof(string)) );
                t.Columns.Add(new DataColumn("GridID", typeof(string)) );
                t.Columns.Add(new DataColumn("XRYZCoord", typeof(double)) );
                t.Columns.Add(new DataColumn("LineType", typeof(string)) { DefaultValue = "Primary" });
                t.Columns.Add(new DataColumn("LineColor", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value});
                t.Columns.Add(new DataColumn("Visible", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("BubbleLoc", typeof(string)) { DefaultValue = "End" });
                t.Columns.Add(new DataColumn("AllVisible", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("BubbleSize", typeof(double)) { DefaultValue = 0.375 });

                return t;
            }
        }
        public static DataTable TableFormat_MaterialProperties01General
        {
            get
            {
                DataTable t = new DataTable("MATERIAL PROPERTIES 01 - GENERAL");

                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("Type", typeof(string)));
                t.Columns.Add(new DataColumn("Grade", typeof(string)));
                t.Columns.Add(new DataColumn("SymType", typeof(string)) { DefaultValue = "Isotropic" });
                t.Columns.Add(new DataColumn("TempDepend", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("Color", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Notes", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }
        public static DataTable TableFormat_MaterialProperties02BasicMechanicalProperties
        {
            get
            {
                DataTable t = new DataTable("MATERIAL PROPERTIES 02 - BASIC MECHANICAL PROPERTIES");

                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("UnitWeight", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("UnitMass", typeof(double)));
                t.Columns.Add(new DataColumn("E1", typeof(double)));
                t.Columns.Add(new DataColumn("G12", typeof(double)) {AllowDBNull = true, DefaultValue = DBNull.Value});
                t.Columns.Add(new DataColumn("U12", typeof(double)));
                t.Columns.Add(new DataColumn("A1", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_MaterialProperties03ASteelData
        {
            get
            {
                DataTable t = new DataTable("MATERIAL PROPERTIES 03A - STEEL DATA");

                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("Fy", typeof(double)));
                t.Columns.Add(new DataColumn("Fu", typeof(double)));
                t.Columns.Add(new DataColumn("EffFy", typeof(double)));
                t.Columns.Add(new DataColumn("EffFu", typeof(double)));

                t.Columns.Add(new DataColumn("SSCurveOpt", typeof(string)) { DefaultValue = "Simple" });
                t.Columns.Add(new DataColumn("SSHysType", typeof(string)) { DefaultValue = "Kinematic" });

                t.Columns.Add(new DataColumn("SHard", typeof(double)));
                t.Columns.Add(new DataColumn("SMax", typeof(double)));
                t.Columns.Add(new DataColumn("SRup", typeof(double)));
                t.Columns.Add(new DataColumn("FinalSlope", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_MaterialProperties03BConcreteData
        {
            get
            {
                DataTable t = new DataTable("MATERIAL PROPERTIES 03B - CONCRETE DATA");

                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("Fc", typeof(double)));
                t.Columns.Add(new DataColumn("eFc", typeof(double)));
                t.Columns.Add(new DataColumn("LtWtConc", typeof(bool)));

                t.Columns.Add(new DataColumn("SSCurveOpt", typeof(string)) { DefaultValue = "Mander" });
                t.Columns.Add(new DataColumn("SSHysType", typeof(string)) { DefaultValue = "Takeda" });

                t.Columns.Add(new DataColumn("SFc", typeof(double)));
                t.Columns.Add(new DataColumn("SCap", typeof(double)));
                t.Columns.Add(new DataColumn("FinalSlope", typeof(double)));
                t.Columns.Add(new DataColumn("FAngle", typeof(double)));
                t.Columns.Add(new DataColumn("DAngle", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_MaterialProperties06DampingParameters
        {
            get
            {
                DataTable t = new DataTable("MATERIAL PROPERTIES 06 - DAMPING PARAMETERS");

                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("ModalRatio", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("VisMass", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("VisStiff", typeof(double)) { DefaultValue = 0d });

                t.Columns.Add(new DataColumn("HysMass", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("HysStiff", typeof(double)) { DefaultValue = 0d });

                return t;
            }
        }
        public static DataTable TableFormat_FrameSectionProperties01General
        {
            get
            {
                DataTable t = new DataTable("FRAME SECTION PROPERTIES 01 - GENERAL");

                t.Columns.Add(new DataColumn("SectionName", typeof(string)));
                t.Columns.Add(new DataColumn("Material", typeof(string)));
                t.Columns.Add(new DataColumn("Shape", typeof(string)));
                t.Columns.Add(new DataColumn("t3", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value , });
                t.Columns.Add(new DataColumn("t2", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("tf", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("tw", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("t2b", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("tfb", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("dis", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                t.Columns.Add(new DataColumn("Area", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TorsConst", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("I33", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("I22", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("I23", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("AS2", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("AS3", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("S33", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("S22", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Z33", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Z22", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("R33", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("R22", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("EccV2", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value , });
                t.Columns.Add(new DataColumn("ConcCol", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("ConcBeam", typeof(bool)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("Color", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TotalWt", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TotalMass", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("FromFile", typeof(bool)) { AllowDBNull = false, DefaultValue = false, });

                t.Columns.Add(new DataColumn("AMod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("A2Mod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("A3Mod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("JMod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("I2Mod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("I3Mod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MMod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("WMod", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Notes", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }
        public static DataTable TableFormat_LoadPatternDefinitions
        {
            get
            {
                DataTable t = new DataTable("LOAD PATTERN DEFINITIONS");

                t.Columns.Add(new DataColumn("LoadPat", typeof(string)));
                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("SelfWtMult", typeof(double)));
                t.Columns.Add(new DataColumn("AutoLoad", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Notes", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }
        public static DataTable TableFormat_LoadCaseDefinitions
        {
            get
            {
                DataTable t = new DataTable("LOAD CASE DEFINITIONS");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("Type", typeof(string)));
                t.Columns.Add(new DataColumn("InitialCond", typeof(string)));
                t.Columns.Add(new DataColumn("ModalCase", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("BaseCase", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MassSource", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("DesTypeOpt", typeof(string)) { DefaultValue = "Prog Det", });
                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("DesActOpt", typeof(string)) { DefaultValue = "Prog Det", });
                t.Columns.Add(new DataColumn("DesignAct", typeof(string)));
                t.Columns.Add(new DataColumn("AutoType", typeof(string)) { DefaultValue = "None", });
                t.Columns.Add(new DataColumn("RunCase", typeof(bool)) { DefaultValue = true, });
                t.Columns.Add(new DataColumn("CaseStatus", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Notes", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }
        public static DataTable TableFormat_CombinationDefinitions
        {
            get
            {
                DataTable t = new DataTable("COMBINATION DEFINITIONS");

                t.Columns.Add(new DataColumn("ComboName", typeof(string)));
                t.Columns.Add(new DataColumn("ComboType", typeof(string)));
                t.Columns.Add(new DataColumn("AutoDesign", typeof(bool)));
                t.Columns.Add(new DataColumn("CaseType", typeof(string)) { DefaultValue = "NonLin Static", });
                t.Columns.Add(new DataColumn("CaseName", typeof(string)));
                t.Columns.Add(new DataColumn("ScaleFactor", typeof(double)));
                t.Columns.Add(new DataColumn("SteelDesign", typeof(string)) { DefaultValue = "Strength", });
                t.Columns.Add(new DataColumn("ConcDesign", typeof(string)) { DefaultValue = "None", });
                t.Columns.Add(new DataColumn("AlumDesign", typeof(string)) { DefaultValue = "None", });
                t.Columns.Add(new DataColumn("ColdDesign", typeof(string)) { DefaultValue = "None", });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("Notes", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });


                return t;
            }
        }
        public static DataTable TableFormat_AutoCombinationOptionData01General
        {
            get
            {
                DataTable t = new DataTable("AUTO COMBINATION OPTION DATA 01 - GENERAL");

                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("AutoGen", typeof(bool)) { DefaultValue = false });

                return t;
            }
        }
        public static DataTable TableFormat_CaseBuckling1General
        {
            get
            {
                DataTable t = new DataTable("CASE - BUCKLING 1 - GENERAL");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("NumBuckMode", typeof(double)));
                t.Columns.Add(new DataColumn("EigenTol", typeof(double)) { DefaultValue = 0.000000001d, });

                return t;
            }
        }
        public static DataTable TableFormat_CaseBuckling2LoadAssignments
        {
            get
            {
                DataTable t = new DataTable("CASE - BUCKLING 2 - LOAD ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("LoadType", typeof(string)) { DefaultValue = "Load pattern", });
                t.Columns.Add(new DataColumn("LoadName", typeof(string)));
                t.Columns.Add(new DataColumn("LoadSF", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_CaseStatic1LoadAssignments
        {
            get
            {
                DataTable t = new DataTable("CASE - STATIC 1 - LOAD ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("LoadType", typeof(string)) { DefaultValue = "Load pattern", });
                t.Columns.Add(new DataColumn("LoadName", typeof(string)));
                t.Columns.Add(new DataColumn("LoadSF", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_CaseStatic2NonLinearLoadApplication
        {
            get
            {
                DataTable t = new DataTable("CASE - STATIC 2 - NONLINEAR LOAD APPLICATION");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("LoadApp", typeof(string)));
                t.Columns.Add(new DataColumn("MonitorDOF", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MonitorJt", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });


                return t;
            }
        }
        public static DataTable TableFormat_CaseStatic4NonLinearParameters
        {
            get
            {
                DataTable t = new DataTable("CASE - STATIC 4 - NONLINEAR PARAMETERS");

                t.Columns.Add(new DataColumn("Case", typeof(string)));
                t.Columns.Add(new DataColumn("GeoNonLin", typeof(string)) { DefaultValue = "Large Displ" });
                t.Columns.Add(new DataColumn("ResultsSave", typeof(string)) { DefaultValue = "Final State" });
                t.Columns.Add(new DataColumn("MaxTotal", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MaxNull", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("UseEvStep", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("EvLumpTol", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MaxEvPerStp", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("UseIter", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MaxIterCS", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MaxIterNR", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("ItConvTol", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("UseLineSrch", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("StageSave", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("StageMinIns", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("StageMinTD", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TimeDepMat", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TFMaxIter", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TFTol", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TFAccelFact", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("TFNoStop", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });


                return t;
            }
        }
        public static DataTable TableFormat_JointCoordinates
        {
            get
            {
                DataTable t = new DataTable("JOINT COORDINATES");

                t.Columns.Add(new DataColumn("Joint", typeof(string)));
                t.Columns.Add(new DataColumn("CoordSys", typeof(string)) { DefaultValue = "GLOBAL", });
                t.Columns.Add(new DataColumn("CoordType", typeof(string)) { DefaultValue = "Cartesian", });
                t.Columns.Add(new DataColumn("XorR", typeof(double)));
                t.Columns.Add(new DataColumn("Y", typeof(double)));
                t.Columns.Add(new DataColumn("Z", typeof(double)));
                t.Columns.Add(new DataColumn("SpecialJt", typeof(bool)));
                t.Columns.Add(new DataColumn("GlobalX", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GlobalY", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GlobalZ", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                
                return t;
            }
        }
        public static DataTable TableFormat_JointRestraintAssignments
        {
            get
            {
                DataTable t = new DataTable("JOINT RESTRAINT ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Joint", typeof(string)));
                t.Columns.Add(new DataColumn("U1", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("U2", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("U3", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("R1", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("R2", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("R3", typeof(bool)) { DefaultValue = false, });

                return t;
            }
        }

        public static DataTable TableFormat_JointLoads_Force
        {
            get
            {
                DataTable t = new DataTable("JOINT LOADS - FORCE");

                t.Columns.Add(new DataColumn("Joint", typeof(string)));
                t.Columns.Add(new DataColumn("LoadPat", typeof(string)));
                t.Columns.Add(new DataColumn("CoordSys", typeof(string)) { DefaultValue = "GLOBAL" });
                t.Columns.Add(new DataColumn("F1", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("F2", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("F3", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("M1", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("M2", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("M3", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) {AllowDBNull = true, DefaultValue = DBNull.Value});

                return t;
            }
        }
        public static DataTable TableFormat_FrameSectionAssignments
        {
            get
            {
                DataTable t = new DataTable("FRAME SECTION ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("SectionType", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("AutoSelect", typeof(string)) { DefaultValue = "N.A.", });
                t.Columns.Add(new DataColumn("AnalSect", typeof(string)));
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MatProp", typeof(string)) { DefaultValue = "Default", });

                return t;
            }
        }
        public static DataTable TableFormat_FrameReleaseAssignments1General
        {
            get
            {
                DataTable t = new DataTable("FRAME RELEASE ASSIGNMENTS 1 - GENERAL");
                
                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("PI", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("V2I", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("V3I", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("TI", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("M2I", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("M3I", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("PJ", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("V2J", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("V3J", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("TJ", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("M2J", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("M3J", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("PartialFix", typeof(bool)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }
        public static DataTable TableFormat_FrameOutputStationAssignments
        {
            get
            {
                DataTable t = new DataTable("FRAME OUTPUT STATION ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("StationType", typeof(string)) { DefaultValue = "MinNumSta", });
                t.Columns.Add(new DataColumn("MinNumSta", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("MaxStaSpcg", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("AddAtElmInt", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("AddAtPtLoad", typeof(bool)) { DefaultValue = false, });

                return t;
            }
        }
        public static DataTable TableFormat_FrameAutoMeshAssignments
        {
            get
            {
                DataTable t = new DataTable("FRAME AUTO MESH ASSIGNMENTS");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("AutoMesh", typeof(bool)) { DefaultValue = true, });
                t.Columns.Add(new DataColumn("AtJoints", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("AtFrames", typeof(bool)) { DefaultValue = false, });
                t.Columns.Add(new DataColumn("NumSegments", typeof(double)) { DefaultValue = 2d, });
                t.Columns.Add(new DataColumn("MaxLength", typeof(double)) { DefaultValue = 0d, });
                t.Columns.Add(new DataColumn("MaxDegrees", typeof(double)) { DefaultValue = 0d, });

                return t;
            }
        }
        public static DataTable TableFormat_PreferencesSteelDesignAisc360_16
        {
            get
            {
                DataTable t = new DataTable("PREFERENCES - STEEL DESIGN - AISC 360-16");

                t.Columns.Add(new DataColumn("THDesign", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("FrameType", typeof(string)) { DefaultValue = "SMF" });
                t.Columns.Add(new DataColumn("PatLLF", typeof(double)) { DefaultValue = 0.75d });
                t.Columns.Add(new DataColumn("SRatioLimit", typeof(double)) { DefaultValue = 0.95d });
                t.Columns.Add(new DataColumn("MaxIter", typeof(double)) { DefaultValue = 1d });
                t.Columns.Add(new DataColumn("SDC", typeof(string)) { DefaultValue = "D" });
                t.Columns.Add(new DataColumn("SeisCode", typeof(double)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("SeisLoad", typeof(double)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("ImpFactor", typeof(double)) { DefaultValue = 1d });
                t.Columns.Add(new DataColumn("SystemRho", typeof(double)) { DefaultValue = 1d });
                t.Columns.Add(new DataColumn("SystemSds", typeof(double)) { DefaultValue = 0.5d });
                t.Columns.Add(new DataColumn("SystemR", typeof(double)) { DefaultValue = 8d });
                t.Columns.Add(new DataColumn("SystemCd", typeof(double)) { DefaultValue = 5.5d });
                t.Columns.Add(new DataColumn("Omega0", typeof(double)) { DefaultValue = 3d });
                t.Columns.Add(new DataColumn("Provision", typeof(string)) { DefaultValue = "LRFD" });
                t.Columns.Add(new DataColumn("AMethod", typeof(string)) { DefaultValue = "Direct Analysis" });
                t.Columns.Add(new DataColumn("SOMethod", typeof(string)) { DefaultValue = "General 2nd Order" });
                t.Columns.Add(new DataColumn("SRMethod", typeof(string)) { DefaultValue = "Tau-b Fixed" });
                t.Columns.Add(new DataColumn("NLCoeff", typeof(double)) { DefaultValue = 0.002d });
                t.Columns.Add(new DataColumn("PhiB", typeof(double)) { DefaultValue = 0.9d });
                t.Columns.Add(new DataColumn("PhiC", typeof(double)) { DefaultValue = 0.9d });
                t.Columns.Add(new DataColumn("PhiTY", typeof(double)) { DefaultValue = 0.9d });
                t.Columns.Add(new DataColumn("PhiTF", typeof(double)) { DefaultValue = 0.75d });
                t.Columns.Add(new DataColumn("PhiV", typeof(double)) { DefaultValue = 0.9d });
                t.Columns.Add(new DataColumn("PhiVRolledI", typeof(double)) { DefaultValue = 1d });
                t.Columns.Add(new DataColumn("PhiVT", typeof(double)) { DefaultValue = 0.9d });
                t.Columns.Add(new DataColumn("PlugWeld", typeof(double)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("HSSWelding", typeof(string)) { DefaultValue = "ERW" });
                t.Columns.Add(new DataColumn("HSSReduceT", typeof(double)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("CheckDefl", typeof(double)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("DLRat", typeof(double)) { DefaultValue = 120d });
                t.Columns.Add(new DataColumn("SDLAndLLRat", typeof(double)) { DefaultValue = 120d });
                t.Columns.Add(new DataColumn("LLRat", typeof(double)) { DefaultValue = 360d });
                t.Columns.Add(new DataColumn("TotalRat", typeof(double)) { DefaultValue = 240d });
                t.Columns.Add(new DataColumn("NetRat", typeof(double)) { DefaultValue = 240d });


                return t;
            }
        }
        public static DataTable TableFormat_ConnectivityFrame
        {
            get
            {
                DataTable t = new DataTable("CONNECTIVITY - FRAME");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("JointI", typeof(string)));
                t.Columns.Add(new DataColumn("JointJ", typeof(string)));
                t.Columns.Add(new DataColumn("IsCurved", typeof(double)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("Length", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("CentroidX", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("CentroidY", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("CentroidZ", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value, });
                t.Columns.Add(new DataColumn("GUID", typeof(string)) { AllowDBNull = true, DefaultValue = DBNull.Value, });

                return t;
            }
        }

        public static DataTable TableFormat_NamedSetsDatabaseTables1General
        {
            get
            {
                DataTable t = new DataTable("NAMED SETS - DATABASE TABLES 1 - GENERAL");

                t.Columns.Add(new DataColumn("DBNamedSet", typeof(string)));
                t.Columns.Add(new DataColumn("SortOrder", typeof(string)) { DefaultValue = "Cases, Elem" });
                t.Columns.Add(new DataColumn("Unformatted", typeof(bool)) { DefaultValue = true });
                t.Columns.Add(new DataColumn("ModeStart", typeof(string)) { DefaultValue = "1" });
                t.Columns.Add(new DataColumn("ModeEnd", typeof(string)) { DefaultValue = "All" });
                t.Columns.Add(new DataColumn("ModalHist", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("DirectHist", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("NLStatic", typeof(string)) { DefaultValue = "LastStep" });
                t.Columns.Add(new DataColumn("BaseReacX", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("BaseReacY", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("BaseReacZ", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Combo", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("Steady", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("SteadyOpt", typeof(string)) { DefaultValue = "Phases" });
                t.Columns.Add(new DataColumn("PSD", typeof(string)) { DefaultValue = "RMS" });
                t.Columns.Add(new DataColumn("Multistep", typeof(string)) { DefaultValue = "Envelopes" });
                t.Columns.Add(new DataColumn("NumTables", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value});
                t.Columns.Add(new DataColumn("NumLoads", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumCases", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumGenDispl", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumSectCuts", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumVWSets", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumNLSSets", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumRSSets", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });
                t.Columns.Add(new DataColumn("NumPFSets", typeof(double)) { AllowDBNull = true, DefaultValue = DBNull.Value });


                return t;
            }
        }
        public static DataTable TableFormat_NamedSetsDatabaseTables2Selections
        {
            get
            {
                DataTable t = new DataTable("NAMED SETS - DATABASE TABLES 2 - SELECTIONS");
                
                t.Columns.Add(new DataColumn("DBNamedSet", typeof(string)));
                t.Columns.Add(new DataColumn("SelectType", typeof(string)));
                t.Columns.Add(new DataColumn("Selection", typeof(string)));

                return t;
            }
        }
        public static DataTable TableFormat_TablesAutomaticallySavedAfterAnalysis
        {
            get
            {
                DataTable t = new DataTable("TABLES AUTOMATICALLY SAVED AFTER ANALYSIS");

                t.Columns.Add(new DataColumn("DBNamedSet", typeof(string)));
                t.Columns.Add(new DataColumn("SelectType", typeof(string)));
                t.Columns.Add(new DataColumn("Selection", typeof(string)));

                return t;
            }
        }

        public static DataTable TableFormat_BucklingFactors
        {
            get
            {
                DataTable t = new DataTable("BUCKLING FACTORS");

                t.Columns.Add(new DataColumn("OutputCase", typeof(string)));
                t.Columns.Add(new DataColumn("StepType", typeof(string)));
                t.Columns.Add(new DataColumn("StepNum", typeof(double)));
                t.Columns.Add(new DataColumn("ScaleFactor", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_ElementForcesFrames
        {
            get
            {
                DataTable t = new DataTable("ELEMENT FORCES - FRAMES");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("Station", typeof(double)));
                t.Columns.Add(new DataColumn("OutputCase", typeof(string)));
                t.Columns.Add(new DataColumn("CaseType", typeof(string)));
                t.Columns.Add(new DataColumn("StepType", typeof(string)));
                t.Columns.Add(new DataColumn("StepNum", typeof(double)));
                t.Columns.Add(new DataColumn("P", typeof(double)));
                t.Columns.Add(new DataColumn("V2", typeof(double)));
                t.Columns.Add(new DataColumn("V3", typeof(double)));
                t.Columns.Add(new DataColumn("T", typeof(double)));
                t.Columns.Add(new DataColumn("M2", typeof(double)));
                t.Columns.Add(new DataColumn("M3", typeof(double)));
                t.Columns.Add(new DataColumn("FrameElem", typeof(string)));
                t.Columns.Add(new DataColumn("ElemStation", typeof(double)));


                return t;
            }
        }
        public static DataTable TableFormat_ElementStressesFrames
        {
            get
            {
                DataTable t = new DataTable("ELEMENT STRESSES - FRAMES");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("Station", typeof(double)));
                t.Columns.Add(new DataColumn("OutputCase", typeof(string)));
                t.Columns.Add(new DataColumn("CaseType", typeof(string)));
                t.Columns.Add(new DataColumn("StepType", typeof(string)));
                t.Columns.Add(new DataColumn("StepNum", typeof(double)));
                t.Columns.Add(new DataColumn("Point", typeof(string)));
                t.Columns.Add(new DataColumn("X2", typeof(double)));
                t.Columns.Add(new DataColumn("X3", typeof(double)));
                t.Columns.Add(new DataColumn("S11", typeof(double)));
                t.Columns.Add(new DataColumn("S12", typeof(double)));
                t.Columns.Add(new DataColumn("S13", typeof(double)));
                t.Columns.Add(new DataColumn("SMax", typeof(double)));
                t.Columns.Add(new DataColumn("SMin", typeof(double)));
                t.Columns.Add(new DataColumn("SVM", typeof(double)));
                t.Columns.Add(new DataColumn("IsS11Max", typeof(bool)));
                t.Columns.Add(new DataColumn("IsS12Max", typeof(bool)));
                t.Columns.Add(new DataColumn("IsS13Max", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSMaxMax", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSMinMax", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSVMMax", typeof(bool)));
                t.Columns.Add(new DataColumn("IsS11Min", typeof(bool)));
                t.Columns.Add(new DataColumn("IsS12Min", typeof(bool)));
                t.Columns.Add(new DataColumn("IsS31Min", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSMaxMin", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSMinMin", typeof(bool)));
                t.Columns.Add(new DataColumn("IsSVMMin", typeof(bool)));
                t.Columns.Add(new DataColumn("FrameElem", typeof(string)));
                t.Columns.Add(new DataColumn("ElemStation", typeof(double)));



                return t;
            }
        }
        public static DataTable TableFormat_JointDisplacements
        {
            get
            {
                DataTable t = new DataTable("JOINT DISPLACEMENTS");

                t.Columns.Add(new DataColumn("Joint", typeof(string)));
                t.Columns.Add(new DataColumn("OutputCase", typeof(string)));
                t.Columns.Add(new DataColumn("CaseType", typeof(string)));
                t.Columns.Add(new DataColumn("StepType", typeof(string)));
                t.Columns.Add(new DataColumn("StepNum", typeof(double)));
                t.Columns.Add(new DataColumn("U1", typeof(double)));
                t.Columns.Add(new DataColumn("U2", typeof(double)));
                t.Columns.Add(new DataColumn("U3", typeof(double)));
                t.Columns.Add(new DataColumn("R1", typeof(double)));
                t.Columns.Add(new DataColumn("R2", typeof(double)));
                t.Columns.Add(new DataColumn("R3", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_JointReactions
        {
            get
            {
                DataTable t = new DataTable("JOINT REACTIONS");

                t.Columns.Add(new DataColumn("Joint", typeof(string)));
                t.Columns.Add(new DataColumn("OutputCase", typeof(string)));
                t.Columns.Add(new DataColumn("CaseType", typeof(string)));
                t.Columns.Add(new DataColumn("StepType", typeof(string)));
                t.Columns.Add(new DataColumn("StepNum", typeof(double)));
                t.Columns.Add(new DataColumn("F1", typeof(double)));
                t.Columns.Add(new DataColumn("F2", typeof(double)));
                t.Columns.Add(new DataColumn("F3", typeof(double)));
                t.Columns.Add(new DataColumn("M1", typeof(double)));
                t.Columns.Add(new DataColumn("M2", typeof(double)));
                t.Columns.Add(new DataColumn("M3", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_ObjectsAndElementsJoints
        {
            get
            {
                DataTable t = new DataTable("OBJECTS AND ELEMENTS - JOINTS");

                t.Columns.Add(new DataColumn("JointElem", typeof(string)));
                t.Columns.Add(new DataColumn("JointObject", typeof(string)) {AllowDBNull = true, DefaultValue = DBNull.Value});
                t.Columns.Add(new DataColumn("GlobalX", typeof(double)));
                t.Columns.Add(new DataColumn("GlobalY", typeof(double)));
                t.Columns.Add(new DataColumn("GlobalZ", typeof(double)));

                return t;
            }
        }
        public static DataTable TableFormat_ObjectsAndElementsFrames
        {
            get
            {
                DataTable t = new DataTable("OBJECTS AND ELEMENTS - FRAMES");

                t.Columns.Add(new DataColumn("FrameElem", typeof(string)));
                t.Columns.Add(new DataColumn("FrameObject", typeof(string)));
                t.Columns.Add(new DataColumn("ElemJtI", typeof(string)));
                t.Columns.Add(new DataColumn("ElemJtJ", typeof(string)));

                return t;
            }
        }

        public static DataTable TableFormat_AnalysisMessages
        {
            get
            {
                DataTable t = new DataTable("ANALYSIS MESSAGES");

                t.Columns.Add(new DataColumn("DateTime", typeof(string)));
                t.Columns.Add(new DataColumn("Type", typeof(string)));
                t.Columns.Add(new DataColumn("Message", typeof(string)));
                t.Columns.Add(new DataColumn("LoadCase", typeof(string)));
                t.Columns.Add(new DataColumn("Operation", typeof(string)));
                t.Columns.Add(new DataColumn("TagRun", typeof(double)));
                t.Columns.Add(new DataColumn("SerialRun", typeof(double)));
                t.Columns.Add(new DataColumn("Computer", typeof(string)));

                return t;
            }
        }
        
        public static DataTable TableFormat_OverwritesSteelDesignAisc360_16
        {
            get
            {
                DataTable t = new DataTable("OVERWRITES - STEEL DESIGN - AISC 360-16");

                t.Columns.Add(new DataColumn("Frame", typeof(string)) { DefaultValue = "1" });
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)) { DefaultValue = "Program Determined" });
                t.Columns.Add(new DataColumn("FrameType", typeof(string)) { DefaultValue = "Program Determined" });
                t.Columns.Add(new DataColumn("Fy", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("RLLF", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("AreaRatio", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("XLMajor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("XLMinor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("XLLTB", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("K1Major", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("K1Minor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("K2Major", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("K2Minor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("KLTB", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("CmMajor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("CmMinor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Cb", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("B1Major", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("B1Minor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("B2Major", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("B2Minor", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("HSSReduceT", typeof(double)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("HSSWelding", typeof(string)) { DefaultValue = "Program Determined" });
                t.Columns.Add(new DataColumn("Omega0", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Ry", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Pnc", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Pnt", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Mn3", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Mn2", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Vn2", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("Vn3", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("CheckDefl", typeof(double)) { DefaultValue = false });
                t.Columns.Add(new DataColumn("DeflType", typeof(string)) { DefaultValue = "Program Determined" });
                t.Columns.Add(new DataColumn("DLRat", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("SDLAndLLRat", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("LLRat", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("TotalRat", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("NetRat", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("DLAbs", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("SDLAndLLAbs", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("LLAbs", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("TotalAbs", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("NetAbs", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("SpecCamber", typeof(double)) { DefaultValue = 0d });
                t.Columns.Add(new DataColumn("DCLimit", typeof(double)) { DefaultValue = 0d });


                return t;
            }
        }

        public static DataTable TableFormat_SteelDesign1SummaryDataAisc360_16
        {
            get
            {
                DataTable t = new DataTable("STEEL DESIGN 1 - SUMMARY DATA - AISC 360-16");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)));
                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("Status", typeof(string)));
                t.Columns.Add(new DataColumn("Ratio", typeof(double)));
                t.Columns.Add(new DataColumn("RatioType", typeof(string)));
                t.Columns.Add(new DataColumn("Combo", typeof(string)));
                t.Columns.Add(new DataColumn("Location", typeof(double)));
                t.Columns.Add(new DataColumn("ErrMsg", typeof(string)));
                t.Columns.Add(new DataColumn("WarnMsg", typeof(string)));

                return t;
            }
        }
        public static DataTable TableFormat_SteelDesign2PmmDetailsAisc360_16
        {
            get
            {
                DataTable t = new DataTable("STEEL DESIGN 2 - PMM DETAILS - AISC 360-16");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)));
                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("Status", typeof(string)));
                t.Columns.Add(new DataColumn("Combo", typeof(string)));
                t.Columns.Add(new DataColumn("Location", typeof(double)));
                t.Columns.Add(new DataColumn("Pr", typeof(double)));
                t.Columns.Add(new DataColumn("MrMajor", typeof(double)));
                t.Columns.Add(new DataColumn("MrMinor", typeof(double)));
                t.Columns.Add(new DataColumn("VrMajor", typeof(double)));
                t.Columns.Add(new DataColumn("VrMinor", typeof(double)));
                t.Columns.Add(new DataColumn("Tr", typeof(double)));
                t.Columns.Add(new DataColumn("Equation", typeof(string)));
                t.Columns.Add(new DataColumn("TotalRatio", typeof(double)));
                t.Columns.Add(new DataColumn("PRatio", typeof(double)));
                t.Columns.Add(new DataColumn("MMajRatio", typeof(double)));
                t.Columns.Add(new DataColumn("MMinRatio", typeof(double)));
                t.Columns.Add(new DataColumn("VMajRatio", typeof(double)));
                t.Columns.Add(new DataColumn("VMinRatio", typeof(double)));
                t.Columns.Add(new DataColumn("TorRatio", typeof(double)));
                t.Columns.Add(new DataColumn("DCLimit", typeof(double)));
                t.Columns.Add(new DataColumn("PrDsgn", typeof(double)));
                t.Columns.Add(new DataColumn("PcComp", typeof(double)));
                t.Columns.Add(new DataColumn("PcTension", typeof(double)));
                t.Columns.Add(new DataColumn("MrMajorDsgn", typeof(double)));
                t.Columns.Add(new DataColumn("McMajor", typeof(double)));
                t.Columns.Add(new DataColumn("MrMinorDsgn", typeof(double)));
                t.Columns.Add(new DataColumn("McMinor", typeof(double)));
                t.Columns.Add(new DataColumn("XLMajor", typeof(double)));
                t.Columns.Add(new DataColumn("XLMinor", typeof(double)));
                t.Columns.Add(new DataColumn("XLLTB", typeof(double)));
                t.Columns.Add(new DataColumn("K1Major", typeof(double)));
                t.Columns.Add(new DataColumn("K1Minor", typeof(double)));
                t.Columns.Add(new DataColumn("K2Major", typeof(double)));
                t.Columns.Add(new DataColumn("K2Minor", typeof(double)));
                t.Columns.Add(new DataColumn("KLTB", typeof(double)));
                t.Columns.Add(new DataColumn("CmMajor", typeof(double)));
                t.Columns.Add(new DataColumn("CmMinor", typeof(double)));
                t.Columns.Add(new DataColumn("Cb", typeof(double)));
                t.Columns.Add(new DataColumn("B1Major", typeof(double)));
                t.Columns.Add(new DataColumn("B1Minor", typeof(double)));
                t.Columns.Add(new DataColumn("B2Major", typeof(double)));
                t.Columns.Add(new DataColumn("B2Minor", typeof(double)));
                t.Columns.Add(new DataColumn("Fy", typeof(double)));
                t.Columns.Add(new DataColumn("E", typeof(double)));
                t.Columns.Add(new DataColumn("Length", typeof(double)));
                t.Columns.Add(new DataColumn("MajAxisAng", typeof(double)));
                t.Columns.Add(new DataColumn("RLLF", typeof(double)));
                t.Columns.Add(new DataColumn("SectClass", typeof(string)));
                t.Columns.Add(new DataColumn("FramingType", typeof(string)));
                t.Columns.Add(new DataColumn("SDC", typeof(string)));
                t.Columns.Add(new DataColumn("Omega0", typeof(double)));
                t.Columns.Add(new DataColumn("SystemCd", typeof(double)));
                t.Columns.Add(new DataColumn("ErrMsg", typeof(string)));
                t.Columns.Add(new DataColumn("WarnMsg", typeof(string)));


                return t;
            }
        }
        public static DataTable TableFormat_SteelDesign3ShearDetailsAisc360_16
        {
            get
            {
                DataTable t = new DataTable("STEEL DESIGN 3 - SHEAR DETAILS - AISC 360-16");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)));
                t.Columns.Add(new DataColumn("DesignType", typeof(string)));
                t.Columns.Add(new DataColumn("Status", typeof(string)));
                t.Columns.Add(new DataColumn("VMajorCombo", typeof(string)));
                t.Columns.Add(new DataColumn("VMajorLoc", typeof(double)));
                t.Columns.Add(new DataColumn("VMajorRatio", typeof(double)));
                t.Columns.Add(new DataColumn("VrMajDsgn", typeof(double)));
                t.Columns.Add(new DataColumn("PhiVnMajor", typeof(double)));
                t.Columns.Add(new DataColumn("VnMajOmega", typeof(double)));
                t.Columns.Add(new DataColumn("TrMajor", typeof(double)));
                t.Columns.Add(new DataColumn("VMinorCombo", typeof(string)));
                t.Columns.Add(new DataColumn("VMinorLoc", typeof(double)));
                t.Columns.Add(new DataColumn("VMinorRatio", typeof(double)));
                t.Columns.Add(new DataColumn("VrMinDsgn", typeof(double)));
                t.Columns.Add(new DataColumn("PhiVnMinor", typeof(double)));
                t.Columns.Add(new DataColumn("VnMinOmega", typeof(double)));
                t.Columns.Add(new DataColumn("TrMinor", typeof(double)));
                t.Columns.Add(new DataColumn("DCLimit", typeof(double)));
                t.Columns.Add(new DataColumn("RLLF", typeof(double)));
                t.Columns.Add(new DataColumn("FramingType", typeof(string)));
                t.Columns.Add(new DataColumn("ErrMsg", typeof(string)));
                t.Columns.Add(new DataColumn("WarnMsg", typeof(string)));



                return t;
            }
        }
        public static DataTable TableFormat_SteelDesign9DecisionParametersAisc360_16
        {
            get
            {
                DataTable t = new DataTable("STEEL DESIGN 9 - DECISION PARAMETERS - AISC 360-16");

                t.Columns.Add(new DataColumn("Frame", typeof(string)));
                t.Columns.Add(new DataColumn("DesignSect", typeof(string)));
                t.Columns.Add(new DataColumn("AlphaPrOPy", typeof(double)));
                t.Columns.Add(new DataColumn("aPrOPyGT05", typeof(double)));
                t.Columns.Add(new DataColumn("AlphaPrOPeL", typeof(double)));
                t.Columns.Add(new DataColumn("aPrOPeGT015", typeof(double)));
                t.Columns.Add(new DataColumn("Taub", typeof(double)));
                t.Columns.Add(new DataColumn("EAmodifier", typeof(double)));
                t.Columns.Add(new DataColumn("EImodifier", typeof(double)));
                
                return t;
            }
        }
        
        public static string GetS2KTextFileTerminator => "END TABLE DATA";

        // Declares the Regexs
        private static readonly Regex s2kTableStartLineRegex = new Regex(@"^TABLE:\s*""(?<table_name>.*)""");
        private static readonly Regex s2kTableEndLineRegex = new Regex(@"^\s{2}$");
        private static readonly Regex s2kTableColumnRegex = new Regex(@"\s*(?<col>\w*)=(?<dat>(?("")"".+?""|[\(\),\w\.\-\+\*\~]*))");
        private static readonly string s2kLargeLineTerminator = $" _";

        public static DataSet ReadDataSetFromS2K([NotNull] string inFullFileName)
        {
            if (inFullFileName == null) throw new ArgumentNullException(nameof(inFullFileName));

            DataSet toRet = new DataSet();
            DataTable currentTable = null;

            string treatmentLine = string.Empty;
            Match debugMatch;

            try
            {
                // Reads the file line by line
                foreach (string line in File.ReadLines(inFullFileName))
                {
                    // Looking for a new table?
                    if (currentTable == null)
                    {
                        Match tableStartMatch = s2kTableStartLineRegex.Match(line);
                        if (tableStartMatch.Success) currentTable = TableFormat_GetFromStringName(tableStartMatch.Groups["table_name"].Value);
                        continue;
                    }
                    else
                    {
                        // Is it a _new_ empty line - this means that the table finished
                        if (currentTable != null && string.IsNullOrWhiteSpace(treatmentLine) && string.IsNullOrWhiteSpace(line))
                        {
                            toRet.Tables.Add(currentTable);
                            currentTable = null;
                            continue;
                        }

                        // Supposes this is a DATA line
                        treatmentLine += line;
                        if (treatmentLine.EndsWith(s2kLargeLineTerminator)) continue; // Must get the other lines

                        // We have the full line!
                        DataRow row = currentTable.NewRow();
                        int colsFound = 0;
                        // Executes lazily - fills the columns
                        foreach (Match colValPair in s2kTableColumnRegex.Matches(treatmentLine))
                        {
                            debugMatch = colValPair;
                            string val = colValPair.Groups["dat"].Value;

                            // Finds the column in the Table
                            DataColumn dataCol = currentTable.Columns[colValPair.Groups["col"].Value];
                            TypeCode colTypeCode = Type.GetTypeCode(dataCol.DataType);

                            // Assigns the typed value
                            switch (colTypeCode)
                            {
                                case TypeCode.Double:
                                    row[dataCol] = double.Parse(val);
                                    break;

                                case TypeCode.Int32:
                                    row[dataCol] = int.Parse(val);
                                    break;

                                case TypeCode.Boolean:
                                    row[dataCol] = (val != "No");
                                    break;

                                case TypeCode.String:
                                    // Deletes the ""
                                    row[dataCol] = val.Trim(new char[] { '"' });
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException($"Type {dataCol.DataType} - TypeCode {colTypeCode} is not supported.");
                            }

                            // Just a flag to see if we actually had successful regex matches
                            colsFound++;
                        }

                        // No match was found - the table finished (we read an empty line)
                        if (colsFound == 0)
                        {
                            toRet.Tables.Add(currentTable);
                            currentTable = null;
                            continue;
                        }

                        // Adds the row to the table
                        treatmentLine = string.Empty;
                        currentTable.Rows.Add(row);
                    }
                }

                // Returns the full DataSet found in the s2k
                return toRet;
            }
            catch (Exception e)
            {
                throw new S2KHelperException($"Error reading the s2k file {inFullFileName}.", e);
            }
        }
    }
}