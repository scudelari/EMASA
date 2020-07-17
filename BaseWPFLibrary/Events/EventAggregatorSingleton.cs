using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace BaseWPFLibrary.Events
{
    public class EventAggregatorSingleton : EventAggregator
    {
        private static IEventAggregator _eventAggregator;
        /// <summary>
        /// This is a lock that can be used by child classes to ensure thread safety.
        /// </summary>
        private static readonly object _padLock = new object();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static IEventAggregator I
        {
            get
            {
                lock (_padLock)
                {
                    if (_eventAggregator == null) _eventAggregator = new EventAggregator();
                    return _eventAggregator;
                }
            }
        }
    }
}
