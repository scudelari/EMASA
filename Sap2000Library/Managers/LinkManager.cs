using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class LinkManager : SapManagerBase
    {
        internal LinkManager(S2KModel model) : base(model) { }

        public SapLink GetByName(string linkName)
        {
            string iPoint = null;
            string jPoint = null;

            if (SapApi.LinkObj.GetPoints(linkName, ref iPoint, ref jPoint) != 0) return null;

            return new SapLink(linkName, s2KModel.PointMan.GetByName(iPoint), s2KModel.PointMan.GetByName(jPoint), this);
        }

        public List<SapLink> GetAll(IProgress<ProgressData> ReportProgress = null)
        {
            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.LinkObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapLink> toReturn = new List<SapLink>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,count));
            }

            return toReturn;
        }
        [Obsolete]
        public List<SapLink> GetSelected(IProgress<ProgressData> ReportProgress = null)
        {
            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapLink>();

            // Gets count of desired element types
            int typeCount = (from a in objectType
                             where a == (int)SelectObjectType.LinkObject
                             select a).Count();
            int currType = 0;

            // Declares the return
            List<SapLink> toReturn = new List<SapLink>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.LinkObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(++currType,count));
                }
            }

            return toReturn;
        }
        public List<SapLink> GetSelected(BusyOverlay BusyOverlay)
        {
            //if (BusyOverlay != null) BusyOverlay.SetDeterminate($"Getting selected links from SAP2000.", "Link");

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapLink>();

            // Gets count of desired element types
            int currType = 0;
            int typeCount = objectType.Count(a => a == (int)SelectObjectType.LinkObject);
            // Declares the return
            List<SapLink> toReturn = new List<SapLink>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.LinkObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    //if (BusyOverlay != null) BusyOverlay.UpdateProgress(currType, typeCount, selectedNames[i]);
                    currType++;
                }
            }

            //BusyOverlay.Stop();
            return toReturn;
        }

        public List<SapLink> GetLinksInGroup(string inGroupName, IProgress<ProgressData> ReportProgress = null)
        {
            if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage($"Getting links that belong to group {inGroupName} from SAP2000.", true));

            int numberItems = 0;
            int[] objectTypes = null;
            string[] names = null;

            int ret = SapApi.GroupDef.GetAssignments(inGroupName, ref numberItems, ref objectTypes, ref names);

            if (0 == ret)
            {
                ReportProgress.Report(ProgressData.Reset());
                throw new S2KHelperException($"Could not get links of group {inGroupName}. Are you sure the group exists?");
            }

            if (numberItems == 0)
            {
                ReportProgress.Report(ProgressData.Reset());
                return new List<SapLink>();
            }

            List<SapLink> toRet = new List<SapLink>();
            for (int i = 0; i < numberItems; i++)
            {
                if ((SapObjectType)objectTypes[i] == SapObjectType.Cable) toRet.Add(GetByName(names[i]));
            }

            ReportProgress.Report(ProgressData.Reset());
            return toRet;
        }

        public bool SelectElements(List<SapLink> links, IProgress<ProgressData> ReportProgress = null)
        {
            bool allSelected = true;
            for (int i = 0; i < links.Count; i++)
            {
                SapLink link = links[i];

                if (SapApi.FrameObj.SetSelected(link.Name, true, eItemType.Objects) != 0) allSelected = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,links.Count));
            }
            return allSelected;
        }

        public bool DeleteLink(SapLink toDelete)
        {
            int ret = SapApi.LinkObj.Delete(toDelete.Name);
            if (ret != 0) throw new S2KHelperException($"Could not delete Link called {toDelete.Name}.");
            return true;
        }
    }
}
