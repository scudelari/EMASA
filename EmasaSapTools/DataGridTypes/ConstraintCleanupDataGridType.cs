using Sap2000Library;

namespace EmasaSapTools.DataGridTypes
{
    public class ConstraintCleanupDataGridType
    {
        private string _constraintName;

        public string ConstraintName
        {
            get => _constraintName;
            set => _constraintName = value;
        }

        public int NumberPoints { get; set; }
        public double? PointMaxDistance { get; set; }
        public string[] PointNames { get; set; }
        public ConstraintTypeEnum ConstraintType { get; set; }
    }
}