namespace Sap2000Library.DataClasses
{
    public class LoadPatternData
    {
		 
		private LoadPatternType loadPatternType;
		public LoadPatternType PatternType
		{
			get { return loadPatternType; }
			set { loadPatternType = value; }
		}

		private string name;
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		private double selfWeightMult = 0;
		public double SelfWeightMultiplier
		{
			get { return selfWeightMult; }
			set { selfWeightMult = value; }
		}

	}
}
