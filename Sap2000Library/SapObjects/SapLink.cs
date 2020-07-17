using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapLink : SapLine
    {
        private LinkManager owner = null;
        internal SapLink(string name, SapPoint iEndPoint, SapPoint jEndPoint, LinkManager linkManager) : base(name, SapObjectType.Frame, iEndPoint, jEndPoint, linkManager)
        {
            owner = linkManager;
        }

        public override string ToString()
        {
            return string.Format("Lname: {0}, Iname: {1}, Jname: {2}", Name, iEndPoint.ToString(), jEndPoint.ToString());
        }
    }
}
