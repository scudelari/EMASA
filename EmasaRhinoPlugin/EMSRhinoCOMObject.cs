using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Rhino.Commands;
using Rhino.Display;

namespace EmasaRhinoPlugin
{
    [ComVisible(true)]
    public class EMSRhinoCOMObject
    {
        public RhinoDoc ActiveDoc
        {
            get { return RhinoDoc.ActiveDoc; }
        }

        public int GetInteger()
        {
            return 24;
        }

        public double[] GetPoint()
        {
            var pt = new List<double> { 2.0, 1.0, 0.0 };
            return pt.ToArray();
        }

        public string AddPoint(double[] coords)
        {
            Point3d point = new Point3d(coords[0], coords[1], coords[2]);

            Guid object_id = ActiveDoc.Objects.AddPoint(point);

            if (!object_id.Equals(Guid.Empty))
            {
                ActiveDoc.Views.Redraw();
                return object_id.ToString();
            }

            return null;
        }
        
        public bool RunScript(string script, bool echo)
        {
            script = script.TrimStart('!');
            bool rc = RhinoApp.RunScript(script, echo);
            return rc;
        }
        
        public bool CloseRhino()
        {
            try
            {
                RhinoApp.Exit();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void Redraw()
        {
            foreach (var item in ActiveDoc.Views)
            {
                item.ActiveViewport.ZoomExtents();
            }
            ActiveDoc.Views.Redraw();
        }
        public void MakeSingleView()
        {
            ActiveDoc.Views.ActiveView.Maximized = true;
            ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtents();
            ActiveDoc.Views.ActiveView.ActiveViewport.SetViewProjection(new ViewportInfo(), false);
        }

        public string[] GetSelectedBrep()
        {
            List<string> Guids = new List<string>();

            foreach (RhinoObject obj in ActiveDoc.Objects.GetSelectedObjects(false,false))
            {
                if (obj.ObjectType == ObjectType.Brep)
                {
                    Guids.Add(obj.Id.ToString());
                }
            }

            return Guids.ToArray();
        }

        public double[] GetNormalAtSurface(double[] inPoint, string inSurfaceGroupName)
        {
            // Gets the selected Surface in the given group
            List<Brep> selBreps = GetBrepsInGroup(inSurfaceGroupName);
            if (selBreps.Count != 1) throw new InvalidOperationException($"The group {inSurfaceGroupName} does not contain one, and only one, Brep.");

            // Adds the point that will be projected to the Rhino Document
            Point3d inPnt = inPoint.COMConvert_FromArrayToPoint3d();
            Guid inPntGuid = ActiveDoc.Objects.AddPoint(inPnt);
            if (inPntGuid == Guid.Empty) throw new InvalidOperationException($"Could not add point {inPnt} to the Rhino Document.");


            try
            {
                Surface closestSurface = selBreps[0].Surfaces.OrderBy(a => { a.ClosestPoint(inPnt, out double u1, out double v1);
                                                                            Point3d closestPnt = a.PointAt(u1, v1);
                                                                            return closestPnt.DistanceTo(closestPnt);
                                                                        }).First();

                closestSurface.ClosestPoint(inPnt, out double u, out double v);

                //Point3d closestPoint = closestSurface.PointAt(u, v);
                //Guid inclosestPointGuid = ActiveDoc.Objects.AddPoint(closestPoint);
                //if (inclosestPointGuid == Guid.Empty) throw new InvalidOperationException($"Could not add point {inPnt} to the Rhino Document.");

                //Line line = new Line(inPnt, closestPoint);
                //Guid lineGuid = ActiveDoc.Objects.AddLine(line);
                //if (lineGuid == Guid.Empty) throw new InvalidOperationException($"Could not add the line {line} to the Rhino Document.");

                Vector3d normalAtClosest = closestSurface.NormalAt(u, v);

                return normalAtClosest.COMConvert_FromVector3dToArray();
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Could not find the closest point on the Brep.");
            }
        }
        private List<Surface> GetSelectedSurfaces()
        {
            return ActiveDoc.Objects.GetSelectedObjects(false, false).Select(a => a.ObjectType == ObjectType.Surface).Cast<Surface>().ToList();
        }
        private List<Brep> GetBrepsInGroup(string group)
        {
            Group grp = ActiveDoc.Groups.FindName(group);
            if (grp == null) throw new InvalidOperationException($"Could not find Rhino group called {group}.");

            List<Brep> list = ActiveDoc.Objects.FindByGroup(grp.Index).Cast<BrepObject>().Select(a => a.BrepGeometry).ToList();
            return list;
        }

        public void AddPointWithTriad(string pointName, double[] pointArray, double[] xVecArray, double[] yVecArray, double[] zVecArray, double size)
        {
            Point3d pnt = pointArray.COMConvert_FromArrayToPoint3d();
            Vector3d xVec = xVecArray.COMConvert_FromArrayToVector3d();
            xVec.Unitize();
            Vector3d yVec = yVecArray.COMConvert_FromArrayToVector3d();
            yVec.Unitize();
            Vector3d zVec = zVecArray.COMConvert_FromArrayToVector3d();
            zVec.Unitize();

            Guid inPntGuid = ActiveDoc.Objects.AddPoint(pnt, new ObjectAttributes() { Name = pointName, ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = Color.White });
            if (inPntGuid == Guid.Empty) throw new InvalidOperationException($"Could not add point {pointName} to the Rhino Document.");
            //PointObject rPnt = (PointObject)ActiveDoc.Objects.FindId(inPntGuid);

            Point3d xPnt = pnt + (xVec*size);
            Guid xLineGuid = ActiveDoc.Objects.AddLine(pnt, xPnt, new ObjectAttributes { Name = $"{pointName}_Dir1", ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = Color.Red });
            if (xLineGuid == Guid.Empty) throw new InvalidOperationException($"Could not add axis 1 line of {pointName} to the Rhino Document.");

            Point3d yPnt = pnt + (yVec*size);
            Guid yLineGuid = ActiveDoc.Objects.AddLine(pnt, yPnt, new ObjectAttributes { Name = $"{pointName}_Dir2", ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = Color.Green });
            if (yLineGuid == Guid.Empty) throw new InvalidOperationException($"Could not add axis 2 line of {pointName} to the Rhino Document.");

            Point3d zPnt = pnt + (zVec*size);
            Guid zLineGuid = ActiveDoc.Objects.AddLine(pnt, zPnt, new ObjectAttributes { Name = $"{pointName}_Dir3", ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = Color.Blue });
            if (zLineGuid == Guid.Empty) throw new InvalidOperationException($"Could not add axis 3 line of {pointName} to the Rhino Document.");
        }

        public void AddIdToGroup(string strGuid, string groupname, int? changeObjectColourArgb = null)
        {
            Color? changeObjectColour = null;
            if (changeObjectColourArgb.HasValue) changeObjectColour = Color.FromArgb(changeObjectColourArgb.Value);

            Group grp = ActiveDoc.Groups.FindName(groupname);
            
            if (grp == null)
            { // Creates the group
                ActiveDoc.Groups.Add(groupname);
                grp = ActiveDoc.Groups.FindName(groupname);
                if (grp == null) throw new InvalidOperationException($"Could not get group called {groupname}. Nor could a new one be added with this name.");
            }

            Guid objGuid = new Guid(strGuid);
            RhinoObject obj = ActiveDoc.Objects.FindId(objGuid);
            if (obj == null) throw new InvalidOperationException($"Could not find the object with Guid {strGuid}.");

            ActiveDoc.Groups.AddToGroup(grp.Index, objGuid);

            if (changeObjectColour.HasValue)
            {
                obj.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = changeObjectColour.Value;
                obj.CommitChanges();
            }
        }
        public string[] GetGuidsByName(string elementName, uint typeFilter = 0)
        {
            if (typeFilter == 0)
            {
                // Filter it by the given values
                return (from a in ActiveDoc.Objects
                        where !string.IsNullOrEmpty(a.Name) && a.Name == elementName 
                        select a.Id.ToString()).ToArray();
            }

            RhinoObjectType enumType = (RhinoObjectType)typeFilter;

            // Filter it by the given values
            return (from a in ActiveDoc.Objects
                    where !string.IsNullOrEmpty(a.Name) && a.Name == elementName &&
                        (enumType & (RhinoObjectType)(uint)a.ObjectType) == (RhinoObjectType)(uint)a.ObjectType
                    select a.Id.ToString()).ToArray();

        }

        #region Grasshopper
        public string GetActiveGrasshopperDocumentFullPath()
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");
            
            if (!gh.IsEditorLoaded()) return null;

            // The document has not been saved or anything else
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return null;

            return Grasshopper.Instances.ActiveCanvas.Document.FilePath;
        }
        #endregion

        #region ImageGeneration
        public string SaveScreenShot(string inFullFilename, int inViewNumber)
        {
            // Check the possible results
            RhinoView[] views = ActiveDoc.Views.GetViewList(true, false);
            if (inViewNumber > views.Count()) return "The Rhino view index is out or range.";

            try
            {
                // Makes the selected view active
                if (inViewNumber != -1) ActiveDoc.Views.ActiveView = views[inViewNumber];

                // Sends the selected view to the clipboard
                bool r = RhinoApp.RunScript($"_-ScreenCaptureToFile \"{inFullFilename}\" _Enter", true);

                return null;
            }
            catch (Exception e)
            {
                return $"Exception: {e.Message}{Environment.NewLine}{e.StackTrace}";
            }
        }

        public void SetActiveViewPort(int inViewPortNumber)
        {
            // Check the possible results
            RhinoView[] views = ActiveDoc.Views.GetViewList(true, false);
            if (inViewPortNumber > views.Count()) throw new Exception("The Rhino view index is out or range.");

            if (inViewPortNumber != -1) ActiveDoc.Views.ActiveView = views[inViewPortNumber];
        }
        #endregion
    }
}
