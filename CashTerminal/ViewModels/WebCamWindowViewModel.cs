using AForge.Imaging.ColorReduction;
using CashTerminal.Models;
using CashTerminal.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebCam;


namespace CashTerminal.ViewModels
{
    public class WebCamWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<WebCamDevice> ListOfWebCamDevice { get; set; } = new ObservableCollection<WebCamDevice>();

        public ObservableCollection<BitmapImage> ObjectList { get; set; } = new ObservableCollection<BitmapImage>();


        private bool isWebCamStreaming;
        public bool IsWebCamStreaming
        {
            get
            {
                return isWebCamStreaming;
            }

            set
            {
                isWebCamStreaming = value;
            }

        }


        private bool isConfigurationMode;
        public bool IsConfigurationMode
        {
            get
            {
                return isConfigurationMode;
            }
            set
            {
                isConfigurationMode = value;
                WebCamConnect.IsConfigurationMode = value;
            }
        }

       
    
        private WebCamDevice selectedWebCamDevice;
        public WebCamDevice SelectedWebCamDevice
        {
            get
            {
                return selectedWebCamDevice;
            }

            set
            {
                if (value != null)
                {
                    selectedWebCamDevice = value;
                    WebCamConnect.SetDevice(value);
                }
            }

        }

        public BitmapImage Image { get; set; }

        public ICommand SaveCornersCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    WebCamConnect.SaveCorners();
                });
            }
        }

        public ICommand ClearCornersCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    WebCamConnect.ClearCorners();
                });
            }
        }

        public ICommand AddTemplates
        {
            get
            {
                return new RelayCommand((o) => 
                {
                    WebCamConnect.AddTemplates();
                });
            }
        }


        public ICommand StartStreamCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    IsWebCamStreaming = WebCamConnect.StartDevice(); ;
                });
            }
        }

        public ICommand StopStreamCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    WebCamConnect.StopDevice();
                    IsWebCamStreaming = false;
                });
            }
        }

        public ICommand ClearDataBaseCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    WebCamConnect.ClearDataBase();
                });
            }
        }

    

        public WebCamWindowViewModel()
        {
            WebCamConnect.GetDevices().ToList().ForEach(item => ListOfWebCamDevice.Add(item));
            WebCamConnect.NewFrame += WebCamConnect_NewFrame;
            WebCamConnect.NewObjectImage += WebCamConnect_NewObjectImage;
            //WebCamConnect.NewObject += WebCamConnect_NewObject;
        }

        private void WebCamConnect_NewObjectImage(List<BitmapImage> image)
        {
            ObjectList.Clear();
            foreach (var img in image)
            {
                ObjectList.Add(img);
            }
        }

        //private void WebCamConnect_NewObject(List<BitmapImage> image)
        //{
        //    ObjectList.Clear();
        //    foreach (var img in image)
        //    {
        //        ObjectList.Add(img);
        //    }
        //}




        //private void WebCamConnect_NewFrame(BitmapImage image, int weightLeft, int weightRight)
        //{
        //    this.Image = image;
        //    this.WeightLeft = weightLeft;
        //    this.WeightRight = weightRight;
        //}
        private void WebCamConnect_NewFrame(BitmapImage image)
        {
            this.Image = image;

        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
