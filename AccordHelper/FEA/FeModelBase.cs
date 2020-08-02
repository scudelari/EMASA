extern alias r3dm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Accord;
using AccordHelper.FEA.Items;
using AccordHelper.FEA.Loads;
using AccordHelper.FEA.Results;
using AccordHelper.Opt;
using Prism.Mvvm;
using r3dm::Rhino.DocObjects;
using r3dm::Rhino.Geometry;

namespace AccordHelper.FEA
{
    public abstract class FeModelBase : BindableBase
    {
        protected ProblemBase Problem { get; set; }

        public virtual string SubDir { get; } = null;

        public string ModelFolder { get; set; }
        public string FileName { get; set; }
        public string FullFileName { get => Path.Combine(ModelFolder, FileName); }

        protected FeModelBase(string inModelFolder, string inFilename, ProblemBase inProblem)
        {
            if (SubDir != null) inModelFolder = Path.Combine(inModelFolder, SubDir);

            ModelFolder = inModelFolder;
            FileName = inFilename;

            if (Directory.Exists(ModelFolder)) Directory.Delete(ModelFolder, true);
            Directory.CreateDirectory(ModelFolder);

            Problem = inProblem;
        }

        public void CleanUpDirectory()
        {
            foreach (string file in Directory.GetFiles(ModelFolder))
            {
                File.Delete(file);
            }
        }

        private Point3d RoundPoint3d(Point3d inPoint)
        {
            return new Point3d(
                Math.Round(inPoint.X, JointRoundingDecimals, MidpointRounding.ToEven),
                Math.Round(inPoint.Y, JointRoundingDecimals, MidpointRounding.ToEven),
                Math.Round(inPoint.Z, JointRoundingDecimals, MidpointRounding.ToEven));
        }

        private int PointCount = 1;
        private int FrameCount = 1;
        //private int SectionCount = 1;

        public string ModelName { get; set; } = "EMSRhinoAuto";

        public Dictionary<int, FeFrame> Frames { get; private set; } = new Dictionary<int, FeFrame>();
        public Dictionary<int, FeJoint> Joints { get; private set; } = new Dictionary<int, FeJoint>();
        public Dictionary<string, FeGroup> Groups { get; private set; } = new Dictionary<string, FeGroup>();
        public Dictionary<int, FeMeshNode> MeshNodes { get; private set; } = new Dictionary<int, FeMeshNode>();
        public Dictionary<int, FeMeshBeamElement> MeshBeamElements { get; private set; } = new Dictionary<int, FeMeshBeamElement>();

        public List<FeLoadBase> Loads { get; private set; } = new List<FeLoadBase>();
        public HashSet<FeSection> Sections
        {
            get => Frames.Select(a => a.Value.Section).Distinct().ToHashSet();
        }
        public HashSet<FeMaterial> Materials
        {
            get => Sections.Select(a => a.Material).Distinct().ToHashSet();
        }

        public FeJoint AddNewOrGetJointByCoordinate(Point3d inPoint)
        {
            // Rounds the point coordinates
            inPoint = RoundPoint3d(inPoint);

            // Already exists in the list?
            if (Joints.Any(a => a.Value.Point == inPoint)) return Joints.First(a => a.Value.Point == inPoint).Value;

            FeJoint newJoint = new FeJoint(PointCount, inPoint);
            Joints.Add(newJoint.Id, newJoint);
            PointCount++;
            return newJoint;
        }
        public FeGroup AddNewOrGetGroup(string inGroupName)
        {
            if (string.IsNullOrWhiteSpace(inGroupName)) throw new ArgumentException("inGroupName cannot be empty or null.");

            FeGroup newGroup = new FeGroup(inGroupName);

            if (!Groups.ContainsKey(newGroup.Name)) Groups.Add(newGroup.Name, newGroup);

            return Groups[newGroup.Name];
        }

        public void AddGravityLoad()
        {
            Loads.Add(FeLoad_Inertial.StandardGravity);
        }

        public void AddPoint3dToGroups(List<Point3d> inPoints, List<string> inGroupNames)
        {
            if (inPoints.Count == 0 || inGroupNames.Count == 0) throw new ArgumentException("No data.");

            foreach (string groupName in inGroupNames)
            {
                FeGroup grp = AddNewOrGetGroup(groupName);
                foreach (Point3d point3d in inPoints)
                {
                    FeJoint j = AddNewOrGetJointByCoordinate(point3d);
                    grp.Joints.Add(j);
                }
            }
        }

