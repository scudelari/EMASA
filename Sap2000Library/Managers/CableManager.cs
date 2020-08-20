using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class CableManager : SapManagerBase
    {
        internal CableManager(S2KModel model) : base(model) { }

        public SapCable GetByName(string cableName)
        {
            string iPoint = null;
            string jPoint = null;

            if (SapApi.CableObj.GetPoints(cableName, ref iPoint, ref jPoint) != 0) return null;

            return new SapCable(cableName, s2KModel.PointMan.GetByName(iPoint), s2KModel.PointMan.GetByName(jPoint), this);
        }

        [Obsolete]
        public List<SapCable> GetAll(IProgress<ProgressData> ReportProgress)
        {
            if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage("Getting all cables"));

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.CableObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapCable> toReturn = new List<SapCable>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,count));
            }

            ReportProgress.Report(ProgressData.Reset());

            return toReturn;
        }
        public List<SapCable> GetAll(BusyOverlay BusyOverlay)
        {
            if (BusyOverlay != null) BusyOverlay.SetDeterminate($"Getting all cables from SAP2000.", "Cable");

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.CableObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapCable> toReturn = new List<SapCable>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (BusyOverlay != null) BusyOverlay.UpdateProgress(i, count, names[i]);
            }

            BusyOverlay.Stop();
            return toReturn;
        }
        public List<SapCable> GetAll(bool inUpdateInteraface = false)
        {
            if (inUpdateInteraface) BusyOverlayBindings.I.SetDeterminate($"SAP2000: Getting All Cables.", "Cable");

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.CableObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapCable> toReturn = new List<SapCable>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (inUpdateInteraface) BusyOverlayBindings.I.UpdateProgress(i, count, names[i]);
            }

            return toReturn;
        }

        public List<SapCable> GetSelected(IProgress<ProgressData> ReportProgress = null)
        {
            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapCable>();

            // Gets count of desired element types
            int typeCount = (from a in objectType
                             where a == (int)SelectObjectType.CableObject
                             select a).Count();
            int currType = 0;

            // Declares the return
            List<SapCable> toReturn = new List<SapCable>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.CableObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(++currType,count));
                }
            }

            return toReturn;
        }
        public List<SapCable> GetGroup(string inGroupName, IProgress<ProgressData> ReportProgress = null)
        {
            if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage($"Getting cables that belong to group {inGroupName} from SAP2000.", true));

            int numberItems = 0;
            int[] objectTypes = null;
            string[] names = null;

            int ret = SapApi.GroupDef.GetAssignments(inGroupName, ref numberItems, ref objectTypes, ref names);

            if (0 == ret)
            {
                ReportProgress.Report(ProgressData.Reset());
                throw new S2KHelperException($"Could not get cables of group {inGroupName}. Are you sure the group exists?");
            }

            if (numberItems == 0)
            {
                ReportProgress.Report(ProgressData.Reset());
                return new List<SapCable>();
            }

            List<SapCable> toRet = new List<SapCable>();
            for (int i = 0; i < numberItems; i++)
            {
                if ((SapObjectType)objectTypes[i] == SapObjectType.Cable) toRet.Add(GetByName(names[i]));
            }

            ReportProgress.Report(ProgressData.Reset());
            return toRet;
        }

        public bool SelectElements(List<SapCable> cables, IProgress<ProgressData> ReportProgress = null)
        {
            bool allSelected = true;
            for (int i = 0; i < cables.Count; i++)
            {
                SapCable cable = cables[i];
                if (SapApi.CableObj.SetSelected(cable.Name, true, eItemType.Objects) != 0) allSelected = false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,cables.Count));
            }
            return allSelected;
        }

        public void Selected_SetTemperatureLoad(string inLoadPatternName, double inTemperature, bool inReplace = true)
        {
            if (0 != SapApi.CableObj.SetLoadTemperature("", inLoadPatternName, inTemperature, "", inReplace, eItemType.SelectedObjects))
            {
                throw new S2KHelperException($"Could not set {inTemperature:+###.###F;-###.###F;0FRef} as the temperature loading for pattern {inLoadPatternName} for the selected cables.");
            }
        }
    }
}
