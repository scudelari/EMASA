using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Sap2000Library.DataClasses;
using Sap2000Library.Managers;
using SAP2000v1;

namespace Sap2000Library.SapObjects
{
    public class SapPoint : SapObject
    {
        private PointManager owner = null;
        private SapPoint(string name, PointManager pointManager) : base(name, SapObjectType.Point, pointManager)
        {
            owner = pointManager;
        }
        internal SapPoint(string name, double x, double y, double z, PointManager pointManager) : this(name, pointManager)
        {
            Point = new Point3D(x, y, z);
        }
        internal SapPoint(string name, Point3D inPnt, PointManager pointManager) : this(name, pointManager)
        {
            Point = inPnt;
        }

        public Point3D Point;
        public bool Found = false;

        public double X
        {
            get
            {
                return Point.X;
            }
            set
            {
                Point = new Point3D(value, Point.Y, Point.Z);
            }
        }
        public double Y
        {
            get
            {
                return Point.Y;
            }
            set
            {
                Point = new Point3D(Point.X, value, Point.Z);
            }
        }
        public double Z
        {
            get
            {
                return Point.Z;
            }
            set
            {
                Point = new Point3D(Point.X, Point.Y, value);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        internal List<string> _jointConstraintNames = null;
        public List<string> JointConstraintNames
        {
            get
            {
                if (_jointConstraintNames == null)
                {
                    int numberItems = 0;
                    string[] pointName = { Name };
                    string[] constName = null;

                    int ret = owner.SapApi.PointObj.GetConstraint(Name, ref numberItems, ref pointName, ref constName);

                    // Sets the name into the object
                    if (ret != 0) return null;


                    _jointConstraintNames = new List<string>();
                    if (constName == null) return _jointConstraintNames;

                    foreach (string item in constName)
                    {
                        _jointConstraintNames.Add(item);
                    }
                }

                return _jointConstraintNames;
            }
        }
        public bool AddJointConstraint(string ConstraintName, bool ReplaceExistingConstraints = true)
        {
            bool ret = owner.SapApi.PointObj.SetConstraint(Name, ref ConstraintName, eItemType.Objects, ReplaceExistingConstraints) == 0;

            if (ret)
            {
                // Forces a refresh of the JointConstraints
                _jointConstraintNames = null;

                // Checks if the buffer must be updated as well
                if (owner.s2KModel.JointConstraintMan._bufferPointConstraintList != null)
                {
                    foreach (SapPoint item in owner.s2KModel.JointConstraintMan._bufferPointConstraintList.Where(a => a.Name == Name))
                    {
                        item._jointConstraintNames = null;
                    }
                }
            }

            return ret;
        }
        public bool RemoveAllJointConstraints()
        {
            bool ret = owner.SapApi.PointObj.DeleteConstraint(Name, eItemType.Objects) == 0;

            if (ret)
            {
                // Forces a refresh of the JointConstraints
                _jointConstraintNames = null;
            }

            return ret;
        }

        private List<PointGroundDisplacementLoad> _pntGrnDispLoad = null;
        public List<PointGroundDisplacementLoad> GroundDisplacementLoadList
        {
            get
            {
                if (_pntGrnDispLoad == null)
                {
                    int numberItems = 0;
                    string[] pointName = null;
                    string[] loadPat = null;
                    int[] LCStep = null;
                    string[] CSys = null;
                    double[] U1 = null;
                    double[] U2 = null;
                    double[] U3 = null;
                    double[] R1 = null;
                    double[] R2 = null;
                    double[] R3 = null;

                    int ret = owner.SapApi.PointObj.GetLoadDispl(Name, ref numberItems, ref pointName, ref loadPat, ref LCStep, ref CSys,
                        ref U1, ref U2, ref U3, ref R1, ref R2, ref R3);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _pntGrnDispLoad = new List<PointGroundDisplacementLoad>();

                    for (int i = 0; i < numberItems; i++)
                    {
                        _pntGrnDispLoad.Add(new PointGroundDisplacementLoad
                        {
                            CSys = CSys[i],
                            LCStep = LCStep[i],
                            LoadPattern = loadPat[i],
                            U1 = U1[i],
                            U2 = U2[i],
                            U3 = U3[i],
                            R1 = R1[i],
                            R2 = R2[i],
                            R3 = R3[i]
                        });
                    }
                }

                return _pntGrnDispLoad;
            }
        }

        private List<PointForceLoad> _pntForceLoad = null;
        public List<PointForceLoad> ForceLoadList
        {
            get
            {
                if (_pntForceLoad == null)
                {
                    int numberItems = 0;
                    string[] pointName = null;
                    string[] loadPat = null;
                    int[] LCStep = null;
                    string[] CSys = null;
                    double[] F1 = null;
                    double[] F2 = null;
                    double[] F3 = null;
                    double[] M1 = null;
                    double[] M2 = null;
                    double[] M3 = null;

                    int ret = owner.SapApi.PointObj.GetLoadForce(Name, ref numberItems, ref pointName, ref loadPat, ref LCStep, ref CSys,
                        ref F1, ref F2, ref F3, ref M1, ref M2, ref M3);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _pntForceLoad = new List<PointForceLoad>();

                    for (int i = 0; i < numberItems; i++)
                    {
                        _pntForceLoad.Add(new PointForceLoad
                        {
                            CSys = CSys[i],
                            LCStep = LCStep[i],
                            LoadPattern = loadPat[i],
                            F1 = F1[i],
                            F2 = F2[i],
                            F3 = F3[i],
                            M1 = M1[i],
                            M2 = M2[i],
                            M3 = M3[i]
                        });
                    }
                }

                return _pntForceLoad;
            }
        }

        private List<JointDisplacementLoad> _pntDispLoad = null;
        public List<JointDisplacementLoad> DisplacementLoadList
        {
            get
            {
                if (_pntDispLoad == null)
                {
                    int numberItems = 0;
                    string[] pointName = null;
                    string[] loadPat = null;
                    int[] LCStep = null;
                    string[] CSys = null;
                    double[] U1 = null;
                    double[] U2 = null;
                    double[] U3 = null;
                    double[] R1 = null;
                    double[] R2 = null;
                    double[] R3 = null;

                    int ret = owner.SapApi.PointObj.GetLoadDispl(Name, ref numberItems, ref pointName, ref loadPat, ref LCStep, ref CSys,
                        ref U1, ref U2, ref U3, ref R1, ref R2, ref R3);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _pntDispLoad = new List<JointDisplacementLoad>();

                    for (int i = 0; i < numberItems; i++)
                    {
                        _pntDispLoad.Add(new JointDisplacementLoad
                        {
                            CSys = CSys[i],
                            LCStep = LCStep[i],
                            LoadPatternName = loadPat[i],
                            U1 = U1[i],
                            U2 = U2[i],
                            U3 = U3[i],
                            R1 = R1[i],
                            R2 = R2[i],
                            R3 = R3[i]
                        });
                    }
                }

                return _pntDispLoad;
            }
        }
        public void AddDisplacementLoad(JointDisplacementLoad jointDisplacementLoad, bool replace = true)
        {
            double[] vals = jointDisplacementLoad.Values;

            if (0 != owner.SapApi.PointObj.SetLoadDispl(Name, jointDisplacementLoad.LoadPatternName, ref vals, replace, jointDisplacementLoad.CSys))
                throw new S2KHelperException($"Could not add displacement loads for joint called {Name}.");
        }

        private PointBasicLocalAxesDef _basicLocalAxes = null;
        public PointBasicLocalAxesDef BasicLocalAxes
        {
            get
            {
                if (_basicLocalAxes == null)
                {
                    double A = 0;
                    double B = 0;
                    double C = 0;
                    bool Advanced = false;

                    int ret = owner.SapApi.PointObj.GetLocalAxes(Name, ref A, ref B, ref C, ref Advanced);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _basicLocalAxes =  new PointBasicLocalAxesDef { A = A, B = B, C = C, Advanced = Advanced};
                }

                return _basicLocalAxes;
            }
            set
            {
                PointBasicLocalAxesDef objective = value ?? PointBasicLocalAxesDef.Default;

                int ret = owner.SapApi.PointObj.SetLocalAxes(Name, value.A, value.B, value.C);

                if (ret != 0) throw new S2KHelperException($"Could not set basic local axes for point {Name}.");

                if (!value.Advanced) AdvancedLocalAxes = null;

                _basicLocalAxes = objective;
            }
        }

        private Matrix<double> _globalPointMatrix = null;
        public Matrix<double> ToGlobalTransformationMatrix
        {
            get
            {
                if (_globalPointMatrix != null) return _globalPointMatrix;

                double[] values = null;
                if (0 != owner.SapApi.PointObj.GetTransformationMatrix(Name, ref values)) throw new S2KHelperException($"Could not get transformation matrix of point {Name}.");

                _globalPointMatrix = Matrix<double>.Build.Dense(3, 3);
                _globalPointMatrix[0, 0] = values[0];
                _globalPointMatrix[0, 1] = values[1];
                _globalPointMatrix[0, 2] = values[2];
                _globalPointMatrix[1, 0] = values[3];
                _globalPointMatrix[1, 1] = values[4];
                _globalPointMatrix[1, 2] = values[5];
                _globalPointMatrix[2, 0] = values[6];
                _globalPointMatrix[2, 1] = values[7];
                _globalPointMatrix[2, 2] = values[8];

                return _globalPointMatrix;
            }
        }
        public Matrix<double> ToLocalTransformationMatrix
        {
            get
            {
                return ToGlobalTransformationMatrix.Inverse();
            }
        }
        public CoordinateSystem LocalCoordinateSystem
        {
            get
            {
                Vector<double> xVec = ToGlobalTransformationMatrix.Multiply(new Vector3D(1, 0, 0).ToVector());
                Vector<double> yVec = ToGlobalTransformationMatrix.Multiply(new Vector3D(0, 1, 0).ToVector());
                Vector<double> zVec = ToGlobalTransformationMatrix.Multiply(new Vector3D(0, 0, 1).ToVector());

                return new CoordinateSystem(Point3D.Origin, 
                    new Vector3D(xVec[0], xVec[1], xVec[2]),
                    new Vector3D(yVec[0], yVec[1], yVec[2]),
                    new Vector3D(zVec[0], zVec[1], zVec[2]) );
            }
        }

        private PointAdvancedLocalAxesDef _advancedLocalAxes = null;
        public PointAdvancedLocalAxesDef AdvancedLocalAxes
        {
            get
            {
                if (_advancedLocalAxes == null)
                {
                    bool Active = false;
                    int AxVectOpt = 0;
                    string AxCSys = null;
                    int[] AxDir = null;
                    string[] AxPt = null;
                    double[] AxVect = null;
                    int Plane2 = 0;
                    int PlVectOpt = 0;
                    string PlCSys = null;
                    int[] PlDir = null;
                    string[] PlPt = null;
                    double[] PlVect = null;

                    int ret = owner.SapApi.PointObj.GetLocalAxesAdvanced(Name, ref Active,
                        ref AxVectOpt, ref AxCSys, ref AxDir, ref AxPt, ref AxVect, ref Plane2,
                        ref PlVectOpt, ref PlCSys, ref PlDir, ref PlPt, ref PlVect);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _advancedLocalAxes = new PointAdvancedLocalAxesDef()
                    {
                        Active = Active,
                        AxVectOpt = (AdvancedAxesAngle_Vector)AxVectOpt,
                        AxCSys = AxCSys,
                        AxDir_int = AxDir,
                        AxPt = AxPt,
                        AxVect = AxVect,
                        Plane2 = (PointAdvancedAxes_Plane2)Plane2,
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
                PointAdvancedLocalAxesDef objective = value ?? PointAdvancedLocalAxesDef.NotSet;

                int ret = owner.SapApi.PointObj.SetLocalAxesAdvanced(Name, objective.Active,
                    objective.AxVectOpt_int, objective.AxCSys, ref objective.AxDir_int, ref objective.AxPt, ref objective.AxVect,
                    objective.Plane2_int,
                    objective.PlVectOpt_int, objective.PlCSys, ref objective.PlDir_int, ref objective.PlPt, ref objective.PlVect);

                if (ret != 0) throw new S2KHelperException($"Could not set the Advanced Local Axes to point {Name}.");

                _advancedLocalAxes = objective;
            }
        }
        public void SetAdvancedLocalAxesFromCoordinateSystem(CoordinateSystem inSys, PointAdvancedAxes_Plane2 refPlane = PointAdvancedAxes_Plane2.Plane12)
        {
            PointAdvancedLocalAxesDef localAxesDef = new PointAdvancedLocalAxesDef()
            {
                Active = true,
                AxVectOpt = AdvancedAxesAngle_Vector.UserVector,
                PlVectOpt = AdvancedAxesAngle_Vector.UserVector,
                Plane2 = refPlane,
                AxVect_Vector = inSys.XAxis,
                PlVect_Vector = inSys.YAxis
            };

            // Resets the local axes defs
            BasicLocalAxes = PointBasicLocalAxesDef.Default;

            // Sets the advanced 
            AdvancedLocalAxes = localAxesDef;
        }

        private double[] _mass = null;
        public double[] Mass
        {
            get
            {
                if (_mass == null)
                {
                    double[] mass = null;

                    int ret = owner.SapApi.PointObj.GetMass(Name, ref mass);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _mass = mass;
                }

                return _mass;
            }
        }

        private int? _mergeNumber = null;
        public int? MergeNumber
        {
            get
            {
                if (_mergeNumber == null)
                {
                    int mergeNum = 0;

                    int ret = owner.SapApi.PointObj.GetMergeNumber(Name, ref mergeNum);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _mergeNumber = mergeNum;
                }

                return _mergeNumber;
            }

            set
            {
                if (!value.HasValue) throw new S2KHelperException("Cannot set to null");

                int ret = owner.SapApi.PointObj.SetMergeNumber(Name, value.Value);

                if (0 != owner.SapApi.PointObj.SetMergeNumber(Name, value.Value)) throw new S2KHelperException($"Could not change the merge number of Joint {Name}.");

                _mergeNumber = value;
            }
        }

        private PointPanelZoneDef _pointPanelZone = null;
        public PointPanelZoneDef PointPanelZone
        {
            get
            {
                if (_pointPanelZone == null)
                {
                    int PropType = 0;
                    double Thickness = 0;
                    double K1 = 0;
                    double K2 = 0;
                    string LinkProp = null;
                    int Connectivity = 0;
                    int LocalAxisFrom = 0;
                    double LocalAxisAngle = 0;

                    int ret = owner.SapApi.PointObj.GetPanelZone(Name, ref PropType, ref Thickness,
                        ref K1, ref K2, ref LinkProp, ref Connectivity, ref LocalAxisFrom, ref LocalAxisAngle);

                    // Sets the name into the object
                    if (ret != 0) return null;

                    _pointPanelZone = new PointPanelZoneDef
                    {
                        PropType = (PointPanelZone_PropType)PropType,
                        Thickness = Thickness,
                        K1 = K1,
                        K2 = K2,
                        LinkProp = LinkProp,
                        Connectivity = (PointPanelZone_Connectivity)Connectivity,
                        LocalAxisFrom = (PointPanelZone_LocalAxisFrom)LocalAxisFrom,
                        LocalAxisAngle = LocalAxisAngle
                    };
                }

                return _pointPanelZone;
            }
        }

        private List<PointPattern> _pointPattern = null;
        public List<PointPattern> PointPatternValue
        {
            get
            {
                if (_pointPattern == null)
                {
                    // Gets list of Point Patterns

                    List<string> pats = owner.s2KModel.GetAllJointPatterns();

                    _pointPattern = new List<PointPattern>();

                    foreach (var item in pats)
                    {
                        double val = 0;
                        int ret = owner.SapApi.PointObj.GetPatternValue(Name, item, ref val);
                        if (ret != 0) return null;

                        _pointPattern.Add(new PointPattern { PatternName = item, Value = val });
                    }
                }

                return _pointPattern;
            }
        }

        /// <summary>
        /// Moves the point to the new coordinate. **** THE SAP2000 FUNCTION IS BUGGED ChangeCoordinates_1 ****
        /// </summary>
        /// <param name="newCoordinates">The new position of the point.</param>
        /// <param name="UsingMove">True if the EditGeneral.Move will be used. Otherwise, will use the EditPoint.ChangeCoordinates_1</param>
        /// <returns></returns>
        public void MoveTo(Point3D newCoordinates, bool UsingMove = true)
        {
            if (UsingMove)
            {
                owner.s2KModel.ClearSelection();
                owner.SapApi.PointObj.SetSelected(Name, true);

                Vector3D displacement = newCoordinates - Point;
                if (displacement.Length < owner.s2KModel.MergeTolerance) throw new S2KHelperException($"Could not move point {Name} to coordinates {newCoordinates.ToString()}.");

                int ret = owner.SapApi.EditGeneral.Move(displacement.X, displacement.Y, displacement.Z);
                if (ret != 0) throw new S2KHelperException($"Could not move point {Name} to coordinates {newCoordinates.ToString()}.");
                return ;
            }
            else
            {
                int ret = owner.SapApi.EditPoint.ChangeCoordinates_1(Name, newCoordinates.X, newCoordinates.Y, newCoordinates.Z, false);
                if (ret != 0) throw new S2KHelperException($"Could not move point {Name} to coordinates {newCoordinates.ToString()}.");
            }
        }

        /// <summary>
        /// Copies the point to the new location. Will also copy all group definitions!
        /// </summary>
        /// <param name="inDeltas">The deltas that will function as destination</param>
        /// <returns>The new point.</returns>
        public SapPoint CopyTo(Vector3D inDeltas)
        {
            List<SapPoint> newPoints = ReplicateLinear(inDeltas, 1);

            if (newPoints.Count == 1) return newPoints.First();

            return null;
        }

        public List<SapPoint> ReplicateLinear(Vector3D inDeltas, int inCount)
        {
            owner.s2KModel.SaveSelectionCache($"Point.Replicate.{Name}");
            owner.s2KModel.ClearSelection();

            Select();

            int numberItems = 0;
            string[] names = null;
            int[] types = null;

            // Now, replicates
            int ret = owner.SapApi.EditGeneral.ReplicateLinear(inDeltas.X, inDeltas.Y, inDeltas.Z, inCount, ref numberItems, ref names, ref types, false);
            if (ret != 0)
                throw new S2KHelperException($"Could not replicate point {Name}.");

            List<SapPoint> toRet = new List<SapPoint>();

            // If it returns 0 but names = null, the new position already contains a point. Acquires that point.
            if (names == null)
            {
                toRet.Add(owner.GetClosestToCoordinate(Point.AddVector(inDeltas)));
            }
            else
            {
                foreach (string name in names)
                {
                    SapPoint pnt = owner.GetByName(name);
                    pnt.CopyGroupsFrom(this);
                    toRet.Add(pnt);
                }
            }

            return toRet;
        }

        private PointRestraintDef _restraints;
        public PointRestraintDef Restraints
        {
            get
            {
                if (_restraints != null) return _restraints;

                bool[] values = null;

                int ret = owner.SapApi.PointObj.GetRestraint(Name, ref values);

                if (0 != ret) return null;

                _restraints = new PointRestraintDef(values);
                return _restraints;
            }
            set
            {
                bool[] restraintValues;

                if (value == null) restraintValues = new bool[] { false, false, false, false, false, false };
                else restraintValues = value.Values;

                int ret = owner.SapApi.PointObj.SetRestraint(Name, ref restraintValues);

                if (ret != 0) throw new S2KHelperException($"Could not change the joint's restraints for joint: {Name}.");

                _restraints = value;
            }
        }

        private List<SapObject> _connectedElements = null;
        /// <summary>
        /// This returns all elements that are directly connected by the joint.
        /// </summary>
        public List<SapObject> ConnectedElements
        {
            get
            {
                if (_connectedElements != null) return _connectedElements;

                int numberItems = 0;
                int[] types = null;
                string[] names = null;
                int[] numberWithinObject = null;

                int ret = owner.SapApi.PointObj.GetConnectivity(Name, ref numberItems, ref types, ref names, ref numberWithinObject);

                if (ret != 0) return null;

                List<SapObject> tempList = new List<SapObject>();

                for (int i = 0; i < numberItems; i++)
                {
                    switch ((SapObjectType)types[0])
                    {
                        case SapObjectType.Point:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                        case SapObjectType.Frame:
                            tempList.Add(owner.s2KModel.FrameMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Link:
                            tempList.Add(owner.s2KModel.LinkMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Cable:
                            tempList.Add(owner.s2KModel.CableMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Area:
                            tempList.Add(owner.s2KModel.AreaMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Solid:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                        case SapObjectType.Tendon:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                    }
                }

                _connectedElements = tempList;
                return _connectedElements;
            }
        }

        private List<SapFrame> _connectedFrames = null;
        /// <summary>
        /// This returns all frames that are directly connected by the joint.
        /// </summary>
        public List<SapFrame> ConnectedFrames
        {
            get
            {
                if (_connectedFrames != null) return _connectedFrames;

                int numberItems = 0;
                int[] types = null;
                string[] names = null;
                int[] numberWithinObject = null;

                int ret = owner.SapApi.PointObj.GetConnectivity(Name, ref numberItems, ref types, ref names, ref numberWithinObject);

                if (ret != 0) return null;

                List<SapFrame> tempList = new List<SapFrame>();

                for (int i = 0; i < numberItems; i++)
                {
                    switch ((SapObjectType)types[0])
                    {
                        case SapObjectType.Point:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                        case SapObjectType.Frame:
                            tempList.Add(owner.s2KModel.FrameMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Link:
                            //tempList.Add(this.owner.s2KModel.LinkMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Cable:
                            //tempList.Add(this.owner.s2KModel.CableMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Area:
                            //tempList.Add(this.owner.s2KModel.AreaMan.GetByName(names[i]));
                            break;
                        case SapObjectType.Solid:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                        case SapObjectType.Tendon:
                            throw new S2KHelperException($"Type {((SapObjectType)types[0]).ToString()} is still not supported in this method. Please write the code.");
                    }
                }

                _connectedFrames = tempList;
                return _connectedFrames;
            }
        }


        public bool HasConnections
        {
            get
            {
                int numberItems = 0;
                int[] types = null;
                string[] names = null;
                int[] numberWithinObject = null;

                int ret = owner.SapApi.PointObj.GetConnectivity(Name, ref numberItems, ref types, ref names, ref numberWithinObject);

                if (ret != 0) throw new S2KHelperException($"Could not get the list of connected elements to point {Name}.");
                if (numberItems != 0) return true;

                numberItems = 0;
                string[] pointName = { Name };
                string[] constName = null;

                ret = owner.SapApi.PointObj.GetConstraint(Name, ref numberItems, ref pointName, ref constName);

                // Sets the name into the object
                if (ret != 0) throw new S2KHelperException($"Could not get the list of constraints at point {Name}.");
                if (numberItems != 0) return true;

                // Not connected nor with constraints
                return false;
            }
        }

        /// <summary>
        /// This function will use the buffer in the JointConstraintManager. You must initialize it. It only considers first-level constraints.
        /// </summary>
        /// <returns>The list of linked frames also considering the constraints.</returns>
        public List<SapFrame> GetAllConnectedFramesAlsoLinkedByConstraints()
        {
            List<SapFrame> ConnFrames = new List<SapFrame>(ConnectedFrames);

            foreach (string constraint in JointConstraintNames)
            {
                // Gets all points with this constraint
                List<SapPoint> sharingConstraint = owner.s2KModel.JointConstraintMan.GetPointsWithConstraintFromPointBuffer(constraint);

                foreach (SapPoint point in sharingConstraint)
                {
                    // Ignores, obviously, the current point
                    if (point.Name == Name) continue;

                    ConnFrames.AddRange(point.ConnectedFrames);
                }
            }

            return ConnFrames;
        }

        public bool Special
        {
            get
            {
                bool isSpecial = false;
                int ret = owner.SapApi.PointObj.GetSpecialPoint(Name, ref isSpecial);
                if (ret != 0) throw new S2KHelperException($"Could not get Special Point information for point {Name}.");
                return isSpecial;
            }
            set
            {
                int ret = owner.SapApi.PointObj.SetSpecialPoint(Name, value, eItemType.Objects);
                if (ret != 0) throw new S2KHelperException($"Could not set Special Point information for point {Name}.");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SapPoint inPoint)
            {
                if (Name == inPoint.Name) return true;
                else return false;
            }

            return false;
        }
        public static bool operator ==(SapPoint lhs, SapPoint rhs)
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
        public static bool operator !=(SapPoint lhs, SapPoint rhs)
        {
            return !(lhs == rhs);
        }
        public override int GetHashCode()
        {
            return (Name, X, Y, Z).GetHashCode();
        }

        /// <summary>
        /// Attention! Does not add to SAP2000.
        /// </summary>
        /// <returns>A new reference to this point.</returns>
        public SapPoint DuplicateReference()
        {
            return new SapPoint(Name, new Point3D(Point.X, Point.Y, Point.Z), owner);
        }
        /// <summary>
        /// Attention! Does not add to SAP2000.
        /// </summary>
        /// <param name="inName">The name to be used at the reference. Caution!</param>
        /// <returns>A new point reference with a different name.</returns>
        public SapPoint DuplicateCoordinatesWithNewName(string inName)
        {
            return new SapPoint(inName, new Point3D(Point.X, Point.Y, Point.Z), owner);
        }
        public string SQLite_InsertStatement
        {
            get
            {
                return $@"INSERT INTO JointCoordinates (Name , X , Y , Z) VALUES('{Name ?? "NULL"}',{Point.X}, {Point.Y}, {Point.Z});";
            }
        }
        public static string SQLite_CreateCoordinateTableStatement
        {
            get
            {
                return @"CREATE TABLE JointCoordinates ( Name TEXT, X DOUBLE, Y DOUBLE, Z DOUBLE );";
            }
        }
    }
    public class SapPointEqualityComparerByCoordinates : IEqualityComparer<SapPoint>
    {
        public bool Equals(SapPoint x, SapPoint y)
        {
            return x.Point.Equals(y);
        }

        public int GetHashCode(SapPoint obj)
        {
            // Makes a hashcode using a tuple
            return (obj.X, obj.Y, obj.Z).GetHashCode();
        }
    }
}
