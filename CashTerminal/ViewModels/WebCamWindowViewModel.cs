using CashTerminal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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

        public ObservableCollection<BitmapImage> ObjectList { get; set; } = new ObservableCollection<BitmapImage>();
        public List<Color> ObjectColorList { get; set; } = new List<Color>();

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

        private bool isWeightMode;
        public bool IsWeightMode
        {
            get => isWeightMode;

            set
            {
                isWeightMode = value;

                WebCamConnect.IsWeightMode = value;

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




        private bool CheckColor(Color color1, Color color2)
        {
            int err = 51;
            if (color1.R > color2.R - err && color1.R < color2.R + err)
                if (color1.G > color2.G - err && color1.R < color2.G + err)
                    if (color1.B > color2.B - err && color1.B < color2.B + err) return true;
            return false;
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
