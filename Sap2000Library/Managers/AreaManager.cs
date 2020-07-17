using System;
using System.Collections.Generic;
using System.Linq;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class AreaManager : SapManagerBase
    {
        internal AreaManager(S2KModel model) : base(model) { }

        public SapArea GetByName(string areaName)
        {
            // Gets the points of the area
            int pointCount = 0;
            string[] pointNames = null;

            if (SapApi.AreaObj.GetPoints(areaName, ref pointCount, ref pointNames) != 0) return null;

            List<SapPoint> points = new List<SapPoint>();
            foreach (string pntName in pointNames)
            {
                points.Add(s2KModel.PointMan.GetByName(pntName));
            }

            return new SapArea(areaName, points, this);
        }

        public List<SapArea> GetAll(IProgress<ProgressData> ReportProgress = null)
        {
            // Gets all names
            int count = 0;
            string[] areaNames = null;

            if (SapApi.AreaObj.GetNameList(ref count, ref areaNames) != 0) return null;

            List<SapArea> toReturn = new List<SapArea>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(areaNames[i]));

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,count));
            }

            return toReturn;
        }
        public List<SapArea> GetSelected(IProgress<ProgressData> ReportProgress = null)
        {
            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapArea>();

            // Gets count of desired element types
            int typeCount = (from a in objectType
                             where a == (int)SelectObjectType.AreaObject
                             select a).Count();
            int currType = 0;

            // Declares the return
            List<SapArea> toReturn = new List<SapArea>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.AreaObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(++currType,count));
                }
            }

            return toReturn;
        }

        public bool SelectElements(string areaName)
        {
            return SapApi.AreaObj.SetSelected(areaName, true, eItemType.Objects) == 0;
        }
        public bool SelectElements(SapArea area)
        {
            return SelectElements(area.Name);
        }
        public bool SelectElements(List<SapArea> areas, IProgress<ProgressData> ReportProgress = null)
        {
            bool allSelected = true;
            for (int i = 0; i < areas.Count; i++)
            {
                SapArea area = areas[i];
                if (!SelectElements(area.Name)) allSelected = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,areas.Count));
            }
            return allSelected;
        }
    }
}
