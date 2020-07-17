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
            InitializeComponent();
            CustomOverlayBindings.Start(this);
        }

        /// <summary>
        /// Gets or sets additional content for the UserControl
        /// </summary>
        public object AdditionalContent
        {
            get { return (object)GetValue(AdditionalContentProperty); }
            set { SetValue(AdditionalContentProperty, value); }
        }
        public static readonly DependencyProperty AdditionalContentProperty = DependencyProperty.Register("AdditionalContent", typeof(object), typeof(CustomOverlay), new PropertyMetadata(null));

        public double BackgroundOpacity
        {
            get => (double)GetValue(BackgroundOpacityProperty);
            set => SetValue(BackgroundOpacityProperty, value);
        }
        public static readonly DependencyProperty BackgroundOpacityProperty =
            DependencyProperty.Register("BackgroundOpacity", typeof(double), typeof(CustomOverlay), new PropertyMetadata(0.7d));


        public SolidColorBrush BackgroundColor
        {
            get => (SolidColorBrush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(SolidColorBrush), typeof(CustomOverlay), new PropertyMetadata(new SolidColorBrush(Colors.DimGray)));


        public HorizontalAlignment ContentHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(ContentHorizontalAlignmentProperty);
            set => SetValue(ContentHorizontalAlignmentProperty, value);
        }
        public static readonly DependencyProperty ContentHorizontalAlignmentProperty =
            DependencyProperty.Register("ContentHorizontalAlignment", typeof(HorizontalAlignment), typeof(CustomOverlay), new PropertyMetadata(HorizontalAlignment.Stretch));


        public VerticalAlignment ContentVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(ContentVerticalAlignmentProperty);
            set => SetValue(ContentVerticalAlignmentProperty, value);
        }
        public static readonly DependencyProperty ContentVerticalAlignmentProperty =
            DependencyProperty.Register("ContentVerticalAlignment", typeof(VerticalAlignment), typeof(CustomOverlay), new PropertyMetadata(VerticalAlignment.Stretch));

        public Thickness ContentMargin
        {
            get => (Thickness)GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }
        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register("ContentMargin", typeof(Thickness), typeof(CustomOverlay), new PropertyMetadata(new Thickness(0d)));


        public double TargetResidual
        {
            get => (double)GetValue(TargetResidualProperty);
            set => SetValue(TargetResidualProperty, value);
        }
        public static readonly DependencyProperty TargetResidualProperty =
            DependencyProperty.Register("TargetResidual", typeof(double), typeof(CustomOverlay), new PropertyMetadata(0.7d));


        public double CurrentResidual
        {
            get => (double)GetValue(CurrentResidualProperty);
            set => SetValue(CurrentResidualProperty, value);
        }
        public static readonly DependencyProperty CurrentResidualProperty =
            DependencyProperty.Register("CurrentResidual", typeof(double), typeof(CustomOverlay), new PropertyMetadata(0.7d));



        public void HideOverlayAndReset()
        {
            CustomOverlayBindings.I.HideOverlayAndReset();
        }
        public void ShowOverlay()
        {
            CustomOverlayBindings.I.ShowOverlay();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Binding binding = new Binding("OverlayVisibility");
            SetBinding(VisibilityProperty, binding);
        }
    }
}
