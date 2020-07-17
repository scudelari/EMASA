using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class SapManagerBase
    {
        private S2KModel s2kmodel;
        internal cSapModel SapApi => s2kmodel.SapApi;
        internal S2KModel s2KModel => s2kmodel;
        internal SapManagerBase(S2KModel model)
        {
            s2kmodel = model;
        }
    }
}
