using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace AccordHelper.FEA.Items
{
    [Serializable]
    public abstract class FeSection : BindableBase, IEquatable<FeSection>
    {
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

        protected FeMaterial _material;
        public FeMaterial Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }

        public readonly Dictionary<string, double> Dimensions = new Dictionary<string, double>();

        public abstract double Area { get; set; }
        public abstract double TorsionalConstant { get; set; }
        public abstract double MomentInertia2 { get; set; }
        public abstract double MomentInertia3 { get; set; }
        public abstract double ProductOfInertia23 { get; set; }
        public abstract double ShearArea2 { get; set; }
        public abstract double ShearArea3 { get; set; }
        public abstract double SectionModulus2 { get; set; }
        public abstract double SectionModulus3 { get; set; }
        public abstract double PlasticModulus2 { get; set; }
        public abstract double PlasticModulus3 { get; set; }
        public abstract double RadiusGyration2 { get; set; }
        public abstract double RadiusGyration3 { get; set; }
        public abstract double ShearCenterEccentricity { get; set; }

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

        public virtual string AnsysSecTypeLine { get { throw new NotImplementedException();} }
        public virtual string AnsysSecDataLine { get { throw new NotImplementedException();} }

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
            return _id;
        }

        public static bool operator ==(FeSection left, FeSection right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FeSection left, FeSection right)
        {
            return !Equals(left, right);
        }
    }
}
