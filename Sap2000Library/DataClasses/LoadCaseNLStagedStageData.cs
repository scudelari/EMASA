using System;
using System.Linq;

namespace Sap2000Library.DataClasses
{
    public class LoadCaseNLStagedStageData
    {
        public LoadCaseNLStagedStageData(StagedConstructionOperation inOperation)
        {
            Operation = inOperation;
        }

        public StagedConstructionOperation Operation { get; set; }

        private string _objectType;
        public string ObjectType
        {
            get { return _objectType; }
            set
            {
                string[] possibleValues = default;

                switch (Operation)
                {
                    case StagedConstructionOperation.AddStructure: // 1
                    case StagedConstructionOperation.RemoveStructure: // 2
                    case StagedConstructionOperation.LoadObjectsIfNew: // 3
                    case StagedConstructionOperation.LoadObjects: // 4
                        possibleValues = new string[] { "Group", "Frame", "Cable", "Tendon", "Area", "Solid", "Link", "Point" };
                        break;

                    case StagedConstructionOperation.ChangeSectionProperties_Area:
                        possibleValues = new string[] { "Group", "Area" };
                        break;
                    case StagedConstructionOperation.ChangeSectionProperties_Cable:
                        possibleValues = new string[] { "Group", "Cable" };
                        break;
                    case StagedConstructionOperation.ChangeSectionProperties_Link:
                        possibleValues = new string[] { "Group", "Link" };
                        break;
                    case StagedConstructionOperation.ChangeSectionProperties_Frame:
                        possibleValues = new string[] { "Group", "Frame" };
                        break;

                    case StagedConstructionOperation.ChangeSectionPropertyModifiers_Area:
                        possibleValues = new string[] { "Group", "Area" };
                        break;
                    case StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame:
                        possibleValues = new string[] { "Group", "Frame" };
                        break;

                    case StagedConstructionOperation.ChangeReleases:
                        possibleValues = new string[] { "Group", "Frame" };
                        break;
                    case StagedConstructionOperation.ChangeSectionPropertiesAndAge:
                        possibleValues = new string[] { "Group", "Frame", "Cable", "Tendon", "Area", "Solid", "Link" };
                        break;
                    case StagedConstructionOperation.AddGuideStructure:
                        possibleValues = new string[] { "Group" };
                        break;
                    default:
                        break;
                }

                if (value != "Group") throw new NotImplementedException("Currently only group is supported for ObjectType of the LoadCaseNLStagedStageData Object.");

                if (possibleValues.Contains(value)) _objectType = value;
                else throw new S2KHelperException($"The ObjectType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option!");
            }
        }
        public string ObjectName = null;

        private double? _ageWhenAdded;
        public double? AgeWhenAdded
        {
            get { return _ageWhenAdded; }
            set
            {
                if (Operation == StagedConstructionOperation.AddStructure) _ageWhenAdded = value;
                else throw new S2KHelperException($"The AgeWhenAdded of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. This is Invalid.");
            }
        }

