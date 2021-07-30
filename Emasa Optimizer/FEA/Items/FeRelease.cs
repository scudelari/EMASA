using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA.Items
{
    public class FeRelease : BindableBase, IEquatable<FeRelease>
    {
        public FeRelease(bool inReleased = false)
        {
            _released = inReleased;
        }

        private bool _released;
        public bool Released
        {
            get => _released;
            set => SetProperty(ref _released, value);
        }
        
        public void IncorporateRelease(FeRelease inOtherRelease)
        {
            Released = this.Released || inOtherRelease.Released;
        }

        public override string ToString()
        {
            if (Released) return "Released";
            else return "Fixed";
        }

        public string SimpleName => this.ToString();
        public string WpfName => $"{GetType().Name}";

        public bool Equals(FeRelease other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _released == other._released;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeRelease) obj);
        }
        public override int GetHashCode()
        {
            return _released.GetHashCode();
        }
        public static bool operator ==(FeRelease left, FeRelease right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(FeRelease left, FeRelease right)
        {
            return !Equals(left, right);
        }
    }
}
