using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BaseWPFLibrary.Forms;

namespace Las_to_SqLite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Sets the window's data context
                this.DataContext = AppSS.I;
            }
            catch (Exception e)
            {
                ExceptionViewer ev = ExceptionViewer.Show(e);
                Application.Current.Shutdown(1);
            }
        }
    }
}
