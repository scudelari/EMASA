using Sap2000Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class TrussSolverBindings : BindableSingleton<TrussSolverBindings>
    {
        private TrussSolverBindings(){}
        public override void SetOrReset()
        {
            StartDisplacement = 5d;
            StartStep = 1d;
            UtilizationRatioTolerance = 1e-2d;
            TargetUtilizationRatio = 1d;
            Report = "";
        }

        private double _StartDisplacement;public double StartDisplacement { get => _StartDisplacement; set => SetProperty(ref _StartDisplacement, value); }

        private double _StartStep;public double StartStep { get => _StartStep; set => SetProperty(ref _StartStep, value); }

        private double _UtilizationRatioTolerance;public double UtilizationRatioTolerance { get => _UtilizationRatioTolerance; set => SetProperty(ref _UtilizationRatioTolerance, value); }

        private double _TargetUtilizationRatio;public double TargetUtilizationRatio { get => _TargetUtilizationRatio; set => SetProperty(ref _TargetUtilizationRatio, value); }

        private string _Report;public string Report { get => _Report; set => SetProperty(ref _Report, value); }
    }
}