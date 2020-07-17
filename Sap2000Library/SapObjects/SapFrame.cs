using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Sap2000Library.DataClasses;
using Sap2000Library.Managers;
using SAP2000v1;

namespace Sap2000Library.SapObjects
{
    public class SapFrame : SapLine
    {
        private FrameManager owner = null;
        internal SapFrame(string name, SapPoint iEndPoint, SapPoint jEndPoint, FrameManager frameManager) : base(name, SapObjectType.Frame, iEndPoint, jEndPoint, frameManager)
        {
            owner = frameManager;
        }

        public double PerpendicularDistance(Point3D inPoint)
        {
            try
            {
                Point3D closestPoint = Line.ClosestPointTo(inPoint, true);

                try
                {
                    return closestPoint.DistanceTo(inPoint);
                }
                catch
                {
                    return double.MaxValue;
                }
            }
            catch (Exception)
            {
                return double.MaxValue;
            }
        }
        public double PerpendicularDistance(SapPoint inSapPoint) { return PerpendicularDistance(inSapPoint.Point); }
        public double PerpendicularDistance(Line3D inLine)
        {
            var points = Line.ClosestPointsBetween(inLine, true);
            return points.Item1.DistanceTo(points.Item2);
        }
        public double PerpendicularDistance(SapFrame inFrame)
        {
            var points = Line.ClosestPointsBetween(inFrame.Line, true);
            return points.Item1.DistanceTo(points.Item2);
        }
        public double DistanceFromIOfProjection(Point3D inPoint)
        {
            Vector3D temp = iEndPoint.Point.VectorTo(inPoint);
            Vector3D projection = temp.ProjectOn(Vector.Normalize());
            return projection.Length;
        }

        public override string ToString()
        {
            return string.Format("Fname: {0}, Iname: {1}, Jname: {2}", Name, iEndPoint.ToString(), jEndPoint.ToString());
        }

        private SapFrameSection _section = null;
        public SapFrameSection Section
        {
            get
            {
                if (_section != null) return _section;

                _section = owner.GetFrameSectionOfFrame(Name);

                return _section;
            }
        }
        public void SetSection(string inSectionName)
        {
            owner.SetFrameSection(Name, inSectionName);
        }

        public SapPoint IsLinkedToThisFrame(SapFrame inFrame)
        {
            if (inFrame == this) return null;
            if (iEndPoint == inFrame.iEndPoint || iEndPoint == inFrame.jEndPoint) return iEndPoint;
            if (jEndPoint == inFrame.iEndPoint || jEndPoint == inFrame.jEndPoint) return jEndPoint;
            return null;
        }

        private FrameEndRelease _endReleases = null;
        public FrameEndRelease EndReleases
        {
            get
            {
                if (_endReleases != null) return _endReleases;

                bool[] ii = null;
                bool[] jj = null;
                double[] startValue = null;
                double[] endValue = null;

                int ret = owner.SapApi.FrameObj.GetReleases(Name, ref ii, ref jj, ref startValue, ref endValue);

                if (0 != ret) return null;

                _endReleases = new FrameEndRelease(ii, jj, startValue, endValue);
                return _endReleases;
            }
            set
            {
                bool[] ii = new bool[] { value.iU1, value.iU2, value.iU3, value.iR1, value.iR2, value.iR3 };
                bool[] jj = new bool[] { value.jU1, value.jU2, value.jU3, value.jR1, value.jR2, value.jR3 };
                double[] startValue = new double[] { value.iU1ParFix, value.iU2ParFix, value.iU3ParFix, value.iR1ParFix, value.iR2ParFix, value.iR3ParFix };
                double[] endValue = new double[] { value.jU1ParFix, value.jU2ParFix, value.jU3ParFix, value.jR1ParFix, value.jR2ParFix, value.jR3ParFix };

                int ret = owner.SapApi.FrameObj.SetReleases(Name, ref ii, ref jj, ref startValue, ref endValue);

                if (ret != 0) throw new S2KHelperException($"Could not add the releases to frame {Name}.");

                _endReleases = value;
            }
        }

