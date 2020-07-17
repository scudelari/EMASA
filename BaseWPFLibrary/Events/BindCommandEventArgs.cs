using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWPFLibrary.Events
{
    public class BindCommandEventArgs
    {
        public BindCommandEventArgs(object inSender, object inEventData)
        {
            Sender = inSender;
            EventData = inEventData;
        }

        public object Sender { get; }
        public object EventData { get; }
    }
}
