using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapArea : SapObject
    {
        private AreaManager owner = null;

        private SapArea(string name, AreaManager areaManager) : base(name, SapObjectType.Area, areaManager)
        {
            owner = areaManager;
        }
        internal SapArea(string name, List<SapPoint> points, AreaManager areaManager) : this(name,areaManager)
        {
            Points = points;
        }

        public bool SetAdvancedAxis(AreaAdvancedAxes_Plane plane, Vector3D vector)
        {
            int[] PlDir = new int[] { 1,2 };
            string[] PlPt = new string[] { "", "" };
            double[] PlVect = new double[] { vector.Normalize().X, vector.Normalize().Y, vector.Normalize().Z };

            return owner.SapApi.AreaObj.SetLocalAxesAdvanced(Name, true, (int)plane,
                (int)AdvancedAxesAngle_Vector.UserVector, "GLOBAL", ref PlDir,
                ref PlPt, ref PlVect, SAP2000v1.eItemType.Objects) == 0;
        }

        public List<SapPoint> Points { get; private set; }

        public override bool ChangeConnectivity(SapPoint oldPoint, SapPoint newPoint)
        {
            if (!Points.Contains(oldPoint)) throw new S2KHelperException($"The old point must belong to the area!{Environment.NewLine}Point {oldPoint.Name} does not belong to the area {Name}.");

            List<SapPoint> newConn = new List<SapPoint>();

            foreach (SapPoint item in Points)
            {
                if (item == oldPoint) newConn.Add(newPoint);
                else newConn.Add(item);
            }

            string[] newConnArray = newConn.Select(a => a.Name).ToArray();

            return owner.SapApi.EditArea.ChangeConnectivity(Name, newConn.Count, ref newConnArray) == 0;
        }
    }
}
