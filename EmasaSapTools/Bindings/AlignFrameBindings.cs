using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library;

namespace EmasaSapTools.Bindings
{
    public class AlignFrameBindings : BindableSingleton<AlignFrameBindings>
    {
        private AlignFrameBindings(){}
        public override void SetOrReset()
        {
            Plane12_IsChecked = true;
            FlipLastButton_IsEnabled = false;

            
        }

        private bool _Plane12_IsChecked;public bool Plane12_IsChecked { get => _Plane12_IsChecked; set => SetProperty(ref _Plane12_IsChecked, value); }

        private bool _Plane13_IsChecked;public bool Plane13_IsChecked { get => _Plane13_IsChecked; set => SetProperty(ref _Plane13_IsChecked, value); }

        private bool _FlipLastButton_IsEnabled;public bool FlipLastButton_IsEnabled { get => _FlipLastButton_IsEnabled; set => SetProperty(ref _FlipLastButton_IsEnabled, value); }

        public FrameAdvancedAxes_Plane2 FrameAlignPlaneOption
        {
            get
            {
                if (Plane12_IsChecked) return FrameAdvancedAxes_Plane2.Plane12;
                if (Plane13_IsChecked) return FrameAdvancedAxes_Plane2.Plane13;
                return FrameAdvancedAxes_Plane2.Plane12;
            }
        }
    }
}