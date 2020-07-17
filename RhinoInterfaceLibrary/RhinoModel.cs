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
using Grasshopper;

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

        private const string _crhinoInterfaceIdString = "Rhino.Interface";
        private static Type RhinoInstanceType => Type.GetTypeFromProgID(_crhinoInterfaceIdString);

        private const string _crhinoApplicationIdString = "Rhino.Application";
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

        public void Redraw()
        {
            _emsPluginReference.Redraw();
        }
        public void MakeSingleView()
        {
            _emsPluginReference.MakeSingleView();
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
        #endregion

    }
}
