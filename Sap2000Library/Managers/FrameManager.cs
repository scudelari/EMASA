using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using ExcelDataReader.Exceptions;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class FrameManager : SapManagerBase
    {
        internal FrameManager(S2KModel model) : base(model) { }

        public string AddByPoint(SapPoint iEnd, SapPoint jEnd, string Section = "Default", string UserName = "")
        {
            string sapName = null;

            int ret = SapApi.FrameObj.AddByPoint(iEnd.Name, jEnd.Name, ref sapName, Section, UserName);

            if (ret == 0 && !string.IsNullOrWhiteSpace(sapName)) return sapName;
            return null;
        }
        public string AddByPoint(string iEndName, string jEndName, string Section = "Default", string UserName = "")
        {
            string sapName = null;

            int ret = SapApi.FrameObj.AddByPoint(iEndName, jEndName, ref sapName, Section, UserName);

            if (ret == 0 && !string.IsNullOrWhiteSpace(sapName)) return sapName;
            return null;
        }

        public SapFrame AddByPoint_ReturnSapEntity(SapPoint iEnd, SapPoint jEnd, string Section = "Default", string UserName = "")
        {
            string frameName = AddByPoint(iEnd, jEnd, Section, UserName);

            if (string.IsNullOrWhiteSpace(frameName)) return null;

            return new SapFrame(frameName, iEnd, jEnd, this);
        }
        public SapFrame AddByPoint_ReturnSapEntity(string iEnd, string jEnd, string Section = "Default", string UserName = "")
        {
            string frameName = AddByPoint(iEnd, jEnd, Section, UserName);

            if (string.IsNullOrWhiteSpace(frameName)) return null;

            return GetByName(frameName);
        }

        public SapFrame GetByName(string frameName)
        {
            string iPoint = null;
            string jPoint = null;

            if (SapApi.FrameObj.GetPoints(frameName, ref iPoint, ref jPoint) != 0) return null;

            return new SapFrame(frameName, s2KModel.PointMan.GetByName(iPoint), s2KModel.PointMan.GetByName(jPoint), this);
        }
        public List<SapFrame> GetByNames(List<string> frameNames)
        {
            List<SapFrame> toRet = new List<SapFrame>();

            foreach (var name in frameNames)
            {
                toRet.Add(GetByName(name));
            }

            return toRet;
        }

        [Obsolete]
        public List<SapFrame> GetAll(IProgress<ProgressData> ReportProgress)
        {
            //if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage("Getting all frames from SAP2000.", true));

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.FrameObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,count));
            }

            if (ReportProgress != null) ReportProgress.Report(ProgressData.Reset());

            return toReturn;
        }
        public List<SapFrame> GetAll(BusyOverlay BusyOverlay)
        {
            if (BusyOverlay != null) BusyOverlay.SetDeterminate($"Getting all frames from SAP2000.", "Frame");

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.FrameObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (BusyOverlay != null) BusyOverlay.UpdateProgress(i, count, names[i]);
            }

            BusyOverlay.Stop();
            return toReturn;
        }
        public List<SapFrame> GetAll(bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"SAP2000: Getting All Frames.", "Frame");

            // Gets all names
            int count = 0;
            string[] names = null;

            if (SapApi.FrameObj.GetNameList(ref count, ref names) != 0) return null;

            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                toReturn.Add(GetByName(names[i]));

                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, count, names[i]);
            }

            return toReturn;
        }

        [Obsolete]
        public List<SapFrame> GetSelected(IProgress<ProgressData> ReportProgress = null)
        {
            //if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage(@"Getting List of Selected Frames. [[Frame: ***]]"));

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapFrame>();

            // Gets count of desired element types
            int typeCount = (from a in objectType
                             where a == (int)SelectObjectType.FrameObject
                             select a).Count();
            int currType = 0;

            // Declares the return
            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.FrameObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(++currType,count, selectedNames[i]));
                }
            }

            if (ReportProgress != null) ReportProgress.Report(ProgressData.Reset());

            return toReturn;
        }
        [Obsolete]
        public List<SapFrame> GetSelected(BusyOverlay BusyOverlay)
        {
            if (BusyOverlay != null) BusyOverlay.SetDeterminate($"Getting selected frames from SAP2000.", "Frame");

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapFrame>();

            // Gets count of desired element types
            int currType = 0;
            int typeCount = objectType.Count(a => a == (int)SelectObjectType.FrameObject);
            // Declares the return
            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.FrameObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (BusyOverlay != null) BusyOverlay.UpdateProgress(currType, typeCount, selectedNames[i]);
                    currType++;
                }
            }

            BusyOverlay.Stop();
            return toReturn;
        }
        public List<SapFrame> GetSelected(bool inUpdateInterface)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"SAP2000: Getting selected Frames.", "Frame");

            int count = 0;
            int[] objectType = null;
            string[] selectedNames = null;

            int ret = SapApi.SelectObj.GetSelected(ref count, ref objectType, ref selectedNames);
            if (ret != 0 || count == 0) return new List<SapFrame>();

            // Gets count of desired element types
            int currType = 0;
            int typeCount = objectType.Count(a => a == (int)SelectObjectType.FrameObject);
            // Declares the return
            List<SapFrame> toReturn = new List<SapFrame>();

            for (int i = 0; i < count; i++)
            {
                if (objectType[i] == (int)SelectObjectType.FrameObject)
                {
                    toReturn.Add(GetByName(selectedNames[i]));

                    if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(currType, typeCount, selectedNames[i]);
                    currType++;
                }
            }

            return toReturn;
        }

        public List<SapFrame> GetGroup(string inGroupName, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"Getting frames that belong to group {inGroupName} from SAP2000.", "Frame");

            int numberItems = 0;
            int[] objectTypes = null;
            string[] names = null;

            int ret = SapApi.GroupDef.GetAssignments(inGroupName, ref numberItems, ref objectTypes, ref names);

            if (0 != ret)
            {
                if (inUpdateInterface) BusyOverlayBindings.I.Stop();
                throw new S2KHelperException($"Could not get frames of group {inGroupName}. Are you sure the group exists?");
            }

            if (numberItems == 0)
            {
                if (inUpdateInterface) BusyOverlayBindings.I.Stop();
                return new List<SapFrame>();
            }

            List<SapFrame> toRet = new List<SapFrame>();
            for (int i = 0; i < numberItems; i++)
            {
                string frameName = names[i];

                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, numberItems, frameName);

                if ((SapObjectType)objectTypes[i] == SapObjectType.Frame) toRet.Add(GetByName(frameName));
            }

            if (inUpdateInterface) BusyOverlayBindings.I.Stop();
            return toRet;
        }

        public SapFrame JoinFrames(List<SapFrame> inFrames)
        {
            if (inFrames == null) throw new S2KHelperException("The list of frames to join cannot be null.");
            if (inFrames.Count < 2) throw new S2KHelperException("The list of frames to join must have at least two frames.");
            foreach (var item1 in inFrames)
            {
                foreach (var item2 in inFrames)
                {
                    if (item1 != item2)
                    {
                        if (!item1.IsColinearTo(item2)) throw new S2KHelperException("All frames in the list must be colinear.");
                    }
                }
            }

            // First frame
            string first = inFrames[0].Name;

            for (int i = 1; i < inFrames.Count; i++)
            {
                if (0 != SapApi.EditFrame.Join(first, inFrames[i].Name)) throw new S2KHelperException($"Could not join frame {inFrames[i].Name} to {first}.");
            }

            return GetByName(first);
        }

        public bool SelectElements(string inFrameName)
        {
            return SapApi.FrameObj.SetSelected(inFrameName, true, eItemType.Objects) == 0;
        }
        public bool SelectElements(SapFrame inFrame)
        {
            return SelectElements(inFrame.Name);
        }
        public bool SelectElements(List<SapFrame> frames, IProgress<ProgressData> ReportProgress = null)
        {
            bool allSelected = true;
            for (int i = 0; i < frames.Count; i++)
            {
                SapFrame frame = frames[i];
                if (!SelectElements(frame.Name)) allSelected=false;

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,frames.Count));
            }
            return allSelected;
        }

        public bool SetModifiers_Group(string GroupName, double CrossArea, double Shear2, double Shear3, double Torsional, double Moment2, double Moment3, double Mass, double Weight)
        {
            double[] values = new double[] { CrossArea, Shear2, Shear3, Torsional, Moment2, Moment3, Mass, Weight };

            return 0 == SapApi.FrameObj.SetModifiers(GroupName, ref values, (eItemType)(int)ItemTypeEnum.Group);
        }

        public SapFrameSection GetFrameSectionOfFrame(string inFrameName)
        {
            string sectName = null;
            string sAutoListName = null;
            if (0 != SapApi.FrameObj.GetSection(inFrameName, ref sectName, ref sAutoListName)) throw new S2KHelperException($"Could not get the assigned section of frame {inFrameName}. Maybe it is prismatic (not yet supported by this coe - WRITE IT)!");

            return s2KModel.FrameSecMan.GetFrameSectionByName(sectName);
        }

        /// <summary>
        /// Sets the section of the given frame.
        /// </summary>
        /// <param name="inFrameName">The name of the frame to change the section.</param>
        /// <param name="inSectionName">The name of the desired section.</param>
        /// <exception cref="S2KHelperException">Thrown when the section could not be assigned to the frame.</exception>
        public void SetFrameSection(string inFrameName, string inSectionName)
        {
            if (0 != SapApi.FrameObj.SetSection(inFrameName, inSectionName))
                throw new S2KHelperException($"Could not set the section of frame named {inFrameName} to section {inSectionName}.");
        }

        public void Selected_SetTemperatureLoad(string inLoadPatternName, double inTemperature, bool inReplace = true)
        {
            if (0 != SapApi.FrameObj.SetLoadTemperature("", inLoadPatternName, 1, inTemperature, "", inReplace, eItemType.SelectedObjects))
            {
                throw new S2KHelperException($"Could not set {inTemperature:+###.###F;-###.###F;0FRef} as the temperature loading for pattern {inLoadPatternName} for the selected frames.");
            }
        }
    }
}
