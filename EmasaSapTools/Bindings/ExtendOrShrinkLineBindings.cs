using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class ExtendOrShrinkLineBindings : BindableSingleton<ExtendOrShrinkLineBindings>
    {
        private ExtendOrShrinkLineBindings(){}
        public override void SetOrReset()
        {
            ExtendDistance = 2;
            OnlyTheLine_IsChecked = true;
            ZAlignProjection_IsChecked = true;

            
        }

        private double? _ExtendDistance;public double? ExtendDistance { get => _ExtendDistance; set => SetProperty(ref _ExtendDistance, value); }

        private bool _OnlyTheLine_IsChecked;public bool OnlyTheLine_IsChecked { get => _OnlyTheLine_IsChecked; set => SetProperty(ref _OnlyTheLine_IsChecked, value); }

        private bool _AllElements_IsChecked;public bool AllElements_IsChecked { get => _AllElements_IsChecked; set => SetProperty(ref _AllElements_IsChecked, value); }

        private bool _Closest_IsChecked;public bool Closest_IsChecked { get => _Closest_IsChecked; set => SetProperty(ref _Closest_IsChecked, value); }

        private bool _ZAlignProjection_IsChecked;public bool ZAlignProjection_IsChecked { get => _ZAlignProjection_IsChecked; set => SetProperty(ref _ZAlignProjection_IsChecked, value); }
    }
}