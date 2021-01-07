using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Eto.Threading;
using GHComponents;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Types;
using Rhino.Commands;
using Rhino.Display;
using Rhino.FileIO;

namespace EmasaRhinoPlugin
{
    [ComVisible(true)]
    public class EMSRhinoCOMObject
    {
        private RhinoDoc ActiveDoc
        {
            get { return RhinoDoc.ActiveDoc; }
        }

        #region Rhino File Handling

        public string GetActiveDocumentFullFileName()
        {
            return ActiveDoc.Path;
        }
        public bool SaveActiveDocumentAs(string inFullPath)
        {
            return ActiveDoc.WriteFile(inFullPath, new FileWriteOptions() {UpdateDocumentPath = true, SuppressDialogBoxes = true});
        }
        public bool OpenDocument(string inFullPath)
        {
            RhinoDoc doc = null;
            try
            {
                bool bla = false;
                doc = RhinoDoc.Open(inFullPath, out bla);
            }
            catch (Exception ex)
            {
                return false;
            }
            return doc != null;

        }

        #endregion

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
        public string AddPointWithName(string inPointName, double[] coords)
        {
            Point3d pnt = new Point3d(coords[0], coords[1], coords[2]);

            Guid inPntGuid = ActiveDoc.Objects.AddPoint(pnt, new ObjectAttributes() { Name = inPointName, ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = Color.White });
            if (inPntGuid == Guid.Empty) throw new InvalidOperationException($"Could not add point {inPointName} to the Rhino Document.");

            if (!inPntGuid.Equals(Guid.Empty))
            {
                ActiveDoc.Views.Redraw();
                return inPntGuid.ToString();
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

        public int AddGroupIfNew(string inGroupName)
        {
            Group grp = ActiveDoc.Groups.FindName(inGroupName);

            if (grp == null)
            { // Creates the group
                ActiveDoc.Groups.Add(inGroupName);
                grp = ActiveDoc.Groups.FindName(inGroupName);
                if (grp == null) throw new InvalidOperationException($"Could not get group called {inGroupName}. Nor could a new one be added with this name.");
            }

            return grp.Index;
        }
        public void AddPointWithGroupAndColor(string inName, double[] inPointArray, int inGroupId, int[] inRGB)
        {
            Point3d p = new Point3d(inPointArray[0], inPointArray[1], inPointArray[2]);

            ObjectAttributes a = new ObjectAttributes();
            a.Name = inName;
            a.ColorSource = ObjectColorSource.ColorFromObject;
            a.ObjectColor = Color.FromArgb(255, inRGB[0], inRGB[1], inRGB[2]);

            Guid pntId = ActiveDoc.Objects.AddPoint(p, a);

            ActiveDoc.Groups.AddToGroup(inGroupId, pntId);
        }
        public void AddLineWithGroupAndColor(string inName, double[] inStart, double[] inEnd, int inGroupId, int[] inRGB)
        {
            Point3d start = new Point3d(inStart[0], inStart[1], inStart[2]);
            Point3d end = new Point3d(inEnd[0], inEnd[1], inEnd[2]);

            Line l = new Line(start, end);

            ObjectAttributes a = new ObjectAttributes();
            a.Name = inName;
            a.ColorSource = ObjectColorSource.ColorFromObject;
            a.ObjectColor = Color.FromArgb(255, inRGB[0], inRGB[1], inRGB[2]);

            Guid lineGuid = ActiveDoc.Objects.AddLine(l, a);

            ActiveDoc.Groups.AddToGroup(inGroupId, lineGuid);
        }
        public void AddSphereWithGroupAndColor(string inName, double[] inCenter, int inGroupId, int[] inRGB, double inRadius)
        {
            Point3d center = new Point3d(inCenter[0], inCenter[1], inCenter[2]);

            Sphere s = new Sphere(center, inRadius);

            ObjectAttributes a = new ObjectAttributes();
            a.Name = inName;
            a.ColorSource = ObjectColorSource.ColorFromObject;
            a.ObjectColor = Color.FromArgb(255, inRGB[0], inRGB[1], inRGB[2]);

            Guid sphereGuid = ActiveDoc.Objects.AddSphere(s, a);

            ActiveDoc.Groups.AddToGroup(inGroupId, sphereGuid);
        }
        public void AddSpheresInterpolatedColor(string[] inNames, double[,] inCenters, int[] inGroupIds, int[] startColor, int[] endColor, double inRadius)
        {
            if (inNames.Length != inCenters.GetLength(0)) throw new ArgumentException("The size of the name and center array must be the same.");
            if (inGroupIds.Length != 1 && (inGroupIds.Length != inNames.Length)) throw new ArgumentException("You must either give only one group for all spheres; or one group per sphere.");

            int elementCount = inNames.Length;

            for (int i = 0; i < inNames.Length; i++)
            {
                int grp = inGroupIds.Length == 1 ? inGroupIds[0] : inGroupIds[i];
                int[] elemColor = LerpRGB_Array(startColor, endColor, (i + 1d) / inNames.Length);
                double[] center = {inCenters[i, 0], inCenters[i, 1], inCenters[i, 2]};

                AddSphereWithGroupAndColor(inNames[i], center, grp, elemColor, inRadius);
            }
        }
        private Color LerpRGB(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }
        private int[] LerpRGB_Array(int[] a, int[] b, double t)
        {
            return new[] {
                (int) (a[0] + (b[0] - a[0]) * t),
                (int) (a[1] + (b[1] - a[1]) * t),
                (int) (a[2] + (b[2] - a[2]) * t)};
        }

        public void ChangePropertiesOfObjectInGroup(string inGroupName, int[] inColor = null, int inLineTypeIndex = -1)
        {
            // Finds the group
            Group grp = ActiveDoc.Groups.FindName(inGroupName);

            foreach (RhinoObject groupMember in ActiveDoc.Groups.GroupMembers(grp.Index))
            {
                if (inColor != null)
                {
                    groupMember.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
                    groupMember.Attributes.ObjectColor = Color.FromArgb(255, inColor[0], inColor[1], inColor[2]);

                }

                if (inLineTypeIndex >= 0)
                {
                    groupMember.Attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
                    groupMember.Attributes.LinetypeIndex = inLineTypeIndex;
                }
            }
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
        public string GetActiveGrasshopperDocumentDescription()
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");

            if (!gh.IsEditorLoaded()) return null;

            // The document has not been saved or anything else
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return null;

            return Grasshopper.Instances.ActiveCanvas.Document.Properties.Description;
        }
        public string[,] Grasshopper_GetDocumentMessages()
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");

            if (!gh.IsEditorLoaded()) return new string[1,2] { {"Error", "Grasshopper editor is nor loaded."}};

            // The document has not been saved or anything else
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return new string[,] { { "Error", "The document has not been saved." } };

            // Gets the messages from the document's ActiveObjects - which includes components
            List<IGH_ActiveObject> activeObjects = Grasshopper.Instances.ActiveCanvas.Document.ActiveObjects(); // Includes Params and Components
            
            List<(string,string)> listOfErrors = new List<(string, string)>();
            foreach (IGH_ActiveObject gh_ActiveObject in activeObjects)
            {
                if (gh_ActiveObject.RuntimeMessageLevel == GH_RuntimeMessageLevel.Blank) continue;

                // Gets the error messages
                foreach (string message in gh_ActiveObject.RuntimeMessages(GH_RuntimeMessageLevel.Error))
                {
                    listOfErrors.Add(("Error", $"{gh_ActiveObject.NickName}: {message}"));
                }

                // Gets the error warning
                foreach (string message in gh_ActiveObject.RuntimeMessages(GH_RuntimeMessageLevel.Warning))
                {
                    listOfErrors.Add(("Warning", $"{gh_ActiveObject.NickName}: {message}"));
                }

                // Gets the error remark
                foreach (string message in gh_ActiveObject.RuntimeMessages(GH_RuntimeMessageLevel.Remark))
                {
                    listOfErrors.Add(("Remark", $"{gh_ActiveObject.NickName}: {message}"));
                }
            }

            if (listOfErrors.Count == 0) return null;
             
            // Establishes the return array
            string[,] toRet = new string[listOfErrors.Count, 2];
            for (int i = 0; i < listOfErrors.Count; i++)
            {
                (string, string) val = listOfErrors[i];
                toRet[i, 0] = val.Item1;
                toRet[i, 1] = val.Item2;
            }

            return toRet; 
        }





        #endregion

        #region Managing Communication With EMASA Components
        public byte[] Grasshopper_GetAllEmasaOutputs_JSON()
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");

            if (!gh.IsEditorLoaded()) return null;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return null;

            // Gets the custom output parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            IEnumerable<IGH_DocumentObject> emsOutputs = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include)
                .Where(a => a is GHEmsOutput_DoubleParameter || a is GHEmsOutput_LineParameter || a is GHEmsOutput_PointParameter);

            // Declares the wrapper that will be used for serialization
            GrasshopperAllEmasaOutputWrapper wrapper = new GrasshopperAllEmasaOutputWrapper();

            // Fills the wrapper
            foreach (IGH_DocumentObject gh_DocumentObject in emsOutputs)
            {
                switch (gh_DocumentObject)
                {
                    case GHEmsOutput_DoubleParameter ghFileOutputDoubleParameter:
                        wrapper.DoubleLists.Add(ghFileOutputDoubleParameter.VariableName, ghFileOutputDoubleParameter.Doubles);
                        break;

                    case GHEmsOutput_LineParameter ghFileOutputLineParameter:
                        wrapper.LineLists.Add(ghFileOutputLineParameter.VariableName, ghFileOutputLineParameter.Lines);
                        break;

                    case GHEmsOutput_PointParameter ghFileOutputPointParameter:
                        wrapper.PointLists.Add(ghFileOutputPointParameter.VariableName, ghFileOutputPointParameter.Points);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(gh_DocumentObject), "Unexpected Grasshopper parameter type.");
                }
            }

