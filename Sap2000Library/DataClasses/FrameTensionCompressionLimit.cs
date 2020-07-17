namespace Sap2000Library.DataClasses
{
    public class FrameTensionCompressionLimit
    {
        public FrameTensionCompressionLimit(double? inTensionLimit, double? inCompressionLimit)
        {
            TensionLimit = inTensionLimit;
            CompressionLimit = inCompressionLimit;
        }

        public double? TensionLimit { get; set; }
        public double? CompressionLimit { get; set; }
    }
}
