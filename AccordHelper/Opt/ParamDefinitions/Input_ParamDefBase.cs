extern alias r3dm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Accord;
using BaseWPFLibrary;
using BaseWPFLibrary.Others;
using Prism.Commands;
using r3dm::Rhino.Geometry;

namespace AccordHelper.Opt.ParamDefinitions
{
    public abstract class Input_ParamDefBase : ParamDefBase
    {
        protected Input_ParamDefBase(string inName, ValueRangeBase inValueRange) : base(inName)
        {
            SearchRange = inValueRange;
        }

        protected object _start = null;
        public virtual object Start
        {
            get => throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
            set => throw new InvalidOperationException($"Type {GetType()} does not implement {MethodBase.GetCurrentMethod()}");
        }
        public string StartDisplayStr => Start != null ? $"{Start}" : "";

        protected ValueRangeBase _searchRange;
        public virtual ValueRangeBase SearchRange
        {
            get => _searchRange;
            set
            {
                _searchRange = value ?? throw new Exception("Input values require a search range.");
            }
        }

        public static double[] GetLowerBounds(FastObservableCollection<Input_ParamDefBase> inInputParams)
        {
            List<double> toRet = new List<double>();

            foreach (Input_ParamDefBase inputDef in inInputParams)
            {
                switch (inputDef)
                {
                    case Integer_Input_ParamDef intParam:
                        toRet.Add((double)intParam.SearchRangeTyped.Range.Min);
                        break;

                    case Double_Input_ParamDef dblParam:
                        toRet.Add(dblParam.SearchRangeTyped.Range.Min);
                        break;

                    case Point_Input_ParamDef pntParam:
                        toRet.Add(pntParam.SearchRangeTyped.RangeX.Min);
                        toRet.Add(pntParam.SearchRangeTyped.RangeY.Min);
                        toRet.Add(pntParam.SearchRangeTyped.RangeZ.Min);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return toRet.ToArray();
        }
        public static double[] GetUpperBounds(FastObservableCollection<Input_ParamDefBase> inInputParams)
        {
            List<double> toRet = new List<double>();

            foreach (Input_ParamDefBase inputDef in inInputParams)
            {
                switch (inputDef)
                {
                    case Integer_Input_ParamDef intParam:
                        toRet.Add((double)intParam.SearchRangeTyped.Range.Max);
                        break;

                    case Double_Input_ParamDef dblParam:
                        toRet.Add(dblParam.SearchRangeTyped.Range.Max);
                        break;

                    case Point_Input_ParamDef pntParam:
                        toRet.Add(pntParam.SearchRangeTyped.RangeX.Max);
                        toRet.Add(pntParam.SearchRangeTyped.RangeY.Max);
                        toRet.Add(pntParam.SearchRangeTyped.RangeZ.Max);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return toRet.ToArray();
        }
        public static double[] GetInitialPositionAtCenter(FastObservableCollection<Input_ParamDefBase> inInputParams)
        {
            double[] lowers = GetLowerBounds(inInputParams);
            double[] uppers = GetUpperBounds(inInputParams);

            if (lowers.Length != uppers.Length) throw new Exception("Count of lower bounds is different than count of upper bounds.");

            double[] average = new double[lowers.Length];

            for (int i = 0; i < lowers.Length; i++)
            {
                average[i] = (lowers[i] + uppers[i]) / 2d;
            }

            return average;
        }

        public bool IsInside(object inValue)
        {
            switch (this)
            {
                case Integer_Input_ParamDef intParam:
                    return intParam.SearchRangeTyped.IsInside((int) inValue);

                case Double_Input_ParamDef dblParam:
                    return dblParam.SearchRangeTyped.IsInside((double)inValue);

                case Point_Input_ParamDef pntParam:
                    return pntParam.SearchRangeTyped.IsInside((Point3d) inValue);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int IndexInDoubleArray = -1;

        #region UI Helpers

        public abstract void UpdateInputParameter(string inStartString, string inMinBoundString, string inMaxBoundString);
        public override void UpdateBindingValues()
        {
            RaisePropertyChanged("StartDisplayStr");
            RaisePropertyChanged("SearchRange");
            IsDirty = false;
        }
        #endregion
    }
}
