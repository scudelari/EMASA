using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Others;
using BaseWPFLibrary.Annotations;
using Prism.Mvvm;
using Prism.Events;

namespace BaseWPFLibrary.Bindings
{
    public abstract class BindableSingleton<K> : BindableBase where K : BindableSingleton<K>
    {
        /// <summary>
        /// Locks public construction. In a derived class, you must declare a private constructor.
        /// </summary>
        protected BindableSingleton() { }

        /// <summary>
        /// A Static Constructor is called BEFORE any other call is made to this instance.
        /// </summary>
        static BindableSingleton()
        {
            // Instantiates the singleton and calls the SetOrReset method
            lock (_padLock)
            {
                // Must be called only once - should be ensured by the static constructor.
                if (_instance != null) throw new Exception("Somehow the instance is not null, thus the static constructor failed.");

                Type t = typeof(K);

                // Ensure there are no public constructors, making this a Singleton class
                if (t.GetConstructors().Length > 0) throw new InvalidOperationException($"{t.Name} has at least one accessible ctor making it impossible to enforce singleton behaviour");

                // Create an _instance via the ***private*** constructor
                _instance = (K)Activator.CreateInstance(t, true);

                // Saves a reference to ALL FrameworkElements in the main window that have a NAME
                if (!string.IsNullOrWhiteSpace(Application.Current.MainWindow.Name)) SaveReferenceToElement(Application.Current.MainWindow);
                else SaveReferenceToElement(Application.Current.MainWindow, "MainWindow");
                
                // Adds a function that will be called when the main window is loaded
                Application.Current.MainWindow.Loaded += (inSender, inArgs) => StaticOnMainWindowLoaded();

                // Calls the SetOrReset method that should have been overwritten
                _instance.SetOrReset();
            }
        }

        private static K _instance;
        /// <summary>
        /// This is a lock that can be used by child classes to ensure thread safety.
        /// </summary>
        protected static readonly object _padLock = new object();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static K I
        {
            get
            {
                if (_instance == null) throw new InvalidOperationException($"{typeof(K).Name} must be initialized using the method Start before usage.");

                return _instance;
            }
        }
        
        private static Dictionary<string, FrameworkElement> _referencedElements = new Dictionary<string, FrameworkElement>();
        public static FrameworkElement GetReferencedFrameworkElement(string inName)
        {
            return _referencedElements[inName];
        }
        public static FrameworkElement FirstReferencedFrameworkElement
        {
            get
            {
                FrameworkElement element = _referencedElements.Values.FirstOrDefault();
                if (element == null) throw new Exception($"There is no element in the reference list.");
                return element;
            }
        }
        public static Window FirstReferencedWindow
        {
            get
            {
                Window wnd = _referencedElements.Values.OfType<Window>().FirstOrDefault();
                if (wnd == null)
                {
                    // Falls back to the main application window
                    wnd = Application.Current.MainWindow;

                    if (wnd == null) throw new Exception($"There is no window in the reference list AND the Application.Current.MainWindow returned null.");
                }
                return wnd;
            }
        }
        
        /// <summary>
        /// Use this function to save a reference in the singleton of the FrameworkElement that is using this singleton as DataContext.
        /// You can add several different FrameworkElement with different names.
        /// A smart place to call this is in the Element's event DataContextChanged handler.
        /// </summary>
        /// <param name="inFe">A reference to the FrameworkElement.</param>
        /// <param name="inName">A custom name. If not given, it will first try to use the FrameworkElement's x:name. Then, it will try to get the FrameworkElement's tag as string. Finally, it will use the FrameworkElement's FullName from Reflection (namespace+classname).</param>
        public static void SaveReferenceToElement([NotNull] FrameworkElement inFe, string inName = null)
        {
            try
            {
                if (inFe == null) throw new ArgumentNullException(nameof(inFe));

                string name;
                if (inName != null) name = inName;
                else if (!string.IsNullOrWhiteSpace(inFe.Name)) name = inFe.Name;
                else name = inFe.GetType().FullName;

                if (!_referencedElements.ContainsKey(name)) _referencedElements.Add(name, inFe); 
                
            }
            catch (Exception e)
            {

                throw;
            }
        }

        /// <summary>
        /// This function is called by the constructor. It must be overwritten by the class that inherits for initialization.
        /// </summary>
        public abstract void SetOrReset();
        private static void StaticOnMainWindowLoaded()
        {
            I.OnMainWindowLoaded();
        }
        /// <summary>
        /// Override this function to add functionality that is called AFTER the main window is loaded.
        /// Note: Add a call to base BEFORE your logic to have all items available through the GetReferencedFrameworkElement.
        /// </summary>
        public virtual void OnMainWindowLoaded()
        {
            foreach (FrameworkElement fe in Application.Current.MainWindow.FindDescendants<FrameworkElement>().Where(a => !string.IsNullOrWhiteSpace(a.Name)))
            {
                SaveReferenceToElement(fe);
            }
        }

