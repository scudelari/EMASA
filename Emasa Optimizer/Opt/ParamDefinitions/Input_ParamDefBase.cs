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
using BaseWPFLibrary;
using BaseWPFLibrary.Others;
using Emasa_Optimizer.WpfResources;
using Prism.Commands;
using r3dm::Rhino.Geometry;

namespace Emasa_Optimizer.Opt.ParamDefinitions
{
    public abstract class Input_ParamDefBase : ParamDefBase
    {
        protected Input_ParamDefBase(string inName) : base(inName)
        {
            StartPositionType = StartPositionTypeEnum.Random;
        }

        public static double[] GetLowerBounds(FastObservableCollection<Input_ParamDefBase> inInputParams)
        {
            List<double> toRet = new List<double>();

            foreach (Input_ParamDefBase inputDef in inInputParams)
            {
                switch (inputDef)
                {
                    case Double_Input_ParamDef dblParam:
                        toRet.Add(dblParam.SearchRange.Range.Min);
                        break;

                    case Point_Input_ParamDef pntParam:
                        toRet.Add(pntParam.SearchRange.RangeX.Min);
                        toRet.Add(pntParam.SearchRange.RangeY.Min);
                        toRet.Add(pntParam.SearchRange.RangeZ.Min);
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
                    case Double_Input_ParamDef dblParam:
                        toRet.Add(dblParam.SearchRange.Range.Max);
                        break;

                    case Point_Input_ParamDef pntParam:
                        toRet.Add(pntParam.SearchRange.RangeX.Max);
                        toRet.Add(pntParam.SearchRange.RangeY.Max);
                        toRet.Add(pntParam.SearchRange.RangeZ.Max);
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

        public Dictionary<StartPositionTypeEnum, string> StartPositionTypeEnumStaticDescriptions => ListDescSH.I.StartPositionTypeEnumStaticDescriptions;
        private StartPositionTypeEnum _startPositionType;
        public StartPositionTypeEnum StartPositionType
        {
            get => _startPositionType;
            set
            {
                SetProperty(ref _startPositionType, value);

                switch (value)
                {
                    case StartPositionTypeEnum.Given:
                        StartValue_TextBoxVisibility = Visibility.Visible;
                        StartPositionPercent_TextBoxVisibility = Visibility.Collapsed;
                        AreBothHidden = false;
                        break;

                    case StartPositionTypeEnum.CenterOfRange:
                        StartValue_TextBoxVisibility = Visibility.Collapsed;
                        StartPositionPercent_TextBoxVisibility = Visibility.Collapsed;
                        AreBothHidden = true;
                        break;

                    case StartPositionTypeEnum.Random:
                        StartValue_TextBoxVisibility = Visibility.Collapsed;
                        StartPositionPercent_TextBoxVisibility = Visibility.Collapsed;
                        AreBothHidden = true;
                        break;

                    case StartPositionTypeEnum.PercentRandomFromCenter:
                        StartValue_TextBoxVisibility = Visibility.Collapsed;
                        StartPositionPercent_TextBoxVisibility = Visibility.Visible;
                        AreBothHidden = false;
                        break;

                    case StartPositionTypeEnum.PercentRandomFromGiven:
                        StartValue_TextBoxVisibility = Visibility.Visible;
                        StartPositionPercent_TextBoxVisibility = Visibility.Visible;
                        AreBothHidden = false;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
        private double _startPositionPercent = 0.1d;
        public double StartPositionPercent
        {
            get => _startPositionPercent;
            set
            {
                if (value > 1d || value <= .05d) throw new Exception("Value must be within 5% and 100%");
                SetProperty(ref _startPositionPercent, value);
            }
        }

        public bool IsInside(object inValue)
        {
            switch (this)
            {
                case Double_Input_ParamDef dblParam:
                    return dblParam.SearchRange.IsInside((double)inValue);

                case Point_Input_ParamDef pntParam:
                    return pntParam.SearchRange.IsInside((Point3d) inValue);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int IndexInDoubleArray = -1;

        #region UI Helpers
        private Visibility _startPositionPercent_TextBoxVisibility;
        public Visibility StartPositionPercent_TextBoxVisibility
        {
            get => _startPositionPercent_TextBoxVisibility;
            set => SetProperty(ref _startPositionPercent_TextBoxVisibility, value);
        }
        private Visibility _startValue_TextBoxVisibility;
        public Visibility StartValue_TextBoxVisibility
        {
            get => _startValue_TextBoxVisibility;
            set => SetProperty(ref _startValue_TextBoxVisibility, value);
        }
        private bool _areBothHidden;
        public bool AreBothHidden
        {
            get => _areBothHidden;
            set => SetProperty(ref _areBothHidden, value);
        }


        #endregion
    }
}
