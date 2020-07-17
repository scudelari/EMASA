namespace Sap2000Library.DataClasses
{
    public class PointPanelZoneDef
    {
        public PointPanelZone_PropType PropType;
        public double Thickness;
        public double K1;
        public double K2;
        public string LinkProp;
        public PointPanelZone_Connectivity Connectivity;
        public PointPanelZone_LocalAxisFrom LocalAxisFrom;
        public double LocalAxisAngle;
    }
}
