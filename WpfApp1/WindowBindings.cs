using BaseWPFLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Others;
using Prism.Commands;
using Prism.Events;

namespace WpfApp1
{
    public class WindowBindings : BindableSingleton<WindowBindings>
    {
        private WindowBindings() { }
        public override void SetOrReset()
        {
            MouseLeftDown = new MouseLeftButtonDown(this);
            TestCommandBehaviourCmd = new TestCommandBehaviourCommand();
            TestCommandBehaviourCmd.ValidState = true;
            NumberValue = 10;
        }

        private int _numberValue;
        public int NumberValue
        {
            get => _numberValue;
            set => SetProperty(ref _numberValue, value);
        }
        private int _numberValue2;
        public int NumberValue2
        {
            get => _numberValue2;
            set => SetProperty(ref _numberValue2, value);
        }

        private DelegateCommand _fieldName;
        public DelegateCommand TextButtonCmd =>
            _fieldName ?? (_fieldName = new DelegateCommand(ExecuteTextButtonCmd)).ObservesCanExecute(() => BoundToChildrenNoErrors);
        
        public async void ExecuteTextButtonCmd()
        {
            void lf_Work()
            {
                OnBeginCommand();

                BusyOverlayBindings.I.Title = "This is a Title!";
                BusyOverlayBindings.I.SetDeterminate("This is the message", "Count");

                for (int i = 0; i < 50; i++)
                {
                    BusyOverlayBindings.I.UpdateProgress(i, 50, $"Value: {i}");
                    Thread.Sleep(200);
                }

                OnEndCommand();
            }

            Task task = new Task(lf_Work);
            task.Start();
            await task;
        }


        public MouseLeftButtonDown MouseLeftDown { private set; get; }

        public TestCommandBehaviourCommand TestCommandBehaviourCmd { private set; get; }

        public void TestSingleButton()
        {
            MessageBox.Show($"Called {MethodBase.GetCurrentMethod()}! Selected Value {SelectedString}");
        }
        public void TestingMethod(int a)
        {
            MessageBox.Show($"Called with parameter {a}!");
        }
        public void LeftButtonClick()
        {
            MessageBox.Show($"Called {MethodBase.GetCurrentMethod()}!");
        }

        public void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"{MethodBase.GetCurrentMethod()} Event Called");
        }

        public void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"{MethodBase.GetCurrentMethod()} Event Called");
        }

        public FastObservableCollection<string> StringList { get; set; } = new FastObservableCollection<string>();

        private string _selectedString;
        public string SelectedString
        {
            get => _selectedString;
            set => SetProperty(ref _selectedString, value);
        }

        public void StartCollection(object sender, RoutedEventArgs e)
        {
            StringList.SuspendCollectionChangeNotification();
            
            StringList.Clear();
            StringList.AddItems(new string[] { "1", "2", "3" });

            StringList.NotifyChanges();
        }
        public void IncreaseCollection(object sender, RoutedEventArgs e)
        {
            StringList.Add(DateTime.Now.ToString());
        }
        public void DecreaseCollection(object sender, RoutedEventArgs e)
        {
            StringList.RemoveAt(StringList.Count - 1);
        }
        public void ClearCollection(object sender, RoutedEventArgs e)
        {
            StringList.Clear();
        }
    }

    public class TextButtonCommand : UpdateableCommand
    {
        private WindowBindings data = null;
        public TextButtonCommand(WindowBindings inData) 
        {
            data = inData;
        }

        private bool _status = false;
        public override bool CanExecute(object parameter)
        {
            _status = !_status;
            return _status;
        }

        public override void Execute(object parameter)
        {
            MessageBox.Show("Bla!");
        }
    }
    public class MouseLeftButtonDown : UpdateableCommand
    {
        private WindowBindings data = null;
        public MouseLeftButtonDown(WindowBindings inData)
        {
            data = inData;
        }

        private bool _status = false;
        public override bool CanExecute(object parameter)
        {
            _status = !_status;
            return _status;
        }

        public override void Execute(object parameter)
        {
            MessageBox.Show("Ble!");
        }
    }

    public class TestCommandBehaviourCommand : UpdateableCommand
    {
        public override void Execute(object parameter)
        {
            MessageBox.Show("Called the function");
        }
    }
}