        public void AddFrameList(List<Line> inLines, List<string> inGroupNames = null, List<FeSection> inPossibleSections = null)
        {
            List<FeFrame> frames = new List<FeFrame>();
            
            if (inPossibleSections == null || inPossibleSections.Count == 0) inPossibleSections = FeSectionPipe.GetAllSections();


            foreach (Line line in inLines)
            {
                frames.Add(AddFrame(line, inPossibleSections));
            }

            // Also adds the frame to the groups?
            if (inGroupNames != null && inGroupNames.Count > 0)
            {
                foreach (string groupName in inGroupNames)
                {
                    FeGroup grp = AddNewOrGetGroup(groupName);
                    foreach (FeFrame feFrame in frames)
                    {
                        grp.Frames.Add(feFrame);
                    }

                }
            }
        }
        public FeFrame AddFrame(Line inRhinoLine, List<FeSection> inPossibleSections, List<string> inGroupNames = null)
        {
            if (inPossibleSections.Count == 0) throw new ArgumentException("Must contain at least one value.", nameof(inPossibleSections));

            // Adds the joints if they are to be added
            FeJoint iJoint = AddNewOrGetJointByCoordinate(inRhinoLine.From);
            FeJoint jJoint = AddNewOrGetJointByCoordinate(inRhinoLine.To);

            // Assigns the value that is at the median considering the area
            inPossibleSections.Sort();
            FeSection section = inPossibleSections[inPossibleSections.Count/2 + 1];


            // Creates the Frame Element
            FeFrame newFrame = new FeFrame(FrameCount, section, iJoint, jJoint);

            // We already have this Frame?
            if (Frames.Any(a => (a.Value.IJoint == newFrame.IJoint && a.Value.JJoint == newFrame.JJoint) ||
                                (a.Value.IJoint == newFrame.JJoint && a.Value.JJoint == newFrame.IJoint)))
            {
                throw new Exception($"A Frame linking joint {newFrame.IJoint} to {newFrame.JJoint} already exists.");
            }

            Frames.Add(newFrame.Id, newFrame);

            // Also adds the frame to the groups?
            if (inGroupNames != null && inGroupNames.Count > 0)
            {
                foreach (string groupName in inGroupNames)
                {
                    FeGroup grp = AddNewOrGetGroup(groupName);
                    grp.Frames.Add(newFrame);
                }
            }

            FrameCount++;

            return newFrame;
        }

        public void AddRestraint(string inGroupName, bool[] inDoF)
        {
            FeGroup grp = AddNewOrGetGroup(inGroupName);
            grp.Restraint = new FeRestraint(inDoF);
        }

        public abstract void InitializeSoftware();

        public abstract void ResetSoftwareData();
        public virtual void ResetClassData()
        {
            PointCount = 1;
            FrameCount = 1;
            Frames = new Dictionary<int, FeFrame>();
            Joints = new Dictionary<int, FeJoint>();
            Groups = new Dictionary<string, FeGroup>();
            MeshNodes = new Dictionary<int, FeMeshNode>();
            MeshBeamElements = new Dictionary<int, FeMeshBeamElement>();

        }

        public abstract void CloseApplication();

        public abstract void InitialPassForSectionAssignment();

        public abstract void RunAnalysisAndGetResults(List<ResultOutput> inDesiredResults);

        #region General Options
        public int JointRoundingDecimals = 3;
        public double SlendernessLimit = 120d;

        private double _elementPlotScale = 50d;
        public double ElementPlotScale
        {
            get => _elementPlotScale;
            set => SetProperty(ref _elementPlotScale, value);
        }

        private bool _originalShapeWireframe;
        public bool OriginalShapeWireframe
        {
            get => _originalShapeWireframe;
            set => SetProperty(ref _originalShapeWireframe, value);
        }

        private bool _displayOnDeformedShape = true;
        public bool DisplayOnDeformedShape
        {
            get => _displayOnDeformedShape;
            set => SetProperty(ref _displayOnDeformedShape, value);
        }
        private bool _displayOnDeformedShape_AutoScale = true;
        public bool DisplayOnDeformedShape_AutoScale
        {
            get => _displayOnDeformedShape_AutoScale;
            set => SetProperty(ref _displayOnDeformedShape_AutoScale, value);
        }
        private double _deformedShapePlotScale;
        public double DeformedShapePlotScale
        {
            get => _deformedShapePlotScale;
            set
            {
                SetProperty(ref _deformedShapePlotScale, value);
            }
        }
        #endregion
    }

    public enum FeaSoftwareEnum
    {
        Ansys,
        Sap2000,
        NoFea
    }

    public enum ResultOutput
    {
        Nodal_Reaction,
        Nodal_Displacement,

        SectionNode_Stress,
        SectionNode_Strain,

        ElementNodal_BendingStrain,
        ElementNodal_Force,
        ElementNodal_Strain,
        ElementNodal_Stress,

        ElementNodal_CodeCheck,

        Element_StrainEnergy,
    }
}