            try
            {
                // JSON Serializer
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GrasshopperAllEmasaOutputWrapper), new DataContractJsonSerializerSettings()
                    {
                    UseSimpleDictionaryFormat = true
                    });

                byte[] toRet = null;
                using (MemoryStream stream1 = new MemoryStream())
                {
                    // Writes the serialized wrapper
                    ser.WriteObject(stream1, wrapper);
                    toRet = stream1.ToArray();
                }

                return toRet;
            }
            catch {}

            return null;
        }
        public byte[] Grasshopper_GetAllEmasaInputDefs_JSON()
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");

            if (!gh.IsEditorLoaded()) return null;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return null;

            // Gets the custom input parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            IEnumerable<IGH_DocumentObject> emsInputs = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include)
                .Where(a => a is GHEmsInput_IntegerParameter || a is GHEmsInput_DoubleParameter || a is GHEmsInput_PointParameter);

            // Declares the serialization wrapper
            GrasshopperAllEmasaInputDefsWrapper wrapper = new GrasshopperAllEmasaInputDefsWrapper();

            foreach (IGH_DocumentObject gh_DocumentObject in emsInputs)
            {
                switch (gh_DocumentObject)
                {
                    case GHEmsInput_IntegerParameter ghFileInputIntegerParameter:
                        wrapper.IntegerInputs.Add(ghFileInputIntegerParameter.VariableName);
                        break;

                    case GHEmsInput_PointParameter ghFileInputPointParameter:
                        wrapper.PointInputs.Add(ghFileInputPointParameter.VariableName, new Tuple<Point3d, Point3d, Point3d>(ghFileInputPointParameter.CurrentValue, ghFileInputPointParameter.LowerBound, ghFileInputPointParameter.UpperBound));
                        break;

                    case GHEmsInput_DoubleParameter ghFileInputDoubleParameter:
                        wrapper.DoubleInputs.Add(ghFileInputDoubleParameter.VariableName, new Tuple<double, double, double>(ghFileInputDoubleParameter.CurrentValue, ghFileInputDoubleParameter.LowerBound, ghFileInputDoubleParameter.UpperBound));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(gh_DocumentObject), "Unexpected Grasshopper parameter type.");
                }
            }

            try
            {
                // JSON Serializer
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GrasshopperAllEmasaInputDefsWrapper), new DataContractJsonSerializerSettings()
                    {
                    UseSimpleDictionaryFormat = true
                    });

                byte[] toRet = null;
                using (MemoryStream stream1 = new MemoryStream())
                {
                    // Writes the serialized wrapper
                    ser.WriteObject(stream1, wrapper);
                    toRet = stream1.ToArray();
                }

                return toRet;
            }
            catch { }

            return null;
        }

        public bool Grasshopper_UpdateEmasaInputs(string[] inNames, double[,] inValues, bool inRecomputeGrasshopper)
        {
            // Checks if the lengths are the same
            if (inNames.Length != inValues.GetLength(0)) return false;

            // Checks if the second length of the inValues is of 3 values
            if (inValues.GetLength(1) != 3) return false;

            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");
            if (!gh.IsEditorLoaded()) return false;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return false;

            // Gets the custom input parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            List<GHEMSParameterInterface> emsInputs = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include)
                .Where(a => a is GHEmsInput_IntegerParameter || a is GHEmsInput_DoubleParameter || a is GHEmsInput_PointParameter).Cast<GHEMSParameterInterface>().ToList();

            // Iterates the parameters
            for (int index = 0; index < inNames.Length; index++)
            {
                string pName = inNames[index];
                // Finds the parameter with this name
                GHEMSParameterInterface iParam = emsInputs.FirstOrDefault(a => a.VariableName == pName);
                if (iParam == null) throw new Exception($"Could not find input parameter named {pName}");

                switch (iParam)
                {
                    case GHEmsInput_DoubleParameter ghFileInputDoubleParameter:
                        ghFileInputDoubleParameter.CurrentValue = inValues[index, 0];
                        break;

                    case GHEmsInput_IntegerParameter ghFileInputIntegerParameter:
                        ghFileInputIntegerParameter.CurrentValue = (int)inValues[index, 0];
                        break;

                    case GHEmsInput_PointParameter ghFileInputPointParameter:
                        ghFileInputPointParameter.CurrentValue = new Point3d(inValues[index, 0], inValues[index, 1], inValues[index, 2]);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(iParam), "Unexpected Grasshopper parameter type.");
                }
            }

            if (inRecomputeGrasshopper)
            {
                gh.RunSolver(true);
            }

            return true;
        }

        public bool Grasshopper_UpdateEmasaInput_Integer(string inName, int inValue, bool inRecomputeGrasshopper)
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");
            if (!gh.IsEditorLoaded()) return false;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return false;

            // Gets the custom input parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            GHEmsInput_IntegerParameter emsParam = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include).FirstOrDefault(a => a is GHEmsInput_IntegerParameter p && p.Name == inName) as GHEmsInput_IntegerParameter;
            if (emsParam == null) throw new Exception($"Could not find input parameter named {inName}");

            // Updates the current value
            emsParam.CurrentValue = inValue;

            if (inRecomputeGrasshopper)
            {
                gh.RunSolver(true);
            }

            return true;
        }
        public bool Grasshopper_UpdateEmasaInput_Double(string inName, double inValue, bool inRecomputeGrasshopper)
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");
            if (!gh.IsEditorLoaded()) return false;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return false;

            // Gets the custom input parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            GHEmsInput_DoubleParameter emsParam = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include).FirstOrDefault(a => a is GHEmsInput_DoubleParameter p && p.Name == inName) as GHEmsInput_DoubleParameter;
            if (emsParam == null) throw new Exception($"Could not find input parameter named {inName}");

            // Updates the current value
            emsParam.CurrentValue = inValue;

            if (inRecomputeGrasshopper)
            {
                gh.RunSolver(true);
            }

            return true;
        }
        public bool Grasshopper_UpdateEmasaInput_Point(string inName, double[] inValues, bool inRecomputeGrasshopper)
        {
            dynamic gh = RhinoApp.GetPlugInObject("Grasshopper");
            if (!gh.IsEditorLoaded()) return false;

            // The document has not been saved or we could not get an open document
            if (Grasshopper.Instances.ActiveCanvas.Document == null) return false;

            // Gets the custom input parameters
            GH_Document ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            GHEmsInput_PointParameter emsParam = ghDoc.FilterObjects(enabledObjects: GH_Filter.Include).FirstOrDefault(a => a is GHEmsInput_PointParameter p && p.Name == inName) as GHEmsInput_PointParameter;
            if (emsParam == null) throw new Exception($"Could not find input parameter named {inName}");

            // Updates the current value
            emsParam.CurrentValue = new Point3d(inValues[0], inValues[1], inValues[2]);

            if (inRecomputeGrasshopper)
            {
                gh.RunSolver(true);
            }

            return true;
        }
        #endregion


        #region ImageGeneration
        public bool PrepareRhinoViewForImageAcquire()
        {
            ManualResetEvent successHandle = new ManualResetEvent(false);
            ManualResetEvent failHandle = new ManualResetEvent(false);

            void lf_UiThread()
            {
                try
                {
                    // Ensures that no views are floating
                    foreach (RhinoView v in ActiveDoc.Views)
                    {
                        if (v.Floating) v.Floating = false;
                    }

                    // Creates a new floating view
                    RhinoView view = ActiveDoc.Views.Add("Rhino", DefinedViewportProjection.Perspective, new Rectangle(0, 0, 810, 610), true);
                    view.Size = new Size(810, 610);
                    view.Redraw();

                    // Closes all other non-floating viewports
                    foreach (RhinoView v in ActiveDoc.Views)
                    {
                        if (!v.Floating) v.Close();
                    }

                    // Makes the view active
                    ActiveDoc.Views.ActiveView = view;

                    // Redraws the viewport
                    ActiveDoc.Views.Redraw();

                    // Tells the other threads that we finished successfully
                    successHandle.Set();
                }
                catch
                {
                    failHandle.Set();
                }
            }

            try
            {
                RhinoApp.InvokeOnUiThread((Action)lf_UiThread, null);

                // Waits for the response from the UI thread
                WaitHandle.WaitAny(new WaitHandle[] { successHandle, failHandle });

                // This thread was released by the failHandle
                if (successHandle.GetSafeWaitHandle().IsClosed) return false; // Rhino activities failed

                return true; // Success!
            }
            catch
            {
                return false; // General failure.
            }
        }
        public string GetScreenshotsInXmlFormat(string[] inDirections)
        {
            List<(string, byte[])> listOfImages = new List<(string, byte[])>();

            ManualResetEvent successHandle = new ManualResetEvent(false);
            ManualResetEvent failHandle = new ManualResetEvent(false);

            void lf_UiThread()
            {
                try
                {
                    foreach (string dir in inDirections)
                    {
                        Point3d cameraLocation = Point3d.Unset;

                        // Sets the direction of the view
                        switch (dir)
                        {
                            case "Top_Towards_ZNeg":
                                cameraLocation = new Point3d(0d, 0d, 1d);
                                break;

                            case "Front_Towards_YPos":
                                cameraLocation = new Point3d(0d, -1d, 0d);
                                break;

                            case "Back_Towards_YNeg":
                                cameraLocation = new Point3d(0d, 1d, 0d);
                                break;
                            case "Right_Towards_XNeg":
                                cameraLocation = new Point3d(1d, 0d, 0d);
                                break;
                            case "Left_Towards_XPos":
                                cameraLocation = new Point3d(-1d, 0d, 0d);
                                break;

                            case "Perspective_Top_Front_Edge":
                                cameraLocation = new Point3d(0.2d, -1d, 1d);
                                break;
                            case "Perspective_Top_Back_Edge":
                                cameraLocation = new Point3d(0.2d, 1d, 1d);
                                break;
                            case "Perspective_Top_Right_Edge":
                                cameraLocation = new Point3d(1d, 0.2d, 1d);
                                break;
                            case "Perspective_Top_Left_Edge":
                                cameraLocation = new Point3d(-1d, 0.2d, 1d);
                                break;


                            case "Perspective_TFL_Corner":
                                cameraLocation = new Point3d(-1d, -1d, 1d);
                                break;
                            case "Perspective_TBR_Corner":
                                cameraLocation = new Point3d(1d, 1d, 1d);
                                break;
                            case "Perspective_TBL_Corner":
                                cameraLocation = new Point3d(-1d, 1d, 1d);
                                break;

                            case "Perspective_TFR_Corner":
                            default:
                                cameraLocation = new Point3d(1d, -1d, 1d);
                                break;
                        }
                        ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(Point3d.Origin, cameraLocation);
                        ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtents();
                        ActiveDoc.Views.ActiveView.Redraw();

                        Bitmap bp = ActiveDoc.Views.ActiveView.CaptureToBitmap(true, true, false);

                        // Converts to Byte Array
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bp.Save(ms, ImageFormat.Png);
                            listOfImages.Add((dir, ms.ToArray()));
                        }
                    }

                    // Tells the other threads that we finished successfully
                    successHandle.Set();
                }
                catch
                {
                    failHandle.Set();
                }
            }

            try
            {
                // Populates the image list on the UI thread
                RhinoApp.InvokeOnUiThread((Action)lf_UiThread, null);

                // Waits for the response from the UI thread
                WaitHandle.WaitAny(new WaitHandle[] { successHandle, failHandle });

                // This thread was released by the failHandle
                if (successHandle.GetSafeWaitHandle().IsClosed) return null; // Rhino activities failed

                try
                {
                    if (listOfImages.Count == 0) return null; // No image came back.

                    // Serializing
                    string xmlData;
                    XmlSerializer serializer = new XmlSerializer(typeof(List<(string, byte[])>));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        serializer.Serialize(ms, listOfImages);
                        xmlData = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
                    }

                    return xmlData; // Success!
                }
                catch (Exception ex)
                {
                    return null; // Serialization failed
                }
            }
            catch
            {
                return null; // General failure
            }
        }
        public bool RestoreRhinoViewFromImageAcquire()
        {
            ManualResetEvent successHandle = new ManualResetEvent(false);
            ManualResetEvent failHandle = new ManualResetEvent(false);

            void lf_UiThread()
            {
                try
                {
                    // Ensures that no views are floating
                    foreach (RhinoView v in ActiveDoc.Views)
                    {
                        if (v.Floating) v.Floating = false;
                    }

                    RunScript("_-4View", true);

                    successHandle.Set();
                }
                catch
                {
                    failHandle.Set();
                }
            }

            try
            {
                RhinoApp.InvokeOnUiThread((Action)lf_UiThread, null);

                // Waits for the response from the UI thread
                WaitHandle.WaitAny(new WaitHandle[] {successHandle, failHandle});

                // This thread was released by the failHandle
                if (successHandle.GetSafeWaitHandle().IsClosed) return false; // Rhino activities failed

                return true; // Success!
            }
            catch
            {
                return false; // General failure.
            }
        }
        #endregion
    }

    [DataContract]
    internal class GrasshopperAllEmasaOutputWrapper
    {
        [DataMember]
        public Dictionary<string, List<double>> DoubleLists { get; set; } = new Dictionary<string, List<double>>();

        [DataMember]
        public Dictionary<string, List<Point3d>> PointLists { get; set; } = new Dictionary<string, List<Point3d>>();

        [DataMember]
        public Dictionary<string, List<Line>> LineLists { get; set; } = new Dictionary<string, List<Line>>();
    }

    [DataContract]
    internal class GrasshopperAllEmasaInputDefsWrapper
    {
        [DataMember]
        public List<string> IntegerInputs { get; set; } = new List<string>();

        [DataMember]
        public Dictionary<string, Tuple<double, double, double>> DoubleInputs { get; set; } = new Dictionary<string, Tuple<double, double, double>>();

        [DataMember]
        public Dictionary<string, Tuple<Point3d, Point3d, Point3d>> PointInputs { get; set; } = new Dictionary<string, Tuple<Point3d, Point3d, Point3d>>();
    }
}
