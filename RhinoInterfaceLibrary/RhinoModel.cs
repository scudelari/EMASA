extern alias r3dm;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using System.Runtime.InteropServices;
using System.Threading;
using MN = MathNet.Spatial.Euclidean;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Grasshopper;

using R3dmGeom = r3dm::Rhino.Geometry;

namespace RhinoInterfaceLibrary
{
    public sealed class RhinoModel : IDisposable
    {
        private static RhinoModel _instance;
        private static readonly object _lockThis = new object();

        public static RhinoModel RM
        {
            get
            {
                lock (_lockThis)
                {
                    if (_instance == null) throw new Exception("The global singleton instance of Rhino must be initialized using the Initialize static method.");
                }

                return _instance;
            }
        }
        public static void Initialize(bool inNewInstance = false, bool inCloseOnRelease = false)
        {
            lock (_lockThis)
            {
                _instance = new RhinoModel(inNewInstance, inCloseOnRelease);
            }
        }

        public RhinoModel(bool inNewInstance = false, bool inCloseOnRelease = false)
        {
            if (inNewInstance)
            {
                // The application always starts a new instance
                _rhinoApp = Activator.CreateInstance(RhinoApplicationType);

                // Waits for Rhino to become responsive
                const int bail_milliseconds = 30 * 1000;
                var time_waiting = 0;
                while (0 == _rhinoApp.IsInitialized())
                {
                    Thread.Sleep(100);
                    time_waiting += 100;
                    if (time_waiting > bail_milliseconds)
                    {
                        throw new Exception("Could not open a new instance of Rhino.");
                    }
                }

                if (!IsRhinoOk) throw new Exception("The new instance of Rhino is not responsive.");
            }
            else
            {
                // Checks if there is a running instance of rhino
                Process[] rhinos = Process.GetProcessesByName("Rhino");
                if (rhinos.Length == 0) throw new Exception("There are no running instances of Rhino.");
                if (rhinos.Length > 1) throw new Exception("There can only be one running instance of Rhino.");

                // The instance will link to an existing; otherwise starts a new instance
                _rhinoApp = Activator.CreateInstance(RhinoInstanceType);

                if (!IsRhinoOk) throw new Exception("The linked instance of Rhino is not responsive.");
            }

            this.RhinoVisible = true;

            if (!SendRhinoCommand("_-EMSBogusCommand", 0)) // Failed
            {
                throw new Exception("You must have the EMSInterfaceRhinoPlugin installed so that Rhino exposes the COM functions.");
            }

            // Guid is from SampleCsRhino assembly
            _emsPluginReference = _rhinoApp.GetPlugInObject(_cemsPluginGuid, _cemsPluginGuid);

            CloseOnRelease = inCloseOnRelease;
        }

        private dynamic _rhinoApp = null;

        private const string _crhinoInterfaceIdString = "Rhino.Interface.6";
        private static Type RhinoInstanceType => Type.GetTypeFromProgID(_crhinoInterfaceIdString);

        private const string _crhinoApplicationIdString = "Rhino.Application.6";
        private static Type RhinoApplicationType => Type.GetTypeFromProgID(_crhinoApplicationIdString);

        private const string _cgrassHopperPluginId = "b45a29b1-4343-4035-989e-044e8580d9cf";
        private const string _cgrassHopperInterfaceId = "00020400-0000-0000-C000-000000000046";

        private readonly dynamic _emsPluginReference = null;
        // It is the GUID that is set in the Visual Studio Project
        private const string _cemsPluginGuid = "e3171621-daad-4a6b-a627-f3297bda12c2";

        public bool CloseOnRelease = false;

        public bool RhinoVisible
        {
            get
            {
                if (!IsRhinoOk) throw new InvalidOperationException("There is no Rhino instance running.");
                return _rhinoApp.Visible == 1;
            }
            set
            {
                if (!IsRhinoOk) throw new InvalidOperationException("There is no Rhino instance running.");
                _rhinoApp.Visible = value ? 1 : 0;
            }
        }

        public string RhinoActiveDocumentFullFileName => _emsPluginReference.GetActiveDocumentFullFileName();
        public void SaveAsActiveRhinoDocument(string inDocFullFileName)
        {
            if (_emsPluginReference.SaveActiveDocumentAs(inDocFullFileName) == false) throw new Exception($"Could not save the active Rhino document as {inDocFullFileName}.");
        }
        public void OpenRhinoDocument(string inFullPath)
        {
            if (!_emsPluginReference.OpenDocument(inFullPath)) throw new Exception($"Could not open the Rhino document {inFullPath}.");
        }
        
