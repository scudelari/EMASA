using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library;

namespace EmasaSapTools.Bindings
{
    public class AlignAreaBindings : BindableSingleton<AlignAreaBindings>
    {
        private AlignAreaBindings()
        {
        }

        public override void SetOrReset()
        {
            Plane31_IsChecked = true;
            FlipLastButton_IsEnabled = false;
        }

        private bool _plane31_IsChecked;

        public bool Plane31_IsChecked
        {
            get => _plane31_IsChecked;
            set => SetProperty(ref _plane31_IsChecked, value);
        }

        private bool _Plane32_IsChecked;

        public bool Plane32_IsChecked
        {
            get => _Plane32_IsChecked;
            set => SetProperty(ref _Plane32_IsChecked, value);
        }

        private bool _FlipLastButton_IsEnabled;

        public bool FlipLastButton_IsEnabled
        {
            get => _FlipLastButton_IsEnabled;
            set => SetProperty(ref _FlipLastButton_IsEnabled, value);
        }

        public AreaAdvancedAxes_Plane AreaAlignPlaneOption
        {
            get
            {
                if (Plane31_IsChecked) return AreaAdvancedAxes_Plane.Plane31;
                if (Plane32_IsChecked) return AreaAdvancedAxes_Plane.Plane32;
                return AreaAdvancedAxes_Plane.Plane31;
            }
        }
    }
}