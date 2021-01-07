using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BaseWPFLibrary.Others;

namespace BaseWPFLibrary.Bindings
{
    public class CustomOverlayBindings : BindableSingleton<CustomOverlayBindings>
    {
        private CustomOverlayBindings()
        {
        }
        public override void SetOrReset()
        {
            Title = "Working ...";
        }

        // Special link to the FrameworkElement
        public CustomOverlay OverlayElement => (CustomOverlay)GetReferencedFrameworkElement("UserControlInternalName_CustomOverlay");

        private Visibility _overlayVisibility;
        public Visibility OverlayVisibility
        {
            get
            {
                lock (_padLock)
                {
                    return _overlayVisibility;
                }
            }
            set
            {
                lock (_padLock)
                {
                    SetProperty(ref _overlayVisibility, value);
                }
            }
        }

        private string _title;
        public string Title
        {
            get
            {
                lock (_padLock)
                {
                    return _title;
                }
            }
            set
            {
                lock (_padLock)
                {
                    SetProperty(ref _title, value);
                }
            }
        }

        public void HideOverlayAndReset()
        {
            // Changes the visibility directly to the element itself
            OverlayElement.Visibility = Visibility.Collapsed;

            Title = "Working ...";
        }
        public void ShowOverlay()
        {
            // Changes the visibility directly to the element itself
            OverlayElement.Visibility = Visibility.Visible;
        }

    }
}
