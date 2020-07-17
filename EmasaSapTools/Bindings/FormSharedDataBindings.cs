using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Others;

namespace EmasaSapTools.Bindings
{
    public class FormSharedDataBindings : BindableSingleton<FormSharedDataBindings>
    {
        private FormSharedDataBindings() { }
        public override void SetOrReset()
        {

        }

        #region Data That is Shared

        private readonly FastObservableCollection<string> _sap2000GroupList = new FastObservableCollection<string>();
        public FastObservableCollection<string> Sap2000GroupList
        {
            get => _sap2000GroupList;
        }

        private readonly FastObservableCollection<string> _sap2000CaseList = new FastObservableCollection<string>();
        public FastObservableCollection<string> Sap2000CaseList
        {
            get => _sap2000CaseList;
        }

        #endregion
    }
}
