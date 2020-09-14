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
using Emasa_Optimizer.WpfResources;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public class LineList_GhGeom_ParamDef : GhGeom_ParamDefBase
    {
        public LineList_GhGeom_ParamDef(string inName, FeRestraint inDefaultRestraint = null , List<string> inDefaultSelectedSectionNames = null) : base(inName)
        {
            if (inDefaultSelectedSectionNames == null || inDefaultSelectedSectionNames.Count == 0)
            {
                // Adds ALL sections
                SelectedSections.AddItems(FeSectionPipe.GetAllSections());
            }
            else
            {

                SelectedSections.AddItems((from a in FeSectionPipe.GetAllSections()
                                                    where inDefaultSelectedSectionNames.Contains(a.Name)
                                                       select a).ToList());
            }

            SelectedSections.CollectionChanged += SelectedSections_CollectionChanged;

            if (inDefaultRestraint == null) Restraint = new FeRestraint(); // All False
            else Restraint = inDefaultRestraint;

            #region Wpf Helpers
            SectionRegexFilterText = ".*";
            AvailableSectionList_View = (new CollectionViewSource()
                {
                Source = ListDescriptionStaticHolder.ListDescSingleton.FeSectionListEnumDescriptions
                }).View;
            AvailableSectionList_View.SortDescriptions.Add(new SortDescription("FirstDimension", ListSortDirection.Ascending));
            AvailableSectionList_View.Filter += inO => inO is FeSection sec && _sectionRegexFilter.IsMatch(sec.NameFixed);
            #endregion
        }
        
        public override string TypeName => "LineList";

        #region Finite Element Assignments
        public FeRestraint Restraint { get; set; }
        private FeSection _optimizationSection;
        public FeSection OptimizationSection
        {
            get => _optimizationSection;
            set => SetProperty(ref _optimizationSection, value);
        }

        public FastObservableCollection<FeSection> SelectedSections { get; } = new FastObservableCollection<FeSection>();
        private void SelectedSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("Display_PossibleSections");
            RaisePropertyChanged("IsDeterminedSection");
            RaisePropertyChanged("SelectedCount");
        }

        public void SelectAllSections()
        {
            SelectedSections.AddItems(FeSectionPipe.GetAllSections(), true);
        }
        public void SelectFamilyMedian()
        {
            List<string> chunk0s = (from a in FeSectionPipe.GetAllSections() select a.NameChunk(0)).Distinct().ToList();

            List<FeSection> toSelect = new List<FeSection>();

            foreach (string c1 in chunk0s)
            {
                List<FeSection> familyMembers = (from a in FeSectionPipe.GetAllSections() where a.NameChunk(0) == c1 select a).ToList();

                toSelect.Add(familyMembers[familyMembers.Count / 2]);
            }

            SelectedSections.AddItems(toSelect, true);
        }

        public bool IsDeterminedSection => SelectedSections.Count == 1;
        public int SelectedCount => SelectedSections.Count;

        public string Display_PossibleSections
        {
            get
            {
                if (SelectedSections.Count == 0) return "None";
                if (SelectedSections.Count == FeSectionPipe.GetAllSections().Count) return "All";

                StringBuilder sb = new StringBuilder();

                List<FeSection> checkList = new List<FeSection>();
                checkList.AddRange(SelectedSections.OrderBy(a => a.NameFixed));

                foreach (string od in checkList.Select(a => a.NameFixedChunk(0)).Distinct())
                {
                    int count = checkList.Count(a => a.NameFixedChunk(0) == od);

                    if (count == 1) sb.Append(checkList.First(a => a.NameFixedChunk(0) == od).NameFixed);
                    else sb.Append($"{od}[{count}]");

                    sb.Append(" ,");
                }

                // Removes the last " ,"
                sb.Remove(sb.Length - 2, 2);

                if (sb.Length > 20) return SelectedSections.Count.ToString();

                return sb.ToString();
            }
        }

        private Visibility _sectionAssignment_Visibility = Visibility.Collapsed;
        public Visibility SectionAssignment_Visibility
        {
            get => _sectionAssignment_Visibility;
            set => SetProperty(ref _sectionAssignment_Visibility, value);
        }
        #endregion

        #region Wpf Helpers
        private Regex _sectionRegexFilter;
        private string _sectionRegexFilterText;
        public string SectionRegexFilterText
        {
            get => _sectionRegexFilterText;
            set
            {
                _sectionRegexFilter = new Regex(value);
                SetProperty(ref _sectionRegexFilterText, value);

                if (AvailableSectionList_View != null)
                {
                    AvailableSectionList_View.Refresh();
                    AvailableSectionList_View.MoveCurrentToFirst();
                }
            }
        }
        public ICollectionView AvailableSectionList_View { get; }
        #endregion
    }

    public class SectionCombination : IEquatable<SectionCombination>
    {
        public SectionCombination()
        {

        }

        public SortedList<LineList_GhGeom_ParamDef, FeSection> Combination { get; } = new SortedList<LineList_GhGeom_ParamDef, FeSection>();

        public bool Equals(SectionCombination other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Combination, other.Combination);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SectionCombination) obj);
        }
        public override int GetHashCode()
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return Combination.Aggregate(seed, (current, item) =>
                    (current * modifier) + item.Key.GetHashCode() + item.Value.GetHashCode());
            }

        }
        public static bool operator ==(SectionCombination left, SectionCombination right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(SectionCombination left, SectionCombination right)
        {
            return !Equals(left, right);
        }
    }
}