        private FrameAutoMesh _autoMesh = null;
        public FrameAutoMesh AutoMesh
        {
            get
            {
                if (_autoMesh != null) return _autoMesh;

                bool automesh = false;
                bool atintermediates = false;
                bool atintersect = false;
                int minCount = 0;
                double maxLength = 0d;

                int ret = owner.SapApi.FrameObj.GetAutoMesh(Name, ref automesh, ref atintermediates, ref atintersect, ref minCount, ref maxLength);

                if (0 != ret) return null;

                _autoMesh = new FrameAutoMesh() { AutoMesh = automesh, AutoMeshAtIntermdiateJoints = atintermediates, AutoMeshAtIntersections = atintersect, MinimumSegments = minCount, AutoMeshMaxLength = maxLength};
                return _autoMesh;
            }
            set
            {
                int ret = owner.SapApi.FrameObj.SetAutoMesh(Name, value.AutoMesh, value.AutoMeshAtIntermdiateJoints, value.AutoMeshAtIntersections, value.MinimumSegments, value.AutoMeshMaxLength);

                if (0 != ret) throw new S2KHelperException($"Could not set the auto mesh for frame {Name}.");

                _autoMesh = value;
            }
        }

        private FrameTensionCompressionLimit _tcLimit = null;
        public FrameTensionCompressionLimit FrameTCLimit
        {
            get
            {
                if (_tcLimit != null) return _tcLimit;

                double tensionLimit = 0;
                bool hasTensionLimit = false;
                double compressionLimit = 0;
                bool hasCompressionLimit = false;

                int ret = owner.SapApi.FrameObj.GetTCLimits(Name, ref hasCompressionLimit, ref compressionLimit, ref hasTensionLimit, ref tensionLimit);

                if (0 == ret)
                {
                    double? tLim = null;
                    if (hasTensionLimit) tLim = tensionLimit;

                    double? cLim = null;
                    if (hasCompressionLimit) cLim = compressionLimit;

                    _tcLimit = new FrameTensionCompressionLimit(tLim, cLim);
                    return _tcLimit;
                }

                return null;
            }
            set
            {
                double tensionLimit = value.TensionLimit ?? 0;
                bool hasTensionLimit = value.TensionLimit.HasValue;
                double compressionLimit = value.CompressionLimit ?? 0;
                bool hasCompressionLimit = value.CompressionLimit.HasValue;

                int ret = owner.SapApi.FrameObj.SetTCLimits(Name, hasCompressionLimit, compressionLimit, hasTensionLimit, tensionLimit);

                if (ret != 0) throw new S2KHelperException($"Could not set the tension & compression limits for frame {Name}.");

                _tcLimit = value;
            }
        }

        private FrameBasicLocalAxesDef _basicLocalAxes = null;
        public FrameBasicLocalAxesDef BasicLocalAxes
        {
            get
            {
                if (_basicLocalAxes == null)
                {
                    double Ang = 0;
                    bool Advanced = false;

                    int ret = owner.SapApi.FrameObj.GetLocalAxes(Name, ref Ang, ref Advanced);

                    // The query failed
                    if (ret != 0) return null;

                    _basicLocalAxes = new FrameBasicLocalAxesDef { Ang = Ang, Advanced = Advanced };
                }

                return _basicLocalAxes;
            }
            set
            {
                FrameBasicLocalAxesDef objective = value ?? FrameBasicLocalAxesDef.Default;

                int ret = owner.SapApi.FrameObj.SetLocalAxes(Name, value.Ang);

                if (ret != 0) throw new S2KHelperException($"Could not set basic local axes for frame {Name}.");

                if (!value.Advanced) AdvancedLocalAxes = null;

                _basicLocalAxes = objective;
            }
        }

