using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class RenameItemsBindings : BindableSingleton<RenameItemsBindings>
    {
        private RenameItemsBindings(){}
        public override void SetOrReset()
        {
            StartCountValue = 100000;
            RenameSelectedIsChecked = true;

            RegexToFind = "KH_";
            RegexRenameToRand = true;

            
        }


        private int _StartCountValue;public int StartCountValue { get => _StartCountValue; set => SetProperty(ref _StartCountValue, value); }

        private bool _RenameSelectedIsChecked;public bool RenameSelectedIsChecked { get => _RenameSelectedIsChecked; set => SetProperty(ref _RenameSelectedIsChecked, value); }

        private bool _RenameAllIsChecked;public bool RenameAllIsChecked { get => _RenameAllIsChecked; set => SetProperty(ref _RenameAllIsChecked, value); }

        private int _LargestPoint;public int LargestPoint { get => _LargestPoint; set => SetProperty(ref _LargestPoint, value); }

        private int _LargestFrame;public int LargestFrame { get => _LargestFrame; set => SetProperty(ref _LargestFrame, value); }

        private string _RegexToFind;public string RegexToFind { get => _RegexToFind; set => SetProperty(ref _RegexToFind, value); }

        private bool _RegexRenameToRand;public bool RegexRenameToRand { get => _RegexRenameToRand; set => SetProperty(ref _RegexRenameToRand, value); }

        private bool _RegexJustRemoveRegex;public bool RegexJustRemoveRegex { get => _RegexJustRemoveRegex; set => SetProperty(ref _RegexJustRemoveRegex, value); }
    }
}