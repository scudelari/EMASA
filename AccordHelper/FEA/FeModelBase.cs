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
using AccordHelper.FEA.Results;
using r3dm::Rhino.DocObjects;
using r3dm::Rhino.Geometry;

namespace AccordHelper.FEA
{
    public abstract class FeModelBase
    {
        public virtual string SubDir { get; } = null;

        public string ModelFolder { get; set; }
        public string FileName { get; set; }
        public string FullFileName { get => Path.Combine(ModelFolder, FileName); }

        protected FeModelBase(string inModelFolder, string inFilename)
        {
            if (SubDir != null) inModelFolder = Path.Combine(inModelFolder, SubDir);

            ModelFolder = inModelFolder;
            FileName = inFilename;

            if (Directory.Exists(ModelFolder)) Directory.Delete(ModelFolder, true);
            Directory.CreateDirectory(ModelFolder);
        }

        public void CleanUpDirectory()
        {
            foreach (string file in Directory.GetFiles(ModelFolder))
            {
                File.Delete(file);
            }
        }

        public int JointRoundingDecimals = 3;
        public double SlendernessLimit = 120d;

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

        public void AddFrameList(List<Line> inLines, string inSectionName = null, List<string> inGroupNames = null)
        {
            List<FeFrame> frames = new List<FeFrame>();

            foreach (Line line in inLines)
            {
                frames.Add(AddFrame(line, inSectionName));
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
        public FeFrame AddFrame(Line inRhinoLine, string inSectionName = null, List<string> inGroupNames = null)
        {
            // Adds the joints if they are to be added
            FeJoint iJoint = AddNewOrGetJointByCoordinate(inRhinoLine.From);
            FeJoint jJoint = AddNewOrGetJointByCoordinate(inRhinoLine.To);

            FeSection section;

            // No section given
            if (string.IsNullOrWhiteSpace(inSectionName))
            {
                // Maybe it is always calculated in the Rhino Library. Ensures only once.
                double lLenght = inRhinoLine.Length;

                // Gets the section with the smallest area that yields an acceptable slenderness ratio
                section = (from a in FeSectionPipe.GetAllSections()
                    where lLenght / a.LeastGyrationRadius < SlendernessLimit // Limits the possible sections by the slenderness ratio of this line
                    orderby a.Area // Orders by the area so that we can get the first
                    select a).First();
            }
            else
            {
                section = FeSectionPipe.GetSectionByName(inSectionName);
            }

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

        public abstract void WriteModelToSoftware();
        public abstract void RunAnalysis();
        public abstract void SaveDataAs(string inFilePath);
        public abstract void CloseApplication();

        public virtual void GetResult_FillElementStrainEnergy() { throw new NotImplementedException(); }

        public virtual void GetResult_FillNodalReactions() { throw new NotImplementedException(); }
        public virtual void GetResult_FillNodalDisplacements() { throw new NotImplementedException(); }

        public virtual void GetResult_FillElementNodalBendingStrain() { throw new NotImplementedException(); }
        public virtual void GetResult_FillElementNodalStress() { throw new NotImplementedException(); }
        public virtual void GetResult_FillElementNodalForces() { throw new NotImplementedException(); }
        public virtual void GetResult_FillElementNodalStrains() { throw new NotImplementedException(); }

        public virtual void GetResult_FillNodalSectionPointStressStrain() { throw new NotImplementedException(); }
    }

    public enum FeaSoftwareEnum
    {
        Ansys,
        Sap2000,
        NoFea
    }


}
