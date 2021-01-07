using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Emasa_Optimizer.FEA.Items;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.WpfResources;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class LineList_GhGeom_ParamDef : GhGeom_ParamDefBase, IProblemConfig_CombinableVariable, IGhElement_HasFeRestraint
    {
        public override string TypeName => "LineList";
        public override string Wpf_TypeNameString => "Lines";

        public LineList_GhGeom_ParamDef(string inName, FeRestraint inDefaultRestraint = null) : base(inName)
        {
            if (inDefaultRestraint == null) Restraint = new FeRestraint(); // All False
            else Restraint = inDefaultRestraint;

            WpfFeSections_ToCombine = CollectionViewSource.GetDefaultView(_feSections_ToCombine);
            WpfFeSections_ToCombine.SortDescriptions.Add(new SortDescription("FirstSortDimension", ListSortDirection.Ascending));
            WpfFeSections_ToCombine.SortDescriptions.Add(new SortDescription("SecondSortDimension", ListSortDirection.Ascending));
        }


        #region Finite Element Assignments
        public FeRestraint Restraint { get; set; }
        #endregion
        

        #region Problem Configuration Combination Assignments
        private FastObservableCollection<FeSection> _feSections_ToCombine { get; } = new FastObservableCollection<FeSection>();
        public int WpfFeSections_ToCombine_Count => _feSections_ToCombine.Count;
        public ICollectionView WpfFeSections_ToCombine { get; }

        public void Wpf_AddSelectedSectionsCombine()
        {
            _feSections_ToCombine.AddItemsIfNew(AppSS.I.PcOpt.WpfSelected_Sections);
            RaisePropertyChanged("WpfFeSections_ToCombine_Count");
        }
        public void Wpf_ClearSelectedSectionsCombine()
        {
            _feSections_ToCombine.Clear();
            RaisePropertyChanged("WpfFeSections_ToCombine_Count");
        }
        #endregion

        public List<ProblemConfig_ElementCombinationValue> FlatCombinationList
        {
            get
            {
                List<ProblemConfig_ElementCombinationValue> toRet = new List<ProblemConfig_ElementCombinationValue>();

                foreach (FeSection feSection in _feSections_ToCombine)
                {
                    toRet.Add(new ProblemConfig_GhLineListConfigValues(this, feSection));
                }

                return toRet;
            }
        }
    }
}
