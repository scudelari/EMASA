using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using MathNet.Spatial.Euclidean;
using MoreLinq;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class PointManager : SapManagerBase
    {
        internal PointManager(S2KModel model) : base(model) { }

        public SapPoint CreateSapPoint(string name, double x, double y, double z)
        {
            return new SapPoint(name, x, y, z, this);
        }
        public SapPoint CreateSapPoint(string name, Point3D inPnt)
        {
            return new SapPoint(name, inPnt, this);
        }

        public string AddByCoord(double X, double Y, double Z, string AttemptName = "", int? MergeNum = null)
        {
            string outName = null;

            int ret = SapApi.PointObj.AddCartesian(X, Y, Z, ref outName, AttemptName, "Global", 
                MergeNum.HasValue, // If a value is given, then SAP2000 will not merge the point automatically
                MergeNum ?? 0 );

            if (ret == 0)
            {
                // Checks if the buffer must be updated as well
                if (s2KModel.JointConstraintMan._bufferPointConstraintList != null)
                {
                    if (s2KModel.JointConstraintMan._bufferPointConstraintList.Any(a => a.Name == outName)) // Just in case that point already exists somehow in the list, we force a refresh
                        foreach (SapPoint item in s2KModel.JointConstraintMan._bufferPointConstraintList.Where(a => a.Name == outName)) item._jointConstraintNames = null;
                    else
                        s2KModel.JointConstraintMan._bufferPointConstraintList.Add(GetByName(outName));
                }

                return outName;
            }
            else
            {
                return null;
            }
        }
        public SapPoint AddByCoord_ReturnSapEntity(double X, double Y, double Z, string AttemptName = "", int? MergeNum = null)
        {
            string sapName = AddByCoord(X, Y, Z, AttemptName, MergeNum);

            if (!string.IsNullOrWhiteSpace(sapName))
            {
                return GetByName(sapName);
            }

            return null;
        }
        public string AddByPoint3D(Point3D pnt, string AttemptName = "", int? MergeNum = null)
        {
            return AddByCoord(pnt.X, pnt.Y, pnt.Z, AttemptName, MergeNum);
        }
        public SapPoint AddByPoint3D_ReturnSapEntity(Point3D pnt, string AttemptName = "", int? MergeNum = null)
        {
            return AddByCoord_ReturnSapEntity(pnt.X, pnt.Y, pnt.Z, AttemptName, MergeNum);
        }

        public SapPoint GetByName(string PointName)
        {
            double X = 0, Y = 0, Z = 0;
            int ret = SapApi.PointObj.GetCoordCartesian(PointName, ref X, ref Y, ref Z);

            if (ret != 0) return null;

            return new SapPoint(PointName, X, Y, Z, this);
        }

        public SapPoint GetClosestToCoordinate(Point3D Coordinates)
        {
            // Get all points
            List<SapPoint> allPoints = GetAll();

            if (allPoints.Count == 0) throw new S2KHelperException("There are no points in the model to get the closest by coordinate");

            SapPoint closest = allPoints.MinBy(a => a.Point.DistanceTo(Coordinates)).First();

            return closest;
        }

        [Obsolete]
        public List<SapPoint> GetSelected(BusyOverlay BusyOverlay)
        {
            if (BusyOverlay != null) BusyOverlay.SetDeterminate($"Getting selected joints from SAP2000.", "Joint");

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            if (SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames) != 0)
                throw new S2KHelperException("Could not get the selected joints from SAP2000.");
            
            if (count == 0)
            {
                if (BusyOverlay != null) BusyOverlay.Stop();
                return new List<SapPoint>();
            }

            // Declares the return
            List<SapPoint> toReturn = new List<SapPoint>();

            int currType = 0;
            int typeCount = objectType.Count(a => a == (int)SelectObjectType.PointObject);
            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.PointObject) // Point Object
                {
                    // Gets the coordinates of the point
                    double X = 0;
                    double Y = 0;
                    double Z = 0;

                    SapApi.PointObj.GetCoordCartesian(selectedNames[i], ref X, ref Y, ref Z);
                    toReturn.Add(new SapPoint(selectedNames[i], X, Y, Z, this));

                    if (BusyOverlay != null) BusyOverlay.UpdateProgress(currType, typeCount, selectedNames[i]);
                    currType++;
                }
            }

            BusyOverlay.Stop();
            return toReturn;
        }
        public List<SapPoint> GetSelected(bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"SAP2000: Getting selected joints.", "Joint");

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            if (SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames) != 0)
                throw new S2KHelperException("Could not get the selected joints from SAP2000.");

            if (count == 0)
            {
                return new List<SapPoint>();
            }

            // Declares the return
            List<SapPoint> toReturn = new List<SapPoint>();

            int currType = 0;
            int typeCount = objectType.Count(a => a == (int)SelectObjectType.PointObject);
            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.PointObject) // Point Object
                {
                    // Gets the coordinates of the point
                    double X = 0;
                    double Y = 0;
                    double Z = 0;

                    SapApi.PointObj.GetCoordCartesian(selectedNames[i], ref X, ref Y, ref Z);
                    toReturn.Add(new SapPoint(selectedNames[i], X, Y, Z, this));

                    if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(currType, typeCount, selectedNames[i]);
                    currType++;
                }
            }
            return toReturn;
        }

        public List<SapPoint> GetAll(bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate($"SAP2000: Getting all joints - Name List.");

            int count = 0;
            string[] names = null;

            if (SapApi.PointObj.GetNameList(ref count, ref names) != 0)
                throw new S2KHelperException("Could not get all joints from SAP2000.");

            if (count == 0)
            {
                return new List<SapPoint>();
            }

            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"SAP2000: Getting all joints - Coordinates.", "Joint");

            // Declares the return
            List<SapPoint> toReturn = new List<SapPoint>();

            for (int i = 0; i < count; i++)
            {
                // Gets the coordinates of the point
                double X = 0;
                double Y = 0;
                double Z = 0;

                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, count, names[i]);

                SapApi.PointObj.GetCoordCartesian(names[i], ref X, ref Y, ref Z);
                toReturn.Add(new SapPoint(names[i], X, Y, Z, this));
            }

            return toReturn;
        }

        public List<SapPoint> GetGroup(string inGroupName, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"Getting joints that belong to group {inGroupName} from SAP2000.", "Joint");

            int numberItems = 0;
            int[] objectTypes = null;
            string[] names = null;

            int ret = SapApi.GroupDef.GetAssignments(inGroupName, ref numberItems, ref objectTypes, ref names);
             
            if (0 != ret) throw new S2KHelperException($"Could not get joints of group {inGroupName}. Are you sure the group exists?");

            if (numberItems == 0)
            {
                if (inUpdateInterface) BusyOverlayBindings.I.Stop();
                return new List<SapPoint>();
            }

            List<SapPoint> toRet = new List<SapPoint>();
            for (int i = 0; i < numberItems; i++)
            {
                string pointName = names[i];

                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, numberItems, pointName);

                if ((SapObjectType)objectTypes[i] == SapObjectType.Point) toRet.Add(GetByName(pointName));
            }

            if (inUpdateInterface) BusyOverlayBindings.I.Stop();
            return toRet;
        }

        /// <summary>
        /// Selects a point.
        /// </summary>
        /// <param name="pointName">The name of the point to select.</param>
        /// <returns>True if the point was selected. False if the point was not selected.</returns>
        public bool SelectElements(string pointName)
        {
            return SapApi.PointObj.SetSelected(pointName, true, eItemType.Objects) == 0;
        }
        /// <summary>
        /// Selects a point.
        /// </summary>
        /// <param name="point">The point to select.</param>
        /// <returns>True if the point was selected. False if the point was not selected.</returns>
        public bool SelectElements(SapPoint point)
        {
            return SelectElements(point.Name);
        }
        /// <summary>
        /// Selects an array of point.
        /// </summary>
        /// <param name="pointNames">The names of the points to select.</param>
        /// <returns>True if all were selected. False if at least one wasn't selected.</returns>
        public bool SelectElements(string[] pointNames, IProgress<ProgressData> ReportProgress = null)
        {
            if (pointNames == null || pointNames.Length == 0) return false;

            bool toRet = true;
            for (int i = 0; i < pointNames.Length; i++)
            {
                string item = pointNames[i];

                if (!SelectElements(item)) toRet = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,pointNames.Length));
            }
            return toRet;
        }

        public bool SelectElements(List<SapPoint> inPoints, IProgress<ProgressData> ReportProgress = null)
        {
            if (inPoints == null || inPoints.Count == 0) return false;

            bool toRet = true;
            for (int i = 0; i < inPoints.Count; i++)
            {
                string item = inPoints[i].Name;

                if (!SelectElements(item)) toRet = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,inPoints.Count));
            }
            return toRet;
        }

        public bool DeletePoint(SapPoint ToDelete)
        {
            if (ToDelete.Special) ToDelete.Special = false;
            int ret = SapApi.PointObj.DeleteSpecialPoint(ToDelete.Name, eItemType.Objects);
            return 0 == ret;
        }
        public void DeleteAllFreePoints(IProgress<ProgressData> ReportProgress = null)
        {
            List<SapPoint> allPoints = GetAll();
            for (int i = 0; i < allPoints.Count; i++)
            {
                SapPoint item = allPoints[i];
                DeletePoint(item);
                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i, allPoints.Count));
            }
        }
    }
}
