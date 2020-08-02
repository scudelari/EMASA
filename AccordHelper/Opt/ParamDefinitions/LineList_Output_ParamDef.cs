using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Items;
using BaseWPFLibrary.Others;

namespace AccordHelper.Opt.ParamDefinitions
{
    public class LineList_Output_ParamDef : Output_ParamDefBase
    {
        public LineList_Output_ParamDef(string inName, List<string> inDefaultSelectedSectionNames = null) : base(inName)
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
        }

        private void SelectedSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("Display_PossibleSections");
            RaisePropertyChanged("IsDeterminedSection");
            RaisePropertyChanged("SelectedCount");
        }

        public override string TypeName => "LineList";

        public FastObservableCollection<FeSection> SelectedSections { get; } = new FastObservableCollection<FeSection>();
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
                checkList.AddRange(SelectedSections.OrderBy(a => a.Name));

                foreach (string od in checkList.Select(a => a.NameChunk(0)).Distinct())
                {
                    int count = checkList.Count(a => a.NameChunk(0) == od);

                    if (count == 1) sb.Append(checkList.First(a => a.NameChunk(0) == od).Name);
                    else sb.Append($"{od}[{count}]");

                    sb.Append(" ,");
                }

                // Removes the last " ,"
                sb.Remove(sb.Length - 2, 2);

                return sb.ToString();
            }
        }
    }
}
