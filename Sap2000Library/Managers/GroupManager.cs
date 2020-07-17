using System;
using System.Collections.Generic;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using Sap2000Library.Other;

namespace Sap2000Library.Managers
{
    public class GroupManager : SapManagerBase
    {
        internal GroupManager(S2KModel model) : base(model) { }

        public List<string> GetGroupList(bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Getting Group Name List.");

            int number = 0;
            string[] names = null;

            int ret = SapApi.GroupDef.GetNameList(ref number, ref names);
            if (ret != 0)
            {
                return null;
            }

            if (names == null || names.Length == 0) return new List<string>();

            return new List<string>(names);
        }

        public bool AddGroup(string group)
        {
            return 0 == SapApi.GroupDef.SetGroup(group);
        }

        public bool SelectGroup(string group)
        {
            return 0 == SapApi.SelectObj.Group(group, false);
        }

        public bool CreateGroup(string group)
        {
            return 0 == SapApi.GroupDef.SetGroup(group);
        }

        public bool ClearGroup(string group)
        {
            return 0 == SapApi.GroupDef.Clear(group);
        }

        public bool DeleteGroup(string group)
        {
            return 0 == SapApi.GroupDef.Delete(group);
        }

        public bool DeleteAllElementsInGroup(string group)
        {
            SapApi.SelectObj.ClearSelection();

            SelectGroup(group);

            s2KModel.InterAuto.FlaUI_Action_SendDeleteKey();

            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "FocusWindow"));

            return true;
        }

        public bool RenameGroup(string from, string to)
        {
            return 0 == SapApi.GroupDef.ChangeName(from, to);
        }

        public bool AddPointToGroup(string inGroup, string inName)
        {
            return 0 == s2KModel.SapApi.PointObj.SetGroupAssign(inName, inGroup);
        }
        public bool RemovePointFromGroup(string inGroup, string inName)
        {
            return 0 == s2KModel.SapApi.PointObj.SetGroupAssign(inName, inGroup, true);
        }

        public bool AddFrameToGroup(string inGroup, string inName)
        {
            return 0 == s2KModel.SapApi.FrameObj.SetGroupAssign(inName, inGroup);
        }
        public bool RemoveFrameFromGroup(string inGroup, string inName)
        {
            return 0 == s2KModel.SapApi.FrameObj.SetGroupAssign(inName, inGroup, true);
        }
    }
}
