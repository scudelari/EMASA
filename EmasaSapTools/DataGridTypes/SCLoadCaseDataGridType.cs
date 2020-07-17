using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EmasaSapTools.DataGridTypes
{
    public class ScLoadCaseDataGridType
    {
        public string Name { get; set; }
        public double DeadMult { get; set; }
        public double LiveMult { get; set; }
        public double WindMult { get; set; }
        public double NotionalMult { get; set; }
        public double TemperatureMult { get; set; }
        public double StrainMult { get; set; }
        public string BaseName { get; set; }
        public bool Active { get; set; }

        public List<(string CaseName, double Mult)> OtherCaseList { get; } = new List<(string CaseName, double Mult)>();

        public string Others
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < OtherCaseList.Count; i++)
                {
                    (string CaseName, double Mult) item = ((string CaseName, double Mult)) OtherCaseList[i];

                    sb.Append(item.CaseName);
                    sb.Append($"({item.Mult})");
                    if (i != OtherCaseList.Count - 1) sb.Append(" % ");
                }

                if (sb.Length > 0) return "...";
                else return "";
                //return sb.ToString();
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                try
                {
                    Regex pattern = new Regex(@"(?<Case>[^\(]*)\({1}(?<Mult>\d*\.?\d*){1}\)( % )?");

                    foreach (Match match in pattern.Matches(value))
                        OtherCaseList.Add((match.Groups["Case"].Value, double.Parse(match.Groups["Mult"].Value)));
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The text in the Others column is invalid!");
                }
            }
        }
    }
}