        /// <summary>
        /// Brings to view (in scrollable regions) the given object, that was used to create an item in a list.
        /// </summary>
        /// <param name="inListNameOrTag">The name or tag of the list.</param>
        /// <param name="inItem">The object used to create the list item.</param>
        /// <param name="inContainer">The container that has the <b>list</b></param>
        public void BringListChildIntoView(string inListNameOrTag, object inItem, FrameworkElement inContainer)
        {
            // Tries to find child by name
            FrameworkElement list = GetDescendantOfElement_ByName(inListNameOrTag, inContainer);
            // Failed by name - tries by Tag
            if (list == null) list = GetDescendantOfElement_ByTag(inListNameOrTag, inContainer);

            // Failed both by name and child
            if (list == null) throw new Exception($"Could not find list with name nor tag {inListNameOrTag} as a child to the given bounded element {inContainer} {inContainer.Name}."); // Could not find the list not by x:Name, not by tag.

            BringListChildIntoView(list, inItem);
        }
        public void BringListChildIntoView(FrameworkElement inList, object inItem)
        {
            // Finds the item in the list
            FrameworkElement listItem = null;
            switch (inList)
            {
                case ListView listView:
                    listItem = listView.ItemContainerGenerator.ContainerFromItem(inItem) as FrameworkElement;
                    break;

                case ListBox listBox:
                    listItem = listBox.ItemContainerGenerator.ContainerFromItem(inItem) as FrameworkElement;
                    break;

                case ItemsControl itemsControl:
                    listItem = itemsControl.ItemContainerGenerator.ContainerFromItem(inItem) as FrameworkElement;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"The list type {inList.GetType()} is not supported as a list container.");
            }
            
            if (listItem != null) listItem.Dispatcher.Invoke(() => { listItem.BringIntoView(); });
        }

        public FrameworkElement GetDescendantOfReferencedFrameworkElement_ByName(string inName, string inReferencedParentFrameworkElementName)
        {
            if (!_referencedElements.Keys.Contains(inReferencedParentFrameworkElementName)) throw new ArgumentOutOfRangeException($"Could not find a bounded FrameworkElement called {inReferencedParentFrameworkElementName}. Add it using the {nameof(SaveReferenceToElement)} method first.");
            FrameworkElement baseElement = _referencedElements[inReferencedParentFrameworkElementName];

            return GetDescendantOfElement_ByName(inName, baseElement);
        }
        public FrameworkElement GetDescendantOfElement_ByName(string inName, FrameworkElement inParentFrameworkElement = null)
        {
            FrameworkElement parent = inParentFrameworkElement ?? Application.Current.MainWindow;

            // Tries to find child by name
            FrameworkElement lf_GetBaseElementNamedChild() => parent.GetAllChildren().OfType<FrameworkElement>().FirstOrDefault(a => a.Name == inName);
            FrameworkElement child = parent.Dispatcher.Invoke(lf_GetBaseElementNamedChild);

            return child;
        }

        public FrameworkElement GetDescendantOfReferencedFrameworkElement_ByTag(string inTag, string inReferencedParentFrameworkElementName)
        {
            if (!_referencedElements.Keys.Contains(inReferencedParentFrameworkElementName)) throw new ArgumentOutOfRangeException($"Could not find a bounded FrameworkElement called {inReferencedParentFrameworkElementName}. Add it using the {nameof(SaveReferenceToElement)} method first.");
            FrameworkElement baseElement = _referencedElements[inReferencedParentFrameworkElementName];

            return GetDescendantOfElement_ByTag(inTag, baseElement);
        }
        public FrameworkElement GetDescendantOfElement_ByTag(string inTag, FrameworkElement inParentFrameworkElement = null)
        {
            FrameworkElement parent = inParentFrameworkElement ?? Application.Current.MainWindow;

            // Tries to find child by name
            FrameworkElement lf_GetBaseElementNamedChild() => parent.GetAllChildren().OfType<FrameworkElement>().FirstOrDefault(a => (a.Tag as string) == inTag);
            FrameworkElement child = parent.Dispatcher.Invoke(lf_GetBaseElementNamedChild);

            return child;
        }

        public void OnBeginCommand(object inEventData = null)
        {
            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Publish(new BindCommandEventArgs(this, inEventData));
        }
        public void OnEndCommand(object inEventData = null)
        {
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Publish(new BindCommandEventArgs(this, inEventData));
        }
        public void OnGenericCommand(object inEventData = null)
        {
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, inEventData));
        }
        public void OnMessage(string inTitle, string inMessage, object inEventData = null)
        {
            EventAggregatorSingleton.I.GetEvent<BindMessageEvent>().Publish(new BindMessageEventArgs(inTitle, inMessage, this, inEventData));
        }
    }
}
