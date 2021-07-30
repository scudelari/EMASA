using System.Collections.Generic;
using BaseWPFLibrary.Bindings;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class FrameSectionManager : SapManagerBase
    {
        internal FrameSectionManager(S2KModel model) : base(model) { }

        public bool ImportFrameSection(string Name, string MatProp, string FileName, string PropName, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.ImportProp(Name, MatProp, FileName, PropName, Color, Notes, GUID);
            return ret == 0;
        }
        public bool SetOrAddISection(string Name, string MatProp, double Depth, double TopFlangeWidth, double TopFlangeThickness, double WebThickness, double BottomFlangeWidth, double BottomFlangeThickness, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetISection(Name, MatProp, 
                Depth, TopFlangeWidth, TopFlangeThickness, WebThickness, BottomFlangeWidth, BottomFlangeThickness,
                Color, Notes, GUID);

            return ret == 0;
        }
        public bool SetOrAddAngle(string Name, string MatProp, double VerticalLegDepth, double HorizontalLegDepth, double HorizontalLegThickness, double VerticalLegThickness, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetAngle(Name, MatProp, 
                VerticalLegDepth, HorizontalLegDepth, HorizontalLegThickness, VerticalLegThickness,
                Color, Notes, GUID);

            return ret == 0;
        }
        public bool SetOrAddPipe(string Name, string MatProp, double OutsideDiameter, double WallThickness, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetPipe(Name, MatProp,
                OutsideDiameter, WallThickness,
                Color, Notes, GUID);

            return ret == 0;
        }
        public bool SetOrAddTube(string Name, string MatProp, double Depth, double Width, double FlangeThickness, double WebThickness, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetTube(Name, MatProp,
                Depth, Width, FlangeThickness, WebThickness,
                Color, Notes, GUID);

            return ret == 0;
        }
        public bool SetOrAddCircle(string Name, string MatProp, double Diameter, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetCircle(Name, MatProp, Diameter,
                Color, Notes, GUID);

            return ret == 0;
        }
        public bool SetOrAddRectangle(string Name, string MatProp, double Thickness, double Width, int Color = -1, string Notes = "", string GUID = "")
        {
            int ret = SapApi.PropFrame.SetRectangle(Name, MatProp, Thickness, Width,
                Color, Notes, GUID);

            return ret == 0;
        }

        public List<string> GetSectionNameList(FramePropType? inFilterType = null, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Getting Frame Section Name List.");

            int countNames = 0;
            string[] sections = null;

            if (inFilterType.HasValue)
            {
                SapApi.PropFrame.GetNameList(ref countNames, ref sections, (eFramePropType)(int)inFilterType);
            }
            else
            {
                SapApi.PropFrame.GetNameList(ref countNames, ref sections);
            }

            return new List<string>(sections);
        }

        public SapFrameSection GetFrameSectionByName(string inName)
        {
            eFramePropType typeRet = default;

            // First acquires the section type
            int ret = SapApi.PropFrame.GetTypeOAPI(inName, ref typeRet);
            if (ret != 0) throw new S2KHelperException($"Could not get the type of the frame section section named {inName}. Are you sure it exists?");

            return new SapFrameSection(inName, (FramePropType)(int)typeRet, this);
        }
    }
}
