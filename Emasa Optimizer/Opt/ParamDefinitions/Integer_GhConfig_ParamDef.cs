using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Items;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class Integer_GhConfig_ParamDef : GhConfig_ParamDefBase, IProblemConfig_CombinableVariable
    {
        public override int VarCount => 1;
        public override string TypeName => "Integer";

        public Integer_GhConfig_ParamDef(string inName) : base(inName)
        {
            WpfIntegers_ToCombine = CollectionViewSource.GetDefaultView(_integers_ToCombine);
            if (WpfIntegers_ToCombine is ListCollectionView lcv) lcv.CustomSort = Comparer<int>.Default;
        }

        #region Problem Configuration Combination Assignments
        private FastObservableCollection<int> _integers_ToCombine { get; } = new FastObservableCollection<int>();
        public ICollectionView WpfIntegers_ToCombine { get; }
        public int WpfIntegers_ToCombine_Count => _integers_ToCombine.Count;

        public void Wpf_AddIntegersCombine()
        {
            _integers_ToCombine.AddItemsIfNew(AppSS.I.PcOpt.WpfSelected_Integers);
            RaisePropertyChanged("WpfIntegers_ToCombine_Count");
        }
        public void Wpf_ClearSelectedIntegersCombine()
        {
            _integers_ToCombine.Clear();
            RaisePropertyChanged("WpfIntegers_ToCombine_Count");
        }
        #endregion

        public List<ProblemConfig_ElementCombinationValue> FlatCombinationList
        {
            get
            {
                List<ProblemConfig_ElementCombinationValue> toRet = new List<ProblemConfig_ElementCombinationValue>();

                foreach (int i in _integers_ToCombine)
                {
                    toRet.Add(new ProblemConfig_GhIntegerInputConfigValues(this, i));
                }

                return toRet;
            }
        }
    }
}
