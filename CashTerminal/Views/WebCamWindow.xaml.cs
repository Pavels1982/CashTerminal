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
using System.Windows.Shapes;
using WebCam;

namespace CashTerminal.Views
{
    /// <summary>
    /// Логика взаимодействия для WebCamWindow.xaml
    /// </summary>
    public partial class WebCamWindow : Window
    {
        public WebCamWindow()
        {
            InitializeComponent();
            this.DataContext = new WebCamWindowViewModel();
        }



        private void Window_Closed(object sender, EventArgs e)
        {
            WebCamConnect.Stop();
        }
    }
}
