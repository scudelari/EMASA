using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class AlignJointBindings : BindableSingleton<AlignJointBindings>
    {
        private AlignJointBindings()
        {
        }

        public override void SetOrReset()
        {
            AlwaysUpwards_IsChecked = true;
            AlignPointFlipLast_IsEnabled = false;

            
        }

        private bool _AlwaysUpwards_IsChecked;public bool AlwaysUpwards_IsChecked { get => _AlwaysUpwards_IsChecked; set => SetProperty(ref _AlwaysUpwards_IsChecked, value); }

        private bool _AlignPointFlipLast_IsEnabled;public bool AlignPointFlipLast_IsEnabled { get => _AlignPointFlipLast_IsEnabled; set => SetProperty(ref _AlignPointFlipLast_IsEnabled, value); }
    }
}