using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using EmasaSapTools.Monitors;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Others;
using Sap2000Library;
using Sap2000Library.Other;

namespace EmasaSapTools.Bindings
{
    public class StatusBarBindings : BindableSingleton<StatusBarBindings>
    {
        private StatusBarBindings()
        {
        }

        public override void SetOrReset()
        {
        }

        private string _sapFileName;
        public string SapFileName
        {
            get => _sapFileName;
            set => SetProperty(ref _sapFileName, value);
        }

        private bool _sap2000IsOpen;
        public bool Sap2000IsOpen
        {
            get => _sap2000IsOpen;
            set
            {
                SetProperty(ref _sap2000IsOpen, value);
            }
        }


        public FastObservableCollection<IMonitorInterfaceItems> MonitorList { get; set; } = new FastObservableCollection<IMonitorInterfaceItems>();
    }
}