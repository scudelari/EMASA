﻿extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Accord.Math.Optimization;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace AccordHelper.Opt.ParamDefinitions
{
    [Serializable]
    public abstract class ParamDefBase : BindableBase, IEquatable<ParamDefBase>
    {
        public virtual int VarCount { get => throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}"); }

        protected ParamDefBase(string inName)
        {
            Name = inName;
        }

        public abstract string TypeName { get; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool Equals(ParamDefBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GetType() == other.GetType() && _name == other._name;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ParamDefBase)obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return GetType().ToString().GetHashCode() ^ _name.GetHashCode();
            }
        }
        public override string ToString()
        {
            return $"{GetType().Name} : {Name}";
        }

        public static bool operator ==(ParamDefBase left, ParamDefBase right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(ParamDefBase left, ParamDefBase right)
        {
            return !Equals(left, right);
        }

        // Parameter Constraint Functions!
        public virtual List<NonlinearConstraint> ConstraintDefinitions(ObjectiveFunctionBase inFunction)
        {
            throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
        }

        #region UI Helpers
        public abstract void UpdateBindingValues();
        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }
        public void SetDirty()
        {
            IsDirty = true;
        }
        #endregion
    }
}
