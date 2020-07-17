using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Sap2000Library
{
    public class S2KHelperException : Exception
    {
        public S2KHelperException(object inAttachedData = null)
        {
            AttachedData = inAttachedData;
        }

        public S2KHelperException(string message, object inAttachedData = null) : base(message)
        {
            AttachedData = inAttachedData;
        }

        public S2KHelperException(string message, Exception innerException, object inAttachedData = null) : base(message, innerException)
        {
            AttachedData = inAttachedData;
        }

        protected S2KHelperException(SerializationInfo info, StreamingContext context, object inAttachedData = null) : base(info, context)
        {
            AttachedData = inAttachedData;
        }

        public S2KHelperException(string message) : base(message)
        {
        }

        public object AttachedData;


    }
}
