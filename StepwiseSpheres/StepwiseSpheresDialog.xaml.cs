using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BaseWPFLibrary;
using BaseWPFLibrary.Events;
using Prism.Events;
using StepwiseSpheres.Bindings;

namespace StepwiseSpheres
{
    /// <summary>
    /// Interaction logic for StepwiseSpheresDialog.xaml
    /// </summary>
    public partial class StepwiseSpheresDialog : Window
    {
        public StepwiseSpheresDialog()
        {
            Thread.CurrentThread.Name = "StepwiseFormThread";
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            StepwiseFormBindings.Start(ContentGrid);

            // Subscribes to the GLOBAL events from the binders
            EventAggregatorSingleton.I.GetEvent<BindBeginCommandEvent>().Subscribe(BindBeginCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindEndCommandEvent>().Subscribe(BindEndCommandEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindMessageEvent>().Subscribe(BindMessageEventHandler, ThreadOption.UIThread);
            EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Subscribe(BindGenericCommandEventHandler);
        }

        #region Revit Communication Objects
        public ExternalCommandData Revit_ExternalCommandData 
        {
            get
            {
                return StepwiseFormBindings.I.Revit_ExternalCommandData;
            }
            set
            {
                StepwiseFormBindings.I.Revit_ExternalCommandData = value;
            }
        }
        public ElementSet Revit_ElementSet
        {
            get
            {
                return StepwiseFormBindings.I.Revit_ElementSet;
            }
            set
            {
                StepwiseFormBindings.I.Revit_ElementSet = value;
            }
        }
        public string Revit_OutputMessage {get => StepwiseFormBindings.I.Revit_OutputMessage; } 
        #endregion

        #region Messages
        private void BindBeginCommandEventHandler(BindCommandEventArgs inObj)
        {
            BusyOverlay.ShowOverlay();
        }
        private void BindEndCommandEventHandler(BindCommandEventArgs inObj)
        {
            BusyOverlay.HideOverlayAndReset();
        }
        private void BindMessageEventHandler(BindMessageEventArgs inObj)
        {
            // If a message came along, we show the message overlay
            if (inObj.Title != null && inObj.Message != null)
            {
                MessageOverlay.ShowOverlay(inObj.Title, inObj.Message);
            }
        }
        private void BindGenericCommandEventHandler(BindCommandEventArgs inObj)
        {
        } 
        #endregion
    }
}
