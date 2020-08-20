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
using AccordHelper.Opt.ParamDefinitions;
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

        public bool IsSectionAnalysis = false;

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

        public List<FeLoad> Loads { get; private set; } = new List<FeLoad>();
        public HashSet<FeSection> Sections
        {
            get => Frames.Select(a => a.Value.Section).Distinct().ToHashSet();
        }
        public HashSet<FeMaterial> Materials
        {
            get => Sections.Select(a => a.Material).Distinct().ToHashSet();
        }

        private FeJoint AddNewOrGetJointByCoordinate(Point3d inPoint)
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

        public void AddGravityLoad(double inFactor = 1d)
        {
            FeLoad gravity = FeLoad_Inertial.StandardGravity;
            gravity.Factor = inFactor;
            Loads.Add(gravity);
        }

        [Obsolete]
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
        [Obsolete]
        public void AddRestraint(string inGroupName, bool[] inDoF)
        {
            FeGroup grp = AddNewOrGetGroup(inGroupName);
            grp.Restraint = new FeRestraint(inDoF);
        }


        public List<Point3d> AddJoints_IntermediateParameter(string inIntermediateParameterName, List<string> inGroupNames = null)
        {
            // The default group is the var name without special chars 
            if (inGroupNames == null) inGroupNames = new List<string>() { inIntermediateParameterName.Replace(' ', '_').Replace('.', '_') };

            // Gets the points from the parameter
            List<Point3d> points = Problem.CurrentSolverSolution.GetIntermediateValueByName<List<Point3d>>(inIntermediateParameterName);

            // Finds the parameter definition in the list - it will have the restraints
            PointList_Output_ParamDef pParam = Problem.ObjectiveFunction.IntermediateDefs.FirstOrDefault(a => a.Name == inIntermediateParameterName) as PointList_Output_ParamDef;

            if (pParam == null) throw new Exception($"The parameter {inIntermediateParameterName} is not a PointList_Output_ParamDef or could not be found.");

            // Adds the joint definitions to the Model
            List<FeJoint> addedJoints = new List<FeJoint>();
            foreach (Point3d pnt in points)
            {
                FeJoint j = AddJoint(pnt, pParam);
                addedJoints.Add(j);
            }

            return points;
        }
        private FeJoint AddJoint(Point3d inRhinoPoint, PointList_Output_ParamDef inPointParam)
        {
            FeJoint joint = AddNewOrGetJointByCoordinate(inRhinoPoint);

            joint.Restraint.IncorporateRestraint(inPointParam.Restraint);

            return joint;
        }

        public List<Line> AddFrames_IntermediateParameter(string inIntermediateParameterName, List<string> inGroupNames = null)
        {
            // The default group is the var name without special chars 
            if (inGroupNames == null) inGroupNames = new List<string>() { inIntermediateParameterName.Replace(' ','_').Replace('.','_') };

            // Gets the lines from the parameter
            List<Line> lines = Problem.CurrentSolverSolution.GetIntermediateValueByName<List<Line>>(inIntermediateParameterName);

            // Finds the parameter definition in the list - it will have the sections and the restraints
            LineList_Output_ParamDef lParam = Problem.ObjectiveFunction.IntermediateDefs.FirstOrDefault(a => a.Name == inIntermediateParameterName) as LineList_Output_ParamDef;
            
            if (lParam == null ) throw new Exception($"The parameter {inIntermediateParameterName} is not a LineList_Output_ParamDef or could not be found.");
            if (lParam.SolveSection == null) throw new Exception("The SolveSection parameter of the LineList_Output_ParamDef must be set.");

            // Adds the line definitions to the Model
            List<FeFrame> addedFrames = new List<FeFrame>();
            foreach (Line line in lines)
            {
                FeFrame f = AddFrame(line, lParam);
                addedFrames.Add(f);
            }

            return lines;
        }
        private FeFrame AddFrame(Line inRhinoLine, LineList_Output_ParamDef inLineParam)
        {
            // Adds the joints if they are to be added
            FeJoint iJoint = AddNewOrGetJointByCoordinate(inRhinoLine.From);
            FeJoint jJoint = AddNewOrGetJointByCoordinate(inRhinoLine.To);

            // Adds a restraint to the joint if there is one given by this line
            iJoint.Restraint.IncorporateRestraint(inLineParam.Restraint);
            jJoint.Restraint.IncorporateRestraint(inLineParam.Restraint);

            // Creates the Frame Element
            FeFrame newFrame = new FeFrame(FrameCount, inLineParam.SolveSection, iJoint, jJoint);

            // We already have this Frame?
            if (Frames.Any(a => (a.Value.IJoint == newFrame.IJoint && a.Value.JJoint == newFrame.JJoint) ||
                                (a.Value.IJoint == newFrame.JJoint && a.Value.JJoint == newFrame.IJoint)))
            {
                throw new Exception($"A Frame linking joint {newFrame.IJoint} to {newFrame.JJoint} already exists.");
            }

            Frames.Add(newFrame.Id, newFrame);

            FrameCount++;

            return newFrame;
        }


        public abstract void InitializeSoftware(bool inIsSectionSelection = false);

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

        public abstract void RunAnalysisAndGetResults(List<ResultOutput> inDesiredResults, int inEigenvalueBucklingMode = 0, double inEigenvalueBucklingScaleFactor = double.NaN);

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

        EigenvalueBuckling_Summary,

        EigenvalueBuckling_ModeShape1,
        EigenvalueBuckling_ModeShape2,
        EigenvalueBuckling_ModeShape3,
    }
}
