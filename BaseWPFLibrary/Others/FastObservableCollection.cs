using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Threading;

namespace BaseWPFLibrary.Others
{
    public class FastObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object locker = new object();

        /// <summary>
        /// This private variable holds the flag to
        /// turn on and off the collection changed notification.
        /// </summary>
        private bool suspendCollectionChangeNotification;

        /// <summary>
        /// Initializes a new instance of the FastObservableCollection class.
        /// </summary>
        public FastObservableCollection()
            : base()
        {
            suspendCollectionChangeNotification = false;
        }

        /// <summary>
        /// This event is overriden CollectionChanged event of the observable collection.
        /// </summary>
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// This method will replace the items of the collection if, and only if, they are different than the current list
        /// </summary>
        /// <param name="items">The new list.</param>
        /// <returns>True if the lists are the same (not replaced).</returns>
        public bool ReplaceItemsIfNew(IList<T> items)
        {
            // Lists are the same (they have the same elements)
            if (this.All(items.Contains) && Count == items.Count) return true;

            AddItems(items, true);
            return false;
        }

        /// <summary>
        /// This method will add the items in the input that don't currently exist.
        /// </summary>
        /// <param name="items">The list of items to add.</param>
        /// <returns>The number of newly added items.</returns>
        public int AddItemsIfNew(IList<T> items)
        {
            List<T> toAdd = (from a in items where !Contains(a) select a).ToList();

            if (toAdd.Count > 0) AddItems(toAdd);

            return toAdd.Count;
        }

        /// <summary>
        /// This method adds the given generic list of items
        /// as a range into current collection by casting them as type T.
        /// It then notifies once after all items are added.
        /// </summary>
        /// <param name="items">The source collection.</param>
        /// <param name="replaceList">If true, will clear the list before adding items.</param>
        public void AddItems(IList<T> items, bool replaceList = false)
        {
            lock (locker)
            {
                SuspendCollectionChangeNotification();

                if (replaceList) ClearItems();

                foreach (var i in items)
                {
                    InsertItem(Count, i);
                }
                NotifyChanges();
            }
        }

        /// <summary>
        /// Raises collection change event.
        /// </summary>
        public void NotifyChanges()
        {
            ResumeCollectionChangeNotification();
            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(arg);
        }

        /// <summary>
        /// This method removes the given generic list of items as a range
        /// into current collection by casting them as type T.
        /// It then notifies once after all items are removed.
        /// </summary>
        /// <param name="items">The source collection.</param>
        public void RemoveItems(IList<T> items)
        {
            lock (locker)
            {
                SuspendCollectionChangeNotification();
                foreach (var i in items)
                {
                    Remove(i);
                }
                NotifyChanges();
            }
        }

        /// <summary>
        /// Resumes collection changed notification.
        /// </summary>
        public void ResumeCollectionChangeNotification()
        {
            suspendCollectionChangeNotification = false;
        }

        /// <summary>
        /// Suspends collection changed notification.
        /// </summary>
        public void SuspendCollectionChangeNotification()
        {
            suspendCollectionChangeNotification = true;
        }

        /// <summary>
        /// This collection changed event performs thread safe event raising.
        /// </summary>
        /// <param name="e">The event argument.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // Recommended is to avoid reentry 
            // in collection changed event while collection
            // is getting changed on other thread.
            using (BlockReentrancy())
            {
                if (!suspendCollectionChangeNotification)
                {
                    NotifyCollectionChangedEventHandler eventHandler = CollectionChanged;
                    if (eventHandler == null)
                    {
                        return;
                    }

                    // Walk thru invocation list.
                    Delegate[] delegates = eventHandler.GetInvocationList();

                    foreach
                    (NotifyCollectionChangedEventHandler handler in delegates)
                    {
                        // If the subscriber is a DispatcherObject and different thread.
                        DispatcherObject dispatcherObject
                             = handler.Target as DispatcherObject;

                        if (dispatcherObject != null)
                        {
                            if (!dispatcherObject.CheckAccess())
                            {
                                // Invoke handler in the target dispatcher's thread... 
                                // asynchronously for better responsiveness.
                                dispatcherObject.Dispatcher.BeginInvoke
                                    (DispatcherPriority.DataBind, handler, this, e);
                            }
                            else
                            {
                                // Execute handler as is.
                                handler(this, e);
                            }
                        }
                        else
                        {
                            // Execute handler as is.
                            handler(this, e);
                        }
                    }
                }
            }
        }
    }
}

