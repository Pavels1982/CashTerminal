using CashTerminal.Models;
using CashTerminal.ViewModels;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebCam;

namespace CashTerminal
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new MainWindowViewModel();
            InitializeComponent();
     
        }

        //private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        //{
        //    int num = 1;
        //    DataRow.SelectedItems.Cast<Dish>().ToList().ForEach(o => {  o.Index = num; num++; });
        //    //e.Row.Header = e.Row.GetIndex() + 1;
        //}



        private void DataGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            WebCamConnect.StopDevice();
        }
    }
}
