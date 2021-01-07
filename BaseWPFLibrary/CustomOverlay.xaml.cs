using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BaseWPFLibrary.Bindings;

namespace BaseWPFLibrary
{
    /// <summary>
    /// Interaction logic for CustomOverlay.xaml
    /// </summary>
    public partial class CustomOverlay : UserControl
    {
        public CustomOverlay()
        {
            // Saves a reference to the this BusyOverlay Instance
            CustomOverlayBindings.SaveReferenceToElement(this, "UserControlInternalName_CustomOverlay");

            InitializeComponent();

            // Sets the data context of the content grid to this class
            CustomOverlay_ContentGrid.DataContext = this;
            // Sets the data context of the content grid to this class
            CustomOverlay_AdditionalContentPresenter.DataContext = this;

            // Sets this visibility to hidden
            this.Visibility = Visibility.Collapsed;
        }

        public object AdditionalContent
        {
            get => (object)GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }
        public static readonly DependencyProperty AdditionalContentProperty = DependencyProperty.Register("AdditionalContent", typeof(object), typeof(CustomOverlay), new PropertyMetadata(null));
        public void SetAdditionalContent_DataContext(object inDataContext)
        {
            // The additional content already begins with the Grid

            if (AdditionalContent is FrameworkElement addContent)
            {
                addContent.DataContext = inDataContext;
            }
        }


        #region Overlay Config Variables
        /// <summary>
        /// The margin of the overlay background from the corners of the container.
        /// </summary>
        public Thickness ContentWindowMargin
        {
            get => (Thickness)GetValue(ContentWindowMarginProperty);
            set => SetValue(ContentWindowMarginProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty ContentWindowMarginProperty = DependencyProperty.Register("ContentWindowMargin", typeof(Thickness), typeof(CustomOverlay), new PropertyMetadata(new Thickness(40)));


        /// <summary>
        /// The color of the overlay Background.
        /// </summary>
        public Brush OverlayBackground
        {
            get => (Brush)GetValue(OverlayBackgroundProperty);
            set => SetValue(OverlayBackgroundProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty OverlayBackgroundProperty = DependencyProperty.Register("OverlayBackground", typeof(Brush), typeof(CustomOverlay), new PropertyMetadata(new SolidColorBrush(Colors.DimGray)));


        /// <summary>
        /// The opacity of the overlay Background.
        /// </summary>
        public double OverlayBackgroundOpacity
        {
            get => (double)GetValue(OverlayBackgroundOpacityProperty);
            set => SetValue(OverlayBackgroundOpacityProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty OverlayBackgroundOpacityProperty = DependencyProperty.Register("OverlayBackgroundOpacity", typeof(double), typeof(CustomOverlay), new PropertyMetadata(0.7d));


        public HorizontalAlignment ContentHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(ContentHorizontalAlignmentProperty);
            set => SetValue(ContentHorizontalAlignmentProperty, value);
        }
        public static readonly DependencyProperty ContentHorizontalAlignmentProperty = DependencyProperty.Register("ContentHorizontalAlignment", typeof(HorizontalAlignment), typeof(CustomOverlay), new PropertyMetadata(HorizontalAlignment.Stretch));


        public VerticalAlignment ContentVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(ContentVerticalAlignmentProperty);
            set => SetValue(ContentVerticalAlignmentProperty, value);
        }
        public static readonly DependencyProperty ContentVerticalAlignmentProperty = DependencyProperty.Register("ContentVerticalAlignment", typeof(VerticalAlignment), typeof(CustomOverlay), new PropertyMetadata(VerticalAlignment.Stretch));


        #endregion
    }
}
