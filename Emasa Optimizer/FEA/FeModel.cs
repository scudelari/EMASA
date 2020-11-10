extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using BaseWPFLibrary.Annotations;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.FEA.Loads;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.FEA
{
    public class FeModel : IFeEntity
    {
        public NlOpt_Point Owner { get; private set; }
        /// <summary>
        /// Creates the abstraction of the FeModel.
        /// </summary>
        /// <param name="inSolPoint">The solution point that contain the Gh Geometry that will define the model. Usually the model is then saved into the its FeModel parameter</param>
        public FeModel([NotNull] NlOpt_Point inSolPoint)
        {
            try
            {
                if (inSolPoint == null) throw new ArgumentNullException(nameof(inSolPoint));

                Owner = inSolPoint;

                // Generates the model
                //////////////////////////

                // For each point list GH Geometry Parameter - Adds the joint
                foreach (PointList_GhGeom_ParamDef pointList_Output_ParamDef in inSolPoint.GhGeom_Values.Keys.OfType<PointList_GhGeom_ParamDef>())
                {
                    FeGroup grp = AddNewOrGet_Group(pointList_Output_ParamDef.FeGroupNameHelper);

                    List<Point3d> pPoints = (List<Point3d>)inSolPoint.GhGeom_Values[pointList_Output_ParamDef];

                    foreach (Point3d p in pPoints)
                    {
                        FeJoint j = AddNewOrGet_JointByCoordinate(p);
                        j.Restraint.IncorporateRestraint(pointList_Output_ParamDef.Restraint);
                        grp.AddElement(j);
                    }
                }

                // For each line list GH Geometry Parameter - Adds the joints and a frame
                foreach (LineList_GhGeom_ParamDef lineList_Output_ParamDef in inSolPoint.GhGeom_Values.Keys.OfType<LineList_GhGeom_ParamDef>())
                {
                    FeGroup grp = AddNewOrGet_Group(lineList_Output_ParamDef.FeGroupNameHelper);

                    List<Line> pLines = (List<Line>)inSolPoint.GhGeom_Values[lineList_Output_ParamDef];

                    foreach (Line l in pLines)
                    {
                        // Adds the From joint
                        FeJoint jFrom = AddNewOrGet_JointByCoordinate(l.From);
                        jFrom.Restraint.IncorporateRestraint(lineList_Output_ParamDef.Restraint);
                        grp.AddElement(jFrom);

                        // Adds the To joint
                        FeJoint jTo = AddNewOrGet_JointByCoordinate(l.To);
                        jTo.Restraint.IncorporateRestraint(lineList_Output_ParamDef.Restraint);
                        grp.AddElement(jTo);

                        // Adds the Frame
                        FeSection s = Owner.Owner.GetGhLineListSection(lineList_Output_ParamDef);
                        FeFrame f = AddNewOrGet_LineByCoordinate(jFrom, jTo, s);
                        grp.AddElement(f);
                    }
                }

                // Adds the gravity loads
                if (AppSS.I.FeOpt.Gravity_IsLoad)
                {
                    FeLoad_Inertial gravity = FeLoad_Inertial.GetStandardGravity(AppSS.I.FeOpt.Gravity_Multiplier);
                    // Sets the direction based on the options
                    switch (AppSS.I.FeOpt.Gravity_DirectionEnum_Selected)
                    {
                        case MainAxisDirectionEnum.xPos:
                            gravity.Direction = Vector3d.XAxis;
                            break;

                        case MainAxisDirectionEnum.xNeg:
                            gravity.Direction = -Vector3d.XAxis;
                            break;

                        case MainAxisDirectionEnum.yPos:
                            gravity.Direction = Vector3d.YAxis;
                            break;

                        case MainAxisDirectionEnum.yNeg:
                            gravity.Direction = -Vector3d.YAxis;
                            break;

                        case MainAxisDirectionEnum.zPos:
                            gravity.Direction = Vector3d.ZAxis;
                            break;

                        case MainAxisDirectionEnum.zNeg:
                            gravity.Direction = -Vector3d.ZAxis;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    Loads.Add(gravity);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error defining the FeModel internal class.", e);
            }
        }

        public string ModelName { get; set; } = "EMSRhinoAuto";

        private int _pointCount = 1;
        private int _frameCount = 1;

        public int JointRoundingDecimals = Properties.Settings.Default.Default_JointRoundingDecimals;
        public Point3d RoundedPoint3d(Point3d inPoint)
        {
            return new Point3d(
                Math.Round(inPoint.X, JointRoundingDecimals, MidpointRounding.ToEven),
                Math.Round(inPoint.Y, JointRoundingDecimals, MidpointRounding.ToEven),
                Math.Round(inPoint.Z, JointRoundingDecimals, MidpointRounding.ToEven));
        }

        public Dictionary<int, FeFrame> Frames { get; } = new Dictionary<int, FeFrame>();
        public Dictionary<int, FeJoint> Joints { get; } = new Dictionary<int, FeJoint>();
        public Dictionary<string, FeGroup> Groups { get; } = new Dictionary<string, FeGroup>();
        public Dictionary<int, FeMeshNode> MeshNodes { get; } = new Dictionary<int, FeMeshNode>();
        public Dictionary<int, FeMeshBeamElement> MeshBeamElements { get; } = new Dictionary<int, FeMeshBeamElement>();

        public List<FeLoad> Loads { get; } = new List<FeLoad>();
        public HashSet<FeSection> Sections => Frames.Select(a => a.Value.Section).Distinct().ToHashSet();
        public HashSet<FeMaterial> Materials => Sections.Select(a => a.Material).Distinct().ToHashSet();

        /// <summary>
        /// Gets a FeJoint by the given coordinates.
        /// </summary>
        /// <param name="inPoint">The point. THE COORDINATES MUST HAVE BEEN PREVIOUSLY ROUNDED!</param>
        /// <returns>The joint if found, otherwise null.</returns>
        public FeJoint Get_JointByCoordinate(Point3d inPoint)
        {
            return Joints.FirstOrDefault(a => a.Value.Point == inPoint).Value;
        }
        private FeJoint AddNewOrGet_JointByCoordinate(Point3d inPoint)
        {
            // Rounds the point coordinates
            inPoint = RoundedPoint3d(inPoint);

            // Already exists in the list?
            if (Joints.Any(a => a.Value.Point == inPoint)) return Joints.First(a => a.Value.Point == inPoint).Value;

            FeJoint newJoint = new FeJoint(_pointCount, inPoint);
            Joints.Add(newJoint.Id, newJoint);
            _pointCount++;
            return newJoint;
        }
        private FeGroup AddNewOrGet_Group([NotNull] string inGroupName)
        {
            if (string.IsNullOrWhiteSpace(inGroupName)) throw new ArgumentException("ResultValue cannot be null or whitespace.", nameof(inGroupName));

            // If the group already exists, gets it
            if (Groups.ContainsKey(inGroupName)) return Groups[inGroupName];

            // Otherwise, creates a new one
            FeGroup newGroup = new FeGroup(inGroupName);
            Groups.Add(newGroup.Name, newGroup);
            return newGroup;
        }
        private FeFrame AddNewOrGet_LineByCoordinate([NotNull] FeJoint inFrom, [NotNull] FeJoint inTo, [NotNull] FeSection inSection)
        {
            if (inFrom == null) throw new ArgumentNullException(nameof(inFrom));
            if (inTo == null) throw new ArgumentNullException(nameof(inTo));
            if (inSection == null) throw new ArgumentNullException(nameof(inSection));

            // Already exists in the list?
            FeFrame alreadyExisting = Frames.FirstOrDefault(a => (a.Value.IJoint == inFrom && a.Value.JJoint == inTo) || (a.Value.JJoint == inFrom && a.Value.IJoint == inTo)).Value;
            if (alreadyExisting != null) return alreadyExisting;
            //if () throw new InvalidOperationException("Finite Element Models are Limited to Having One Frame Per Location.");

            FeFrame newFrame = new FeFrame(_frameCount, inSection, inFrom, inTo);
            Frames.Add(newFrame.Id, newFrame);
            _frameCount++;
            return newFrame;
        }

        public BoundingBox Joints_BoundingBox => new BoundingBox(from a in Joints.Values select a.Point);
        public double Joints_BoundingBox_MaxLength => (from a in Joints_BoundingBox.GetEdges() select a.Length).Max();

        #region Results
        public bool HasMeshNodes = false;
        public bool HasMeshBeams = false;
        public List<FeResultItem> Results { get; } = new List<FeResultItem>();
        #endregion



        public string WpfName => ModelName;
    }
}
