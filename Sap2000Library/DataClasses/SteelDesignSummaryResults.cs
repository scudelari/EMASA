namespace Sap2000Library.DataClasses
{
    public class SteelDesignSummaryResults
    {
        public string FrameName { get; set; }
        public double Ratio { get; set; }
        public SteelDesignRatioType RatioType { get; set; }
        public double Location { get; set; }
        public string CombName { get; set; }
        public string ErrorSummary { get; set; }
        public string WarningSummary { get; set; }
    }
}