        private FrameAdvancedLocalAxesDef _advancedLocalAxes = null;
        public FrameAdvancedLocalAxesDef AdvancedLocalAxes
        {
            get
            {
                if (_advancedLocalAxes == null)
                {
                    bool Active = false;
                    int Plane2 = 0;
                    int PlVectOpt = 0;
                    string PlCSys = null;
                    int[] PlDir = null;
                    string[] PlPt = null;
                    double[] PlVect = null;

                    int ret = owner.SapApi.FrameObj.GetLocalAxesAdvanced(Name, ref Active,
                        ref Plane2,
                        ref PlVectOpt, ref PlCSys, ref PlDir, ref PlPt, ref PlVect);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _advancedLocalAxes = new FrameAdvancedLocalAxesDef()
                    {
                        Active = Active,
                        Plane2 = (FrameAdvancedAxes_Plane2)Plane2,
                        PlVectOpt = (AdvancedAxesAngle_Vector)PlVectOpt,
                        PlCSys = PlCSys,
                        PlDir_int = PlDir,
                        PlPt = PlPt,
                        PlVect = PlVect
                    };
                }

                return _advancedLocalAxes;
            }
            set
            {
                FrameAdvancedLocalAxesDef objective = value ?? FrameAdvancedLocalAxesDef.NotSet;

                int ret = owner.SapApi.FrameObj.SetLocalAxesAdvanced(Name, objective.Active,
                    objective.Plane2_int,
                    objective.PlVectOpt_int, objective.PlCSys, ref objective.PlDir_int, ref objective.PlPt, ref objective.PlVect);

                if (ret != 0) throw new S2KHelperException($"Could not set advanced local axes for frame {Name}.");

                _advancedLocalAxes = objective;
            }
        }

