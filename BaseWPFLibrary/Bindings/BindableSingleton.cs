using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Others;
using Prism.Mvvm;
using Prism.Events;

namespace BaseWPFLibrary.Bindings
{
    public abstract class BindableSingleton<K> : BindableBase where K : BindableSingleton<K>
    {
        private static K _instance;
        /// <summary>
        /// This is a lock that can be used by child classes to ensure thread safety.
        /// </summary>
        public static readonly object _padLock = new object();

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

        /// <summary>
        /// Must be called before using the binding container.
        /// </summary>
        /// <param name="inFe">A reference to the Bound FrameworkElement.</param>
        /// <param name="inSetToContext">If true, will set the DataContext of the Bound FrameworkElement to the singleton instance.</param>
        public static void Start(FrameworkElement inFe = null, bool inSetToContext = true)
        {
            lock (_padLock)
            {
                Type t = typeof(K);

                if (_instance != null)
                {
                    MethodInfo[] methods = t.GetMethods();
                    foreach (MethodInfo methodInfo in methods)
                    {
                        if (methodInfo.Name == "SetOrReset")
                            methodInfo.Invoke(methodInfo, null);
                    }

                    return;
                }

                // Ensure there are no public constructors...
                ConstructorInfo[] ctors = t.GetConstructors();
                if (ctors.Length > 0)
                {
                    throw new InvalidOperationException($"{t.Name} has at least one accessible ctor making it impossible to enforce singleton behaviour");
                }

                // Create an _instance via the private constructor
                _instance = (K)Activator.CreateInstance(t, true);
            }

            if (inFe != null)
            {
                _instance.BoundTo = inFe;
                if (inSetToContext) inFe.DataContext = _instance;
            }
        }
        
        private FrameworkElement _boundTo;
        public FrameworkElement BoundTo
        {
            get => _boundTo;
            private set
            {
                _boundTo = value;

                Validation.AddErrorHandler(value, ValidationErrorHandler);
            }
        }

        private void ValidationErrorHandler(object inSender, ValidationErrorEventArgs inE)
        {
            if (!(inSender is DependencyObject depSender)) return;

            bool AnyChildHasError = depSender.FindVisualChildren<DependencyObject>().Any(a => Validation.GetHasError(a));
            
            if (!AnyChildHasError != BoundToChildrenNoErrors)
            {
                // Will fire a INotifyPropertyChangedEvent
                BoundToChildrenNoErrors = !AnyChildHasError;
            }
        }

        /// <summary>
        /// This function is called by the constructor. It must be overwritten by the class that inherits this object.
        /// </summary>
        public abstract void SetOrReset();

        protected BindableSingleton()
        {
            SetOrReset();
        }

        private bool _boundToChildrenNoErrors = true;
        public bool BoundToChildrenNoErrors
        {
            get { lock (_padLock) { return _boundToChildrenNoErrors; } }
            set { lock (_padLock) { SetProperty(ref _boundToChildrenNoErrors, value); } }
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
