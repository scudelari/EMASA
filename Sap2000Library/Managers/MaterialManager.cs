using System;
using System.Collections.Generic;
using System.Linq;
using BaseWPFLibrary;
using BaseWPFLibrary.Annotations;
using BaseWPFLibrary.Bindings;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class MaterialManager : SapManagerBase
    {
        internal MaterialManager(S2KModel model) : base(model) { }

        public string AddNewMaterial(MatTypeEnum matType, string Region, string Standard, string Grade, string Username = null)
        {
            string name = null;

            // Default Sap2000 value
            if (string.IsNullOrWhiteSpace(Username)) Username = "";

            int ret = SapApi.PropMaterial.AddMaterial(ref name, (eMatType)(int)matType, Region, Standard, Grade, Username);

            if (ret == 0)
            {
                return name;
            }
            else
            {
                return null;
            }
        }
        public bool SetIsotropicMaterialProperties(string Name, double E, double poisson = 0.3, double a = 6.500E-06d)
        {
            int ret = SapApi.PropMaterial.SetMPIsotropic(Name, E, poisson, a);

            return ret == 0;
        }
        public bool SetOtherSteelMaterialProperties(string inName, double inFy, double inFu, double inExpectedFy, double inExpectedFu)
        {
            return 0 == SapApi.PropMaterial.SetOSteel_1(inName, inFy, inFu, inFy,inFu, 0, 0, 0, 0, 0, 0);
        }
        public string SetMaterial(MatTypeEnum matType, string Username)
        {
            // Default Sap2000 value
            if (string.IsNullOrWhiteSpace(Username)) return null;

            int ret = SapApi.PropMaterial.SetMaterial(Username, (eMatType)(int)matType);

            if (ret == 0)
            {
                return Username;
            }
            else
            {
                return null;
            }
        }
        public bool DeleteMaterial(string Name)
        {
            if (SapApi.PropMaterial.Delete(Name) == 0) return true;
            else return false;
        }

        /// <summary>
        /// Deletes all defined materials. Note that Sap2000 breaks if there are no defined materials and one tries to open the Material dialog.
        /// </summary>
        /// <param name="matType">The type of materials to delete.</param>
        /// <returns>0 if all materials deleted. Otherwise, returns the number of remaining materials that could not be deleted.</returns>
        public int DeleteAllMaterials(MatTypeEnum? matType = null)
        {
            List<string> allMats = GetMaterialList(matType);

            int ret = 0;
            foreach (string mat in allMats)
            {
                if (!DeleteMaterial(mat)) ret++;
            }

            return ret;
        }

        public List<string> GetMaterialList(MatTypeEnum? matType = null, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Getting Material Name List");

            int count = 0;
            string[] names = null;

            int ret = -1;
            if (matType.HasValue)
            {
                ret = SapApi.PropMaterial.GetNameList(ref count, ref names, (eMatType)(int)matType.Value);
            }
            else
            {
                ret = SapApi.PropMaterial.GetNameList(ref count, ref names);
            }

            if (ret == 0 && names != null && names.Length != 0) return names.ToList();
            else return null;
        }

        public bool SoftenMaterial([NotNull] string inMatName, double inFactor)
        {
            if (inMatName == null) throw new ArgumentNullException(nameof(inMatName));

            double e = 0d, u = 0d, a = 0d, g = 0d;

            // Gets the material properties
            if (SapApi.PropMaterial.GetMPIsotropic(inMatName, ref e, ref u, ref a, ref g) != 0) throw new S2KHelperException($"Could not get the isotropic material properties of material {inMatName}.");

            if (SapApi.PropMaterial.SetMPIsotropic(inMatName, e * inFactor, u, a) != 0) throw new S2KHelperException($"Could not change the isotropic material properties of material {inMatName}.");

            return true;
        }
    }
}
