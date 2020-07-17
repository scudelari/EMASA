using System;
using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapCable : SapLine
    {
        private CableManager owner = null;
        internal SapCable(string name, SapPoint iEndPoint, SapPoint jEndPoint, CableManager cableManager) : base(name, SapObjectType.Cable, iEndPoint, jEndPoint, cableManager)
        {
            owner = cableManager;
        }

        private SapCableSection _section = null;
        public SapCableSection Section
        {
            get
            {
                if (_section != null) return _section;

                string propName = null;
                int ret = owner.SapApi.CableObj.GetProperty(Name, ref propName);

                if (ret != 0) throw new S2KHelperException($"Could not get cable section for cable named {Name}. Are you sure it exists?");

                _section = owner.s2KModel.CableSecMan.GetCableSectionByName(propName);
                return _section;
            }
        }
        public bool SetSection()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("Cname: {0}, Iname: {1}, Jname: {2}", Name, iEndPoint.ToString(), jEndPoint.ToString());
        }
    }
}
