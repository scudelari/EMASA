using System;
using System.Collections.Generic;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;

namespace Sap2000Library.Managers
{
    public class CableSectionManager : SapManagerBase
    {
        internal CableSectionManager(S2KModel model) : base(model) { }

        public List<string> GetSectionNameList(IProgress<ProgressData> progReporter = null)
        {
            progReporter?.Report(ProgressData.SetMessage("Getting cable section list from model", true));

            int ret = 0;

            int numbernames = 0;
            string[] sections = null;

            ret = SapApi.PropCable.GetNameList(ref numbernames, ref sections);

            progReporter?.Report(ProgressData.Reset());

            return new List<string>(sections);
        }

        public SapCableSection GetCableSectionByName(string inName)
        {
            string material = null;
            double area = 0;
            int color = 0;
            string notes = null;
            string guid = null;

            int ret = SapApi.PropCable.GetProp(inName, ref material, ref area, ref color, ref notes, ref guid);

            if (ret != 0) throw new S2KHelperException($"Could not get the cable section named {inName}. Are you sure it exists?");

            return new SapCableSection(inName, this) { Area = area, Material = material};
        }
    }
}
