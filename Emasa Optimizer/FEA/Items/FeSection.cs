using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Items
{
    [Serializable]
    public abstract class FeSection : BindableBase, IEquatable<FeSection>, IComparable<FeSection>
    {
        public int CompareTo(FeSection other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return _area.CompareTo(other._area);
        }

        protected int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        protected string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _nameFixed = null;
        public string NameFixed => _nameFixed ?? (_nameFixed = Name.Replace('_', '.'));


        protected FeMaterial _material;
        public FeMaterial Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }

        public readonly Dictionary<string, double> Dimensions = new Dictionary<string, double>();
        public virtual double OuterDiameter => throw new NotImplementedException($"{this.GetType().Name} does not implement {nameof(MethodBase.GetCurrentMethod)}.");
        public virtual double Thickness => throw new NotImplementedException($"{this.GetType().Name} does not implement {nameof(MethodBase.GetCurrentMethod)}.");
        public double FirstDimension => Dimensions.First().Value;

        protected double _area;
        public virtual double Area
        {
            get => _area;
            set => SetProperty(ref _area, value);
        }

        protected double _torsionalConstant;
        public virtual double TorsionalConstant
        {
            get => _torsionalConstant;
            set => SetProperty(ref _torsionalConstant, value);
        }

        protected double _momentInertia2;
        public virtual double MomentInertia2
        {
            get => _momentInertia2;
            set => SetProperty(ref _momentInertia2, value);
        }

        protected double _momentInertia3;
        public virtual double MomentInertia3
        {
            get => _momentInertia3;
            set => SetProperty(ref _momentInertia3, value);
        }

        protected double _productOfInertia23;
        public virtual double ProductOfInertia23
        {
            get => _productOfInertia23;
            set => SetProperty(ref _productOfInertia23, value);
        }

        protected double _shearArea2;
        public virtual double ShearArea2
        {
            get => _shearArea2;
            set => SetProperty(ref _shearArea2, value);
        }

        protected double _shearArea3;
        public virtual double ShearArea3
        {
            get => _shearArea3;
            set => SetProperty(ref _shearArea3, value);
        }

        protected double _sectionModulus2;
        public virtual double SectionModulus2
        {
            get => _sectionModulus2;
            set => SetProperty(ref _sectionModulus2, value);
        }

        protected double _sectionModulus3;
        public virtual double SectionModulus3
        {
            get => _sectionModulus3;
            set => SetProperty(ref _sectionModulus3, value);
        }

        protected double _plasticModulus2;
        public virtual double PlasticModulus2
        {
            get => _plasticModulus2;
            set => SetProperty(ref _plasticModulus2, value);
        }

        protected double _plasticModulus3;
        public virtual double PlasticModulus3
        {
            get => _plasticModulus3;
            set => SetProperty(ref _plasticModulus3, value);
        }

        protected double _radiusGyration2;
        public virtual double RadiusGyration2
        {
            get => _radiusGyration2;
            set => SetProperty(ref _radiusGyration2, value);
        }

        protected double _radiusGyration3;
        public virtual double RadiusGyration3
        {
            get => _radiusGyration3;
            set => SetProperty(ref _radiusGyration3, value);
        }

        protected double _shearCenterEccentricity;
        public virtual double ShearCenterEccentricity
        {
            get => _shearCenterEccentricity;
            set => SetProperty(ref _shearCenterEccentricity, value);
        }

        public double LeastGyrationRadius { get; set; }

        public string DimensionString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, double> keyValuePair in Dimensions)
                {
                    sb.Append($"<{keyValuePair.Key}:{keyValuePair.Value:F4}>");
                }

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return $"{Id}:{GetType().Name}:[{DimensionString}]";
        }

        #region Specific to Ansys
        public virtual string AnsysSecTypeLine => throw new NotImplementedException();
        public virtual string AnsysSecDataLine => throw new NotImplementedException();

        #endregion

        #region Equality - Based on ID
        public bool Equals(FeSection other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _id == other._id;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeSection)obj);
        }
        public override int GetHashCode()
        {

            return GetType().ToString().GetHashCode() ^ _id.GetHashCode();
        }
        public static bool operator ==(FeSection left, FeSection right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeSection left, FeSection right)
        {
            return !Equals(left, right);
        } 
        #endregion

        public string NameChunk(int inIndex, string inSeparator = "X")
        {
            return Name.Split(new []{inSeparator}, StringSplitOptions.RemoveEmptyEntries)[inIndex];
        }

        public string NameFixedChunk(int inIndex, string inSeparator = "X")
        {
            return NameFixed.Split(new[] { inSeparator }, StringSplitOptions.RemoveEmptyEntries)[inIndex];
        }
    }

    public class FeSection_ComparerByFirstNameChunk : IEqualityComparer<FeSection>
    {
        public bool Equals(FeSection x, FeSection y)
        {
            
            if (x == null && y == null) return true;

            if (x == null || y == null) return false; 

            return x.NameChunk(0) == y.NameChunk(0);
        }

        public int GetHashCode(FeSection obj)
        {
            return obj.NameChunk(0).GetHashCode();
        }
    }

}
