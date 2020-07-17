using System.Linq;

namespace Sap2000Library.DataClasses
{
    public class LoadCaseNLLoadData
    {
        private string _loadType;
        public string LoadType
        {
            get { return _loadType; }
            set
            {
                if (value != "Load" && value != "Accel")
                    throw new S2KHelperException($"The LoadType of the LoadCaseLoadData Object was set to {value}, which is an Invalid Option!");
                _loadType = value;
            }
        }

        private string _loadName;
        public string LoadName
        {
            get => _loadName;
            set
            {
                if (string.IsNullOrWhiteSpace(LoadType))
                    throw new S2KHelperException($"The LoadType of the LoadCaseLoadData Object must be set before setting the LoadName!");

                if (LoadType == "Accel")
                {
                    string[] possibleValues = { "UX", "UY", "UZ", "RX", "RY", "RZ" };

                    if (possibleValues.Contains(value)) _loadName = value;
                    else throw new S2KHelperException($"The LoadName of the LoadCaseLoadData Object was set to {value}, which is an Invalid Option when LoadType is {LoadType}!");

                    return;
                }
                else _loadName = value;
            }
        }

        private double _scaleFactor;
        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (value == 0d)
                {
                    throw new S2KHelperException($"The ScaleFactor of the LoadCaseLoadData Object was set to {value}, which is an Invalid Option. Don't Add the Load!");
                }
                _scaleFactor = value;
            }
        }
    }
}
