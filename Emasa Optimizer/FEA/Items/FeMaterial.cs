using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Items
{
    [Serializable]
    public class FeMaterial : BindableBase, IEquatable<FeMaterial>
    {
        private static object _lock = new object();
        private static List<FeMaterial> _materialList;

        public static FeMaterial GetMaterialById(int inId)
        {
            return GetAllMaterials().First(a => a.Id == inId);
        }
        public static FeMaterial GetMaterialByName(string inMatName)
        {
            return GetAllMaterials().First(a => a.Name == inMatName);
        }

        public static List<FeMaterial> GetAllMaterials()
        {
            lock (_lock)
            {
                // If it is the first time, the list gets generated in the memory.
                if (_materialList == null)
                {
                    int MaterialIdCounter = 1;

                    _materialList = new List<FeMaterial>();
                    _materialList.Add(new FeMaterial(MaterialIdCounter++,
                        "S355",
                        7850, // kg/m3
                        2.1e11, // N/m2
                        0.3,    // Poisson
                        inFy: 3.55e8,    // N/mm2
                        inFu: 5.1e8,     // N/mm2
                        1.170E-05 // C^-1
                    ));
                }

                return _materialList;
            }
        }

        public FeMaterial(int inId, string inName, double inDensity, double inYoungModulus, double inPoisson, double inFy, double inFu, double inThermalCoefficient)
        {
            _id = inId;
            _name = inName;
            _density = inDensity;
            _youngModulus = inYoungModulus;
            _poisson = inPoisson;
            _fy = inFy;
            _fu = inFu;
            _thermalCoefficient = inThermalCoefficient;
        }

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        
        private double _youngModulus;
        public double YoungModulus
        {
            get => _youngModulus;
            set => SetProperty(ref _youngModulus, value);
        }
        public double YoungModulus_Soft => YoungModulus * Properties.Settings.Default.Default_FeMaterial_SoftMaterialMultiplier;

        private double _poisson;
        public double Poisson
        {
            get => _poisson;
            set => SetProperty(ref _poisson, value);
        }

        private double _density;
        public double Density
        {
            get => _density;
            set => SetProperty(ref _density, value);
        }
        
        private double _fy;
        public double Fy
        {
            get => _fy;
            set => SetProperty(ref _fy, value);
        }

        private double _fu;
        public double Fu
        {
            get => _fu;
            set => SetProperty(ref _fu, value);
        }

        private double _thermalCoefficient;
        public double ThermalCoefficient
        {
            get => _thermalCoefficient;
            set => SetProperty(ref _thermalCoefficient, value);
        }

        #region Equality - Based on the name
        public bool Equals(FeMaterial other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_name, other._name, StringComparison.InvariantCulture);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeMaterial)obj);
        }
        public override int GetHashCode()
        {
            return (GetType(), Name).GetHashCode();
        }
        public static bool operator ==(FeMaterial left, FeMaterial right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeMaterial left, FeMaterial right)
        {
            return !Equals(left, right);
        } 
        #endregion

        public override string ToString()
        {
            return $"{Name} E={YoungModulus} P={Poisson} Fy={Fy} Fu={Fu}";
        }

        public string WpfName => $"{GetType().Name} - {Name}";
    }
}
