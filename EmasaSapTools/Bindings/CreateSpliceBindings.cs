using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class CreateSpliceBindings : BindableSingleton<CreateSpliceBindings>
    {
        private CreateSpliceBindings()
        {
        }

        public override void SetOrReset()
        {
            TotalSpliceLength = 2d;
            
        }

        private double _TotalSpliceLength;public double TotalSpliceLength { get => _TotalSpliceLength; set => SetProperty(ref _TotalSpliceLength, value); }
    }
}