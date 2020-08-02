using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Loads
{
    public abstract class FeLoadBase
    {
        protected FeLoadBase()
        {
        }

        public abstract void LoadModel(FeModelBase inModel, double inFactor = 1d);
    }
}
