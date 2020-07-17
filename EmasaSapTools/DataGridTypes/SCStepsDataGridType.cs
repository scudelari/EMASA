using Sap2000Library;

namespace EmasaSapTools.DataGridTypes
{
    public class SCStepsDataGridType
    {
        public string GroupName { get; set; }
        public string NamedProp { get; set; }
        public StagedConstructionOperation Operation { get; set; }
        public int Order { get; set; }
    }
}