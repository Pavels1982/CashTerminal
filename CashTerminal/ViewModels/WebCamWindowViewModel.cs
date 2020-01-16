﻿using CashTerminal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebCam;

namespace CashTerminal.ViewModels
{
    public class WebCamWindowViewModel:INotifyPropertyChanged
    {
        public ObservableCollection<WebCamDevice> ListOfWebCamDevice { get; set; } = new ObservableCollection<WebCamDevice>();

        public int WeightLeft { get; set; }
        public int WeightRight { get; set; }

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



        private bool isThreshold;
        public bool IsThreshold
        {
            get => isThreshold;

            set
            {
                isThreshold = value;
                if (!value) WebCamConnect.Threshold = null;

            }
        }

        private int? threshold;
        public int? Threshold
        {
            get => threshold;
            set
            {
                    threshold = value;
                    if (isThreshold) WebCamConnect.Threshold = value;
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
                    WebCamConnect.Start();
                });
            }
        }

        public ICommand StopStreamCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    WebCamConnect.Stop();
                });
            }
        }



        public WebCamWindowViewModel()
        {
            WebCamConnect.GetDevices().ToList().ForEach(item => ListOfWebCamDevice.Add(item));
            WebCamConnect.NewFrame += WebCamConnect_NewFrame;
        }




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