        public CoordinateSystem GetCSysFromAxesDefinitions()
        {
            UnitVector3D vec2, vec3, vecP;
            UnitVector3D vec1 = Vector.Normalize();

            if (!BasicLocalAxes.Advanced)
            { // Default Orientation
               
                // Local 1-2 is vertical, towards +Z. If the frame is vertical, then the local axis is towards +X
                vecP = vec1.IsVectorVertical() ? UnitVector3D.Create(1, 0, 0) : UnitVector3D.Create(0, 0, 1);

                // V3 is perpendicular to both the axial vector and the reference vector
                vec3 = vec1.CrossProduct(vecP);

                // V2 is then perpendicular to both
                vec2 = vec3.CrossProduct(vec1);
            }
            else
            { // Has Advanced Angles
                if (AdvancedLocalAxes.PlCSys != "GLOBAL") throw new S2KHelperException("Only coordinate systems in the GLOBAL coordinates are supported.");

                switch (AdvancedLocalAxes.PlVectOpt)
                {
                    case AdvancedAxesAngle_Vector.CoordinateDirection:
                        
                        UnitVector3D vecP_primary;
                        switch (AdvancedLocalAxes.PlDir[0])
                        {
                            case AdvancedAxesAngle_PlaneReference.PosX:
                                vecP_primary = UnitVector3D.Create(1, 0, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.PosY:
                                vecP_primary = UnitVector3D.Create(0, 1, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.PosZ:
                                vecP_primary = UnitVector3D.Create(0, 0, 1);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegX:
                                vecP_primary = UnitVector3D.Create(-1, 0, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegY:
                                vecP_primary = UnitVector3D.Create(0, -1, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegZ:
                                vecP_primary = UnitVector3D.Create(0, 0, -1);
                                break;
                            default:
                                throw new S2KHelperException($"Unexpected enum value. Note: Not all axis are supported.");
                        }

                        UnitVector3D vecP_secondary;
                        switch (AdvancedLocalAxes.PlDir[1])
                        {
                            case AdvancedAxesAngle_PlaneReference.PosX:
                                vecP_secondary = UnitVector3D.Create(1, 0, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.PosY:
                                vecP_secondary = UnitVector3D.Create(0, 1, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.PosZ:
                                vecP_secondary = UnitVector3D.Create(0, 0, 1);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegX:
                                vecP_secondary = UnitVector3D.Create(-1, 0, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegY:
                                vecP_secondary = UnitVector3D.Create(0, -1, 0);
                                break;
                            case AdvancedAxesAngle_PlaneReference.NegZ:
                                vecP_secondary = UnitVector3D.Create(0, 0, -1);
                                break;
                            default:
                                throw new S2KHelperException($"Unexpected enum value. Note: Not all axis are supported.");
                        }

                        // Decides which is going to be the vecP
                        if (!vecP_primary.IsParallelTo(vec1))
                            vecP = vecP_primary;
                        else if (!vecP_secondary.IsParallelTo(vec1))
                            vecP = vecP_secondary;
                        else 
                            throw new S2KHelperException($"Both Reference Coordinate Directions are parallel to the Frame's axis.");
                        
                        break;
                    case AdvancedAxesAngle_Vector.TwoJoints:
                        // Gets both points
                        SapPoint firstPoint = owner.s2KModel.PointMan.GetByName(AdvancedLocalAxes.PlPt[0]);
                        SapPoint secondPoint = owner.s2KModel.PointMan.GetByName(AdvancedLocalAxes.PlPt[1]);

                        UnitVector3D candidate = firstPoint.Point.VectorTo(secondPoint.Point).Normalize();

                        if (candidate.IsParallelTo(vec1)) throw new S2KHelperException($"The vector from joints are parallel to the Frame's axis.");

                        vecP = candidate;

                        break;
                    case AdvancedAxesAngle_Vector.UserVector:
                        vecP = AdvancedLocalAxes.PlVect_Vector.Normalize();
                        break;
                    default:
                        throw new S2KHelperException($"Unexpected enum value.");
                }

                switch (AdvancedLocalAxes.Plane2)
                {
                    case FrameAdvancedAxes_Plane2.Plane12:
                        // V3 is perpendicular to both the axial vector and the reference vector
                        vec3 = vec1.CrossProduct(vecP);

                        // V2 is then perpendicular to both
                        vec2 = vec3.CrossProduct(vec1);
                        break;
                    case FrameAdvancedAxes_Plane2.Plane13:
                        // V3 is perpendicular to both the axial vector and the reference vector
                        vec2 = vecP.CrossProduct(vec1);

                        // V2 is then perpendicular to both
                        vec3 = vec1.CrossProduct(vec2);
                        break;
                    default:
                        throw new S2KHelperException($"Unexpected enum value.");
                }
            }

            // The default reference vector is // to 1-2, then
            CoordinateSystem toRet = new CoordinateSystem(Point3D.Origin, vec1, vec2, vec3);

            if (BasicLocalAxes.Ang != 0)
            {
                toRet = toRet.RotateCoordSysAroundVector(toRet.XAxis.Normalize(), Angle.FromDegrees(BasicLocalAxes.Ang));
            }
            return toRet;
        }

        public List<SapFrame> DivideAtDistanceFromPoint(SapPoint inSapPoint, double inDistance, string rename = null, double inMinLenghtMoreThanDistance = 1, uint? ensureMinTime = null)
        {
            if (!IsPointIJ(inSapPoint)) throw new S2KHelperException($"Cannot break frame {Name}. The given point {inSapPoint.Name} is not a point of this frame.");
            if (Length < inDistance + inMinLenghtMoreThanDistance) throw new S2KHelperException($"Cannot break frame {Name}. It is too short!{Environment.NewLine}Requested Length: {inDistance}{Environment.NewLine}Frame Length: {Length}{inDistance}{Environment.NewLine}Frame Must Have: Length > Distance + {inMinLenghtMoreThanDistance}");

            bool brakeFromI = iEndPoint == inSapPoint;

            // Breaks the frame
            string[] newFrames = null;

            List<SapFrame> toRet;

            Stopwatch watch = null;
            if (ensureMinTime.HasValue)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            int ret = 0;
            ret = owner.SapApi.EditFrame.DivideAtDistance(Name, inDistance, brakeFromI, ref newFrames);

            if (ensureMinTime.HasValue)
            {
                watch.Stop();
                if (watch.ElapsedMilliseconds < ensureMinTime.Value) Thread.Sleep((int)ensureMinTime.Value - (int)watch.ElapsedMilliseconds);
            }

            if (ret != 0 ||
                newFrames == null ||
                newFrames.Length == 0)
            {
                // Checks if *really* the frame wasn't broken
                SapPoint pnt = owner.s2KModel.PointMan.GetByName(inSapPoint.Name);
                if (pnt.ConnectedFrames.Count != 2) throw new S2KHelperException($"Cannot break frame {Name}. SAP2000's function failed.");

                toRet = new List<SapFrame>(pnt.ConnectedFrames);
            }
            else
            {
                toRet = owner.GetByNames(newFrames.ToList());
            }

            if (!string.IsNullOrWhiteSpace(rename))
            {
                for (int i = 0; i < toRet.Count; i++)
                {
                    SapFrame frame = toRet[i];
                    frame.ChangeName($"{Name}{rename}{i}");
                }
            }

            return toRet;
        }
        /// <summary>
        /// Breaks the frame at an existing point. Note, the selections will be cleared out.
        /// </summary>
        /// <param name="inSapPoint">The point that will break the frame.</param>
        /// <param name="rename">Null if the broken pieces should not be renamed. Otherwise, it will rename with the pattern [parentName]+[this_string]+counter.</param>
        /// <param name="ensureMinTime">If set, will force this function to wait for the given milliseconds. This is stupid, but SAP2000 has something that if a loop breaks elements too fast, it stops working.</param>
        /// <returns>The list of new frames</returns>
        /// <exception cref="S2KHelperException">Thrown if the breaking of the frame fails.</exception>
        public List<SapFrame> DivideAtIntersectPoint(SapPoint inSapPoint, string rename = null, uint? ensureMinTime = null)
        {
            owner.s2KModel.ClearSelection();
            inSapPoint.Select();

            // Breaks the frame
            string[] newFrames = null;
            int countNewFrames = 0;

            List<SapFrame> toRet;

            Stopwatch watch = null;
            if (ensureMinTime.HasValue)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            int ret = 0;
            ret = owner.SapApi.EditFrame.DivideAtIntersections(Name, ref countNewFrames, ref newFrames);

            if (ensureMinTime.HasValue)
            {
                watch.Stop();
                if (watch.ElapsedMilliseconds < ensureMinTime.Value) Thread.Sleep( (int)ensureMinTime.Value - (int)watch.ElapsedMilliseconds);
            }

            if (ret != 0 ||
                newFrames == null ||
                newFrames.Length == 0)
            {
                // Checks if *really* the frame wasn't broken
                SapPoint pnt = owner.s2KModel.PointMan.GetByName(inSapPoint.Name);
                if (pnt.ConnectedFrames.Count != 2) throw new S2KHelperException($"Cannot break frame {Name}. SAP2000's function failed.");

                toRet = new List<SapFrame>(pnt.ConnectedFrames);
            }
            else
            {
                toRet = owner.GetByNames(newFrames.ToList());
            }

            if (!string.IsNullOrWhiteSpace(rename))
            {
                for (int i = 0; i < toRet.Count; i++)
                {
                    SapFrame frame = toRet[i];
                    frame.ChangeName($"{Name}{rename}{i}");
                }
            }

            return toRet;
        }

        /// <summary>
        /// Divides the frame by adding a short frame in it's middle.
        /// </summary>
        /// <param name="inDistanceFromI">The desired distance from the beginning of the frame in the middle.</param>
        /// <param name="desiredLenght">The desired length of the frame in the middle.</param>
        /// <param name="rename">The string that will be appended to the frame's name. If one string is given, the appended names will be numbered sequencially. If two strings are given, the first will be appended to the extreme frames and the second string will be appended to the frame in the middle.</param>
        /// <param name="inMinLenghtMoreThanDistance">The minimum length each section must have.</param>
        /// <param name="ensureMinTime">The minimum time the function shall take - SAP2000 things...</param>
        /// <returns>A list with the new frames and a reference to the frame in the middle.</returns>
        public (List<SapFrame> allFrames, SapFrame desired) DivideInsertNewFrameInMiddle(double inDistanceFromI, double desiredLenght, string[] rename = null, double inMinLenghtMoreThanDistance = 1, uint? ensureMinTime = null)
        {
            if (desiredLenght < inMinLenghtMoreThanDistance) throw new S2KHelperException($"Cannot break the frame. The desired length is too short.");
            if (Length < (inDistanceFromI + desiredLenght + inMinLenghtMoreThanDistance)) throw new S2KHelperException($"Cannot break the frame. The frame is too short to accomodate the DistanceFromI and the desired length.");
            if (rename != null && rename.Length > 2) throw new S2KHelperException($"The rename list can only contain either one or two values.");

            List<SapFrame> firstChunks = DivideAtDistanceFromPoint(iEndPoint, inDistanceFromI);

            List<SapFrame> secondChunks = firstChunks[1].DivideAtDistanceFromPoint(firstChunks[1].iEndPoint, desiredLenght);

            List<SapFrame> finalParts = new List<SapFrame>() { firstChunks[0], secondChunks[0], secondChunks[1] };

            if (rename != null)
            {
                if (rename.Length == 1)
                {
                    for (int i = 0; i < finalParts.Count; i++)
                    {
                        SapFrame frame = finalParts[i];
                        frame.ChangeName($"{Name}{rename[0]}{i}");
                    }
                }
                else
                {
                    finalParts[0].ChangeName($"{Name}{rename[0]}0");
                    finalParts[1].ChangeName($"{Name}{rename[1]}");
                    finalParts[2].ChangeName($"{Name}{rename[0]}1");
                }
            }

            return (finalParts, finalParts[1]);
        }

        /// <summary>
        /// Adds a point along the frame that is closest to the given point.
        /// If the closest point is iJoint or jJoint (within Sap's Merge Tolerance), it will return one of these two points without adding a new one to the model.
        /// If the given point is along the frame (within Sap's Merge Tolerance), the given point will be returned.
        /// </summary>
        /// <param name="inPoint">Point closest to the frame.</param>
        /// <param name="attemptName">The name to be given to the point.</param>
        /// <returns></returns>
        public SapPoint AddPointInFrameClosestToGiven(SapPoint inPoint, string attemptName = null)
        {
            // Gets the closest point to the closest frame
            Point3D point3DAtFrame = Line.ClosestPointTo(inPoint.Point, true);

            if (iEndPoint.Point.DistanceTo(point3DAtFrame) <= owner.s2KModel.MergeTolerance) return iEndPoint;
            if (jEndPoint.Point.DistanceTo(point3DAtFrame) <= owner.s2KModel.MergeTolerance) return jEndPoint;
            if (inPoint.Point.DistanceTo(point3DAtFrame) <= owner.s2KModel.MergeTolerance) return inPoint;

            // Adds the point to the model
            return owner.s2KModel.PointMan.AddByPoint3D_ReturnSapEntity(point3DAtFrame, attemptName);
        }

        public override bool Equals(object obj)
        {
            if (obj is SapFrame inFrame)
            {
                if (Name == inFrame.Name) return true;
                else return false;
            }

            return false;
        }
        public static bool operator ==(SapFrame lhs, SapFrame rhs)
        {

            // If left hand side is null...
            if (lhs is null)
            {
                // ...and right hand side is null...
                if (rhs is null)
                {
                    //...both are null and are Equal.
                    return true;
                }

                // ...right hand side is not null, therefore not Equal.
                return false;
            }

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SapFrame lhs, SapFrame rhs)
        {
            return !(lhs == rhs);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public SapFrame DuplicateReference()
        {
            return new SapFrame(Name, iEndPoint.DuplicateReference(), jEndPoint.DuplicateReference(), owner);
        }

        #region SAP2000 Handlers
        public bool SetLocalAxes(double Angle, ItemTypeEnum itemTypeEnum = ItemTypeEnum.Objects)
        {
            return owner.SapApi.FrameObj.SetLocalAxes(Name, Angle, (eItemType)(int)itemTypeEnum) == 0;
        }
        public bool SetAdvancedAxis(FrameAdvancedAxes_Plane2 plane, Vector3D vector)
        {
            int[] PlDir = new int[] { 1, 2 };
            string[] PlPt = new string[] { "", "" };
            double[] PlVect = new double[] { vector.Normalize().X, vector.Normalize().Y, vector.Normalize().Z };

            return owner.SapApi.FrameObj.SetLocalAxesAdvanced(Name, true, (int)plane,
                (int)AdvancedAxesAngle_Vector.UserVector, "GLOBAL", ref PlDir,
                ref PlPt, ref PlVect, eItemType.Objects) == 0;
        }
        public bool SetModifiers(double CrossArea, double Shear2, double Shear3, double Torsional, double Moment2, double Moment3, double Mass, double Weight)
        {
            double[] values = new double[] { CrossArea, Shear2, Shear3, Torsional, Moment2, Moment3, Mass, Weight };

            return 0 == owner.SapApi.FrameObj.SetModifiers(Name, ref values);
        }
        #endregion


    }
}