        public bool SendRhinoCommand(string inCommand, int inEcho = 0)
        {
            if (!IsRhinoOk) throw new InvalidOperationException("There is no Rhino instance running.");

            try
            {
                // Sends the inCommand to Rhino
                int ret = _rhinoApp.RunScript(inCommand, 0);
                if (ret == 0) throw new ExternalException($"The inCommand {inCommand} failed in Rhino");
                return ret == 1;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool IsRhinoOk
        {
            get
            {
                if (_rhinoApp == null) return false;
                if (_rhinoApp.IsInitialized() == 0) return false;
                return true;
            }
        }

        #region Grasshopper

        private dynamic _comGrasshopper = null;
        public bool IsGrasshopperOk
        {
            get
            {
                if (_comGrasshopper == null)
                {
                    // It is possible that it has already been loaded automatically when opening Rhino.
                    // In this case, tries to get the plugin instance.
                    dynamic comGrasshopper = _rhinoApp.GetPlugInObject(_cgrassHopperPluginId, _cgrassHopperInterfaceId);

                    if (comGrasshopper.IsEditorLoaded())
                    {
                        _comGrasshopper = comGrasshopper;
                    }
                    else return false;
                }
                if (!_comGrasshopper.IsEditorLoaded()) return false;
                return true;
            }
        }
        public bool GrasshopperVisible
        {
            get
            {

                if (!IsRhinoOk) throw new InvalidOperationException("There is no Rhino instance running.");
                return _rhinoApp.Visible;
            }
            set
            {
                if (this._comGrasshopper != null)
                    if (value)
                    {
                        this._comGrasshopper.ShowEditor();
                    }
                    else
                    {
                        this._comGrasshopper.HideEditor();
                    }

            }
        }

        private dynamic _comOtherGH = null;

        public void ReleaseGrasshopper()
        {
            if (this._comGrasshopper != null)
            {
                try
                {
                    Marshal.ReleaseComObject(_comGrasshopper);
                    //Marshal.FinalReleaseComObject(_comGrasshopper);
                }
                catch (Exception)
                {
                }
                _comGrasshopper = null;
            }
        }
        public bool OpenGrasshopperDocument(string inFullFileName)
        {
            if (!IsRhinoOk) throw new InvalidOperationException("There is no Rhino running.");
            if (!IsGrasshopperOk) throw new InvalidOperationException("There is no Grasshopper running.");

            if (!_comGrasshopper.CloseAllDocuments()) throw new Exception($"Could not close all currently open Grasshopper documents.");

            // Opens the Grasshopper document
            bool openDocResult = _comGrasshopper.OpenDocument(inFullFileName);

            // No need as it will poool GH everytime
            //if (openDocResult) GrasshopperFullFileName = inFullFileName;
            return openDocResult;
        }
        public void SolveGrasshopper()
        {
            if (!IsGrasshopperOk) throw new InvalidOperationException("There is no Grasshopper running.");

            // Updates the Grasshopper Solution
            _comGrasshopper.RunSolver(true);
        }
        #endregion

        #region Helpers for the Grasshopper Optimizations
        public string GrasshopperFullFileName
        {
            get
            {
                if (!IsGrasshopperOk) throw new InvalidOperationException("Can't get the name of the active Grasshopper file because Grasshopper is not OK.");

                string ghName = _emsPluginReference.GetActiveGrasshopperDocumentFullPath();
                if (string.IsNullOrWhiteSpace(ghName)) throw new InvalidOperationException("Can't get the name of the active Grasshopper file. Please check if there is an open file and if the file is saved.");
                return ghName;
            }
        }

        public string GrasshopperDescription
        {
            get
            {
                if (!IsGrasshopperOk) throw new InvalidOperationException("Can't get the description of the active Grasshopper file because Grasshopper is not OK.");

                string ghName = _emsPluginReference.GetActiveGrasshopperDocumentDescription();
                if (ghName == null) throw new InvalidOperationException("Can't get the description of the active Grasshopper file. Please check if there is an open file and if the file is saved.");
                return ghName;
            }
        }
        #endregion

        public void Dispose()
        {
            if (this._comGrasshopper != null)
            {
                this.ReleaseGrasshopper();
            }

            if (CloseOnRelease)
            {
                // Should close
                _rhinoApp.ReleaseWithoutClosing = 0;
                // Is exit necessary?
                //SendRhinoCommand("_Exit", 1);
                Marshal.ReleaseComObject(_rhinoApp);
                _rhinoApp = null;
            }
            else
            {
                _rhinoApp.ReleaseWithoutClosing = 1;
                Marshal.ReleaseComObject(_rhinoApp);
                _rhinoApp = null;
            }
        }

        public static void DisposeAll()
        {
            if (_instance != null) RM.Dispose();
            _instance = null;
        }

        #region Rhino functions through the CUSTOM COM PLUG-IN

        public int GetInt()
        {
            return (int)_emsPluginReference.GetInteger();
        }
        public Guid AddPoint(Point3d inPoint)
        {
            string pointGuid = (string)_emsPluginReference.AddPoint(new double[] { inPoint.X, inPoint.Y, inPoint.Z });

            return Guid.Parse(pointGuid);
        }
        public Guid AddPoint(string inPointName, Point3d inPoint)
        {
            string pointGuid = (string)_emsPluginReference.AddPoint(inPointName, new double[] { inPoint.X, inPoint.Y, inPoint.Z });

            return Guid.Parse(pointGuid);
        }
        public Guid AddPoint3DWithName(string inPointName, MN.Point3D inPoint)
        {
            string pointGuid = (string)_emsPluginReference.AddPointWithName(inPointName, new double[] { inPoint.X, inPoint.Y, inPoint.Z });

            return Guid.Parse(pointGuid);
        }

        public MN.Vector3D GetNormalAtSurface(MN.Point3D inPoint, string RhinoGroupName)
        {
            double[] vecCoords = (double[])_emsPluginReference.GetNormalAtSurface(new double[] { inPoint.X, inPoint.Y, inPoint.Z }, RhinoGroupName);

            return new MN.Vector3D(vecCoords[0], vecCoords[1], vecCoords[2]);
        }

        public void AddPointWithTriad(string pointName, MN.Point3D point, MN.CoordinateSystem pointCsys, double size)
        {
            double[] pointArray = new double[] { point.X, point.Y, point.Z };

            double[] xVecArray = new double[] { pointCsys.XAxis.X, pointCsys.XAxis.Y, pointCsys.XAxis.Z };
            double[] yVecArray = new double[] { pointCsys.YAxis.X, pointCsys.YAxis.Y, pointCsys.YAxis.Z };
            double[] zVecArray = new double[] { pointCsys.ZAxis.X, pointCsys.ZAxis.Y, pointCsys.ZAxis.Z };

            _emsPluginReference.AddPointWithTriad(pointName, pointArray, xVecArray, yVecArray, zVecArray, size);
        }
        public void AddIdToGroup(string strGuid, string groupname, Color? changeObjectColour = null)
        {
            int? colorParam = null;
            if (changeObjectColour.HasValue) colorParam = changeObjectColour.Value.ToArgb();

            _emsPluginReference.AddIdToGroup(strGuid, groupname, colorParam);
        }
        public string[] GetGuidsByName(string elementName, RhinoObjectType typeFilter = RhinoObjectType.None)
        {
            return _emsPluginReference.GetGuidsByName(elementName, (uint)typeFilter);
        }

        public MN.Point3D[] GetPointListAlongArc(MN.Point3D startJoint, MN.Point3D midJoint, MN.Point3D endJoint, int numberSegments)
        {
            throw new NotImplementedException();
        }

        public void AddPointWithGroupAndColor(string inPointName, MN.Point3D inPointLocation, int inGroupId, Color inColor)
        {
            double[] pointArray = { inPointLocation.X, inPointLocation.Y, inPointLocation.Z };
            int[] rgb = {inColor.R, inColor.G, inColor.B};

            _emsPluginReference.AddPointWithGroupAndColor(inPointName, pointArray, inGroupId, rgb);
        }
        public int AddGroupIfNew(string inGroupName)
        {
            return _emsPluginReference.AddGroupIfNew(inGroupName);
        }
        public void AddLineWithGroupAndColor(string inName, MN.Point3D inStart, MN.Point3D inEnd, int inGroupId, Color inColor)
        {
            double[] startArray = { inStart.X, inStart.Y, inStart.Z };
            double[] endArray = { inEnd.X, inEnd.Y, inEnd.Z };
            int[] rgb = { inColor.R, inColor.G, inColor.B };

            _emsPluginReference.AddLineWithGroupAndColor(inName, startArray, endArray, inGroupId, rgb);
        }
        public void AddSphereWithGroupAndColor(string inName, MN.Point3D inCenter, int inGroupId, Color inColor, double inRadius)
        {
            double[] centerArray = { inCenter.X, inCenter.Y, inCenter.Z };
            int[] rgb = { inColor.R, inColor.G, inColor.B };

            _emsPluginReference.AddSphereWithGroupAndColor(inName, centerArray, inGroupId, rgb, inRadius);
        }
        public void AddSpheresInterpolatedColor(string[] inNames, List<MN.Point3D> inCenters, int[] inGroupIds, Color inStartColor, Color inEndColor, double inRadius)
        {
            double[,] centers = new double[inCenters.Count,3];
            for (int i = 0; i < inCenters.Count; i++)
            {
                MN.Point3D pnt = inCenters[i];

                centers[i, 0] = pnt.X;
                centers[i, 1] = pnt.Y;
                centers[i, 2] = pnt.Z;
            }

            int[] startRgb = { inStartColor.R, inStartColor.G, inStartColor.B };
            int[] endRgb = { inEndColor.R, inEndColor.G, inEndColor.B };

            // public void AddSpheresInterpolatedColor(string[] inNames, double[,] inCenters, int[] inGroupIds, int[] startColor, int[] endColor, double inRadius)
            _emsPluginReference.AddSpheresInterpolatedColor(inNames,
                centers,
                inGroupIds,
                startRgb,
                endRgb,
                inRadius);
        }

        public void ChangePropertiesOfObjectInGroup(string inGroupName, Color? inColor = null, int inLineTypeIndex = -1)
        {
            int[] rgb = null;
            if (inColor.HasValue) rgb = new int[] { inColor.Value.R, inColor.Value.G, inColor.Value.B };

            _emsPluginReference.ChangePropertiesOfObjectInGroup(inGroupName, rgb, inLineTypeIndex);
        }

        public string[,] Grasshopper_GetDocumentMessages()
        {
            return _emsPluginReference.Grasshopper_GetDocumentMessages();
        }

        #endregion

        #region Managing Communication With EMASA Components
        public GrasshopperAllEmasaOutputWrapper_AsRhino3dm Grasshopper_GetAllEmasaOutputs()
        {
            // Requests the Json from COM
            byte[] json = _emsPluginReference.Grasshopper_GetAllEmasaOutputs_JSON();
            if (json == null) return null;
            if (json.Length == 0) return new GrasshopperAllEmasaOutputWrapper_AsRhino3dm();

            // JSON Serializer
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GrasshopperAllEmasaOutputWrapper_AsRhino3dm), new DataContractJsonSerializerSettings()
                {
                UseSimpleDictionaryFormat = true
                });

            GrasshopperAllEmasaOutputWrapper_AsRhino3dm toret = null;
            try
            {
                using (MemoryStream stream = new MemoryStream(json))
                {
                    toret = (GrasshopperAllEmasaOutputWrapper_AsRhino3dm)ser.ReadObject(stream);
                }
            }
            catch {}

            return toret;
        }
        public GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm Grasshopper_GetAllEmasaInputDefs()
        {
            // Requests the Json from COM
            byte[] json = _emsPluginReference.Grasshopper_GetAllEmasaInputDefs_JSON();
            if (json == null) return null;
            if (json.Length == 0) return new GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm();

            // JSON Serializer
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm), new DataContractJsonSerializerSettings()
                {
                UseSimpleDictionaryFormat = true
                });

            GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm toret = null;
            try
            {
                using (MemoryStream stream = new MemoryStream(json))
                {
                    toret = (GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm)ser.ReadObject(stream);
                }
            }
            catch { }

            return toret;
        }

        public bool Grasshopper_UpdateEmasaInputs(Dictionary<string, object> inValuePairs, bool inRecompute = false)
        {
            // Builds the expected format
            string[] names = new string[inValuePairs.Count];
            double[,] values = new double[inValuePairs.Count, 3];

            int i = 0;
            foreach (KeyValuePair<string, object> pair in inValuePairs)
            {
                names[i] = pair.Key;

                switch (pair.Value)
                {
                    case double d:
                        values[i, 0] = d;
                        break;
                    case int integer:
                        values[i, 0] = (double)integer;
                        break;
                    case R3dmGeom.Point3d r3dmPoint:
                        values[i, 0] = r3dmPoint.X;
                        values[i, 1] = r3dmPoint.Y;
                        values[i, 2] = r3dmPoint.Z;
                        break;
                    default:
                        throw new InvalidOperationException($"The type {pair.Key.GetType()} of the value {pair.Value} in key {pair.Value} is not supported.");
                }
                i++;
            }

            return _emsPluginReference.Grasshopper_UpdateEmasaInputs(names, values, inRecompute);
        }

        public bool Grasshopper_UpdateEmasaInput_Integer(string inParamName, int inValue, bool inRecompute = false)
        {
            return _emsPluginReference.Grasshopper_UpdateEmasaInput_Integer(inParamName, inValue, inRecompute);
        }
        public bool Grasshopper_UpdateEmasaInput_Double(string inParamName, double inValue, bool inRecompute = false)
        {
            return _emsPluginReference.Grasshopper_UpdateEmasaInput_Double(inParamName, inValue, inRecompute);
        }
        public bool Grasshopper_UpdateEmasaInput_Point(string inParamName, R3dmGeom.Point3d inValue, bool inRecompute = false)
        {
            return _emsPluginReference.Grasshopper_UpdateEmasaInput_Point(inParamName, new double[] { inValue.X, inValue.Y, inValue.Z}, inRecompute);
        }
        #endregion

        #region ScreenShot Related COM Wrappers
        public void PrepareRhinoViewForImageAcquire()
        {
            if (!_emsPluginReference.PrepareRhinoViewForImageAcquire()) throw new Exception($"Failed while preparing Rhino for image acquisition.");
        }
        public List<(string dir, Image image)> GetScreenshots(string[] inDirections)
        {
            if (inDirections == null || inDirections.Length == 0) throw new Exception($"{nameof(inDirections)} must contain at least one value.");

            string xmlData = _emsPluginReference.GetScreenshotsInXmlFormat(inDirections);

            if (xmlData == null) throw new Exception("Failed when getting Rhino's screenshots.");

            List<(string, byte[])> toRetTempBytes;
            
            // Deserializes
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlData)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<(string, byte[])>));
                toRetTempBytes = serializer.Deserialize(ms) as List<(string, byte[])>;
            }

            return toRetTempBytes.Select((inTuple, inI) =>
            {
                Image img;
                using (MemoryStream ms = new MemoryStream(inTuple.Item2))
                {
                    img = Image.FromStream(ms);
                }
                return (inTuple.Item1, img);
            }).ToList();
        }
        public void RestoreRhinoViewFromImageAcquire()
        {
            if (!_emsPluginReference.RestoreRhinoViewFromImageAcquire()) throw new Exception($"Failed while restoring default Rhino from image acquisition.");
        }
        #endregion
    }

    [DataContract]
    public class GrasshopperAllEmasaOutputWrapper_AsRhino3dm
    {
        [DataMember]
        public Dictionary<string, List<double>> DoubleLists { get; set; } = new Dictionary<string, List<double>>();
        [DataMember]
        public Dictionary<string, List<R3dmGeom.Point3d>> PointLists { get; set; } = new Dictionary<string, List<R3dmGeom.Point3d>>();
        [DataMember]
        public Dictionary<string, List<R3dmGeom.Line>> LineLists { get; set; } = new Dictionary<string, List<R3dmGeom.Line>>();
    }
    [DataContract]
    public class GrasshopperAllEmasaInputDefsWrapper_AsRhino3dm
    {
        [DataMember]
        public List<string> IntegerInputs { get; set; } = new List<string>();

        [DataMember]
        public Dictionary<string, Tuple<double, double, double>> DoubleInputs { get; set; } = new Dictionary<string, Tuple<double, double, double>>();

        [DataMember]
        public Dictionary<string, Tuple<R3dmGeom.Point3d, R3dmGeom.Point3d, R3dmGeom.Point3d>> PointInputs { get; set; } = new Dictionary<string, Tuple<R3dmGeom.Point3d, R3dmGeom.Point3d, R3dmGeom.Point3d>>();
    }
}