        private string _myType;
        public string MyType
        {
            get { return _myType; }
            set
            {
                // 1, 2, 14
                if (Operation == StagedConstructionOperation.AddStructure 
                    || Operation == StagedConstructionOperation.RemoveStructure
                    || Operation == StagedConstructionOperation.AddGuideStructure)
                    throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. Operations {Operation.ToString()} cannot have MyType set.");

                // 3, 4
                if (Operation == StagedConstructionOperation.LoadObjects
                    || Operation == StagedConstructionOperation.LoadObjectsIfNew)
                {
                    if (value == "Load" || value == "Accel")
                    {
                        _myType = value;
                        return;
                    }
                    throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. Operations {Operation.ToString()} must have MyType set to either <Load> or <Accel>.");
                }

                // 5, 6, 7, 11
                if (Operation == StagedConstructionOperation.ChangeSectionProperties_Area 
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Frame 
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Cable 
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Link

                    || Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Area
                    || Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame

                    || Operation == StagedConstructionOperation.ChangeReleases
                    || Operation == StagedConstructionOperation.ChangeSectionPropertiesAndAge)
                {
                    if (string.IsNullOrWhiteSpace(ObjectType)) throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. In this case, you must before set something to the ObjectType property.");

                    if (ObjectType == "Group")
                    {
                        if (Operation == StagedConstructionOperation.ChangeSectionProperties_Area)
                        {
                            string[] possibleValues = { "Area" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionProperties_Link)
                        {
                            string[] possibleValues = { "Link" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionProperties_Frame)
                        {
                            string[] possibleValues = { "Frame" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionProperties_Cable)
                        {
                            string[] possibleValues = { "Cable" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionPropertiesAndAge)
                        {
                            string[] possibleValues = { "Frame", "Cable", "Tendon", "Area", "Solid", "Link" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionPropertiesAndAge)
                        {
                            string[] possibleValues = { "Frame", "Cable", "Tendon", "Area", "Solid", "Link" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Area)
                        {
                            string[] possibleValues = { "Area" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame)
                        {
                            string[] possibleValues = { "Frame" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                        else if (Operation == StagedConstructionOperation.ChangeReleases)
                        {
                            string[] possibleValues = { "Frame" };

                            if (possibleValues.Contains(value)) _myType = value;
                            else throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation}!");

                            return;
                        }
                    }
                    else
                    {
                        throw new S2KHelperException($"The MyType of the LoadCaseNLStagedStageData Object was set to {value}, which is unecessary when Operation is {Operation} and the ObjectType is not <Group>!");
                    }
                }

            }
        }

        private string _myName;
        public string MyName
        {
            get { return _myName; }
            set
            {
                // 1, 2, 14
                if (Operation == StagedConstructionOperation.AddStructure
                    || Operation == StagedConstructionOperation.RemoveStructure
                    || Operation == StagedConstructionOperation.AddGuideStructure)
                    throw new S2KHelperException($"The MyName of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. Operations {Operation.ToString()} cannot have MyName set.");

                if (string.IsNullOrWhiteSpace(MyType))
                    throw new S2KHelperException($"The MyName of the LoadCaseNLStagedStageData Object was set to {value} but MyType is not set. . In this case, you must before set something to the MyType property.");

                // 3, 4
                if (Operation == StagedConstructionOperation.LoadObjects
                    || Operation == StagedConstructionOperation.LoadObjectsIfNew)
                {
                    if (MyType == "Load")
                    {
                        _myName = value;
                        return;
                    }
                    else if (MyType == "Accel")
                    {
                        string[] possibleValues = { "UX", "UY", "UZ", "RX", "RY", "RZ" };

                        if (possibleValues.Contains(value)) _myName = value;
                        else throw new S2KHelperException($"The MyName of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation} and when MyType is {MyType}!");

                        return;
                    }
                    else
                    {
                        throw new S2KHelperException($"The MyName of the LoadCaseNLStagedStageData Object was set to {value}, which is an Invalid Option when Operation is {Operation} and when MyType is {MyType}!");
                    }
                }

                if (Operation == StagedConstructionOperation.ChangeSectionProperties_Area
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Cable
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Frame
                    || Operation == StagedConstructionOperation.ChangeSectionProperties_Link
                    || Operation == StagedConstructionOperation.ChangeSectionPropertiesAndAge)
                {
                    // Name of the element Frame, Cable, Tendon, Area, Solid or Link object
                    _myName = value;
                    return;
                }

                if (Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Area
                    || Operation == StagedConstructionOperation.ChangeSectionPropertyModifiers_Frame)
                {
                    // Name of the element Frame, Cable, Area
                    _myName = value;
                    return;
                }

                if (Operation == StagedConstructionOperation.ChangeReleases)
                {
                    // Name of the element Frame
                    _myName = value;
                    return;
                }
            }
        }

        private double? _scaleFactor;
        public double? ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (Operation == StagedConstructionOperation.LoadObjectsIfNew
                    || Operation == StagedConstructionOperation.LoadObjects) _scaleFactor = value;
                else throw new S2KHelperException($"The ScaleFactor of the LoadCaseNLStagedStageData Object was set to {value} but the Operation is {Operation.ToString()}. This is Invalid.");
            }
        }
    }
}
