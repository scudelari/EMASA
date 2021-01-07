using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emasa_Optimizer.FEA.Items
{
    [Serializable]
    public class FeSectionPipe : FeSection
    {
        public FeSectionPipe(string inName, int inId, FeMaterial inMaterial,
            double inArea, double inTorsionalConstant, double inMomentInertia2, double inMomentInertia3, double inProductOfInertia23, double inShearArea2, double inShearArea3, double inSectionModulus2, double inSectionModulus3, double inPlasticModulus2, double inPlasticModulus3, double inRadiusGyration2, double inRadiusGyration3, double inShearCenterEccentricity,
            double inDimOuterDiameter, double inDimThickness)
        {
            _id = inId;
            _name = inName;
            _material = FeMaterial.GetMaterialByName("S355");

            _area = inArea;
            _torsionalConstant = inTorsionalConstant;
            _momentInertia2 = inMomentInertia2;
            _momentInertia3 = inMomentInertia3;
            _productOfInertia23 = inProductOfInertia23;
            _shearArea2 = inShearArea2;
            _shearArea3 = inShearArea3;
            _sectionModulus2 = inSectionModulus2;
            _sectionModulus3 = inSectionModulus3;
            _plasticModulus2 = inPlasticModulus2;
            _plasticModulus3 = inPlasticModulus3;
            _radiusGyration2 = inRadiusGyration2;
            _radiusGyration3 = inRadiusGyration3;
            _shearCenterEccentricity = inShearCenterEccentricity;

            Dimensions.Add("OuterDiameter", inDimOuterDiameter);
            Dimensions.Add("Thickness", inDimThickness);

            LeastGyrationRadius = Math.Min(_radiusGyration2, _radiusGyration3);
        }
        //_sectionList.Add(new FeSectionPipe("244_5X5", SectionIdCounter++, FeMaterial.GetMaterialByName("S355"),AREA= 0.00376, 0.000053970004, 0.000026989998, 0.000026989998, 0, 0.00192029895019531, 0.00192029895019531, 0.000220777079754601, 0.000220777079754601, 0.00028684290625, 0.00028684290625, 0.0847242052629991, 0.0847242052629991, 0, 0.2445, 0.005));
        #region Specific to Ansys
        public override string AnsysSecTypeLine
        {
            get => $"SECTYPE,{Id},BEAM,CTUBE,P{Dimensions["OuterDiameter"]:F0}x{Dimensions["Thickness"]:F0},{Properties.Settings.Default.Default_Ansys_OptimizationSectionRefinementLevel}";
        }
        public override string AnsysSecDataLine
        {
            get => $"SECDATA,{(Dimensions["OuterDiameter"] / 2d) - Dimensions["Thickness"]:F6},{(Dimensions["OuterDiameter"] / 2d):F6},8";
        } 
        #endregion

        public override double OuterDiameter => Dimensions["OuterDiameter"];
        public override double Thickness => Dimensions["Thickness"];

        public override double TorsionalShearConstant => (Math.PI * Thickness * Math.Pow(OuterDiameter - Thickness,2d)) / 2d;

        private static List<FeSectionPipe> _allSectionsOfThisType = null;
        public static List<FeSectionPipe> GetAllSectionsOfThisType()
        {
            if (_allSectionsOfThisType == null) _allSectionsOfThisType = GetAllSections().OfType<FeSectionPipe>().ToList();
            return _allSectionsOfThisType;
        }

        private static string _ansysList = null;
        public static string GetFullAnsysTable()
        {
            if (_ansysList != null) return _ansysList;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("! START DEFINE SECTION DATA");
            sb.AppendLine("! -------------------------");
            sb.AppendLine("! Table Contains [1] Id, [2] MatId, [3] DefP1, [4] DefP2, [5] DefP3, [6] Area, [7] PMod2, [8] PMod3, [9] LeastGyr, [10-15] To Use for Calcs");
            sb.AppendLine();
            sb.AppendLine($"*DIM,sec_array,ARRAY,15,{GetAllSectionsOfThisType().Count}");

            List<FeSectionPipe> list = GetAllSectionsOfThisType();
            for (int index = 0; index < list.Count; index++)
            {
                FeSection s = list[index];
                sb.AppendLine($"sec_array(1,{index})={s.Id},{s.Material.Id},{(s.Dimensions["OuterDiameter"] / 2d) - s.Dimensions["Thickness"]:F6},{(s.Dimensions["OuterDiameter"] / 2d):F6},0,{s.Area},{s.PlasticModulus2},{s.PlasticModulus3},{s.LeastGyrationRadius}");
            }

            sb.AppendLine();
            sb.AppendLine("! -----------------------");
            sb.AppendLine("! END DEFINE SECTION DATA");

            _ansysList = sb.ToString();

            return _ansysList;
        }

        private string _family = null;
        public override string Family => _family ?? (_family = Name.Split(new[] {'X'}, StringSplitOptions.RemoveEmptyEntries)[0]);
    }
}
