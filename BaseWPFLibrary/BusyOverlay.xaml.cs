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
using System.Windows.Navigation;
using System.Windows.Shapes;
using BaseWPFLibrary.Bindings;

namespace BaseWPFLibrary
{
    /// <summary>
    /// Interaction logic for BusyOverlay.xaml
    /// </summary>
    public partial class BusyOverlay : UserControl
    {
        public BusyOverlay()
        {
            // Saves a reference to the this BusyOverlay Instance
            BusyOverlayBindings.SaveReferenceToElement(this, "UserControlInternalName_BusyOverlay");

            InitializeComponent();

            // Sets the data context of the content grid to this class
            BusyOverlay_ContentGrid.DataContext = this;

            // Sets this visibility to hidden
            this.Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            BusyOverlayBindings.I.CancelOperation();
        }

        #region Overlay Config Variables
        /// <summary>
        /// Gets or sets the Label which is displayed next to the field
        /// </summary>
        public Thickness ContentWindowMargin
        {
            get => (Thickness)GetValue(ContentWindowMarginProperty);
            set => SetValue(ContentWindowMarginProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty ContentWindowMarginProperty = DependencyProperty.Register("ContentWindowMargin", typeof(Thickness), typeof(BusyOverlay), new PropertyMetadata(new Thickness(40)));


        /// <summary>
        /// Gets or sets the Label which is displayed next to the field
        /// </summary>
        public Brush OverlayBackground
        {
            get => (Brush)GetValue(OverlayBackgroundProperty);
            set => SetValue(OverlayBackgroundProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty OverlayBackgroundProperty = DependencyProperty.Register("OverlayBackground", typeof(Brush), typeof(BusyOverlay), new PropertyMetadata(new SolidColorBrush(Colors.DimGray)));


        /// <summary>
        /// Gets or sets the Label which is displayed next to the field
        /// </summary>
        public double OverlayBackgroundOpacity
        {
            get => (double)GetValue(OverlayBackgroundOpacityProperty);
            set => SetValue(OverlayBackgroundOpacityProperty, value);
        }
        /// <summary>
        /// Identified the Label dependency property
        /// </summary>
        public static readonly DependencyProperty OverlayBackgroundOpacityProperty = DependencyProperty.Register("OverlayBackgroundOpacity", typeof(double), typeof(BusyOverlay), new PropertyMetadata(0.7d));
        #endregion
    }
}