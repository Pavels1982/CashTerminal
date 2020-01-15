using AForge.Video.DirectShow;
using CashTerminal.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace WebCam
{

    public static class WebCamConnect
    {
        private static SynchronizationContext context = SynchronizationContext.Current;

        private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();

        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();

        public static bool IsStarted { get; set; }

        private static event NewFrame_Event newFrame;

        public delegate void NewFrame_Event(BitmapImage image, int weightLeft, int weightRight);

        public static event NewFrame_Event NewFrame
        {
            add
            {
                if (newFrame == null)
                    newFrame += value;
            }
            remove
            {
                newFrame -= value;
            }

        }


        private static WebCamDevice currentDevice;
        public static WebCamDevice CurrentDevice
        {
            get
            {
                return currentDevice;
            }

            private set
            {
                if (value != null)
                {
                    if (videoCaptureDevice.IsRunning)
                    {
                        videoCaptureDevice.Stop();
                    }

                    videoCaptureDevice.Source = value.Moniker;
                    videoCaptureDevice.VideoResolution = videoCaptureDevice.VideoCapabilities[18];
          
                   //  videoCaptureDevice.DesiredFrameSize = new Size(320,240);

                }

                currentDevice = value;
               
            }
        
        }

        public static List<WebCamDevice> GetDevices()
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo device in videoDevices)
            {
                deviceList.Add(new WebCamDevice(device.Name, device.MonikerString));
            }
            return deviceList;
        }

        public static void SetDevice(WebCamDevice device)
        {
            if (device != null)
            {
                CurrentDevice = device;
            }
        }

        public static void Start()
        {
            if (videoCaptureDevice.SourceObject == null && !IsStarted)
            {
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                try
                {
                    videoCaptureDevice.Start();
                    IsStarted = true;
                }
                catch
                {
                    IsStarted = false;
                }

            }
        }

        public static void Stop()
        {

                videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;

                try
                {
                    videoCaptureDevice.Stop();
                    IsStarted = false;
                }
                catch
                {
                IsStarted = false;
                }
        }


        private static int framerate = 1;
        private static int countframe = 0;
        private static int weightLeft = 0;
        private static int weightRight = 0;

        private static void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            if (countframe >= framerate)
            {
                Bitmap tmp = (Bitmap)eventArgs.Frame.Clone();



                context.Post(PostImage, MergeImage(GetBitmapArea(tmp, type.leftUp, 100), GetBitmapArea(tmp, type.rightUp, 100)) );

                countframe = 0;
            }
            countframe++;
        }



        public static void PostImage(object o)
        {

            BitmapImage btm = new BitmapImage();
            using (MemoryStream memStream2 = new MemoryStream())
            {
                (o as Bitmap).Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
                memStream2.Position = 0;
                btm.BeginInit();
                btm.CacheOption = BitmapCacheOption.OnLoad;
                btm.UriSource = null;
                btm.StreamSource = memStream2;
                btm.EndInit();
            }

     
            newFrame(btm, weightLeft, weightRight);
            weightLeft = 0;
            weightRight = 0;
        }

        enum type { leftUp, rightUp };


        private static Bitmap GetBitmapArea(Bitmap source, type type, int size)
        {
            Bitmap newArea = new Bitmap(size, size);
            switch (type)
            {
                case type.leftUp:
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            Color color = source.GetPixel(x, y);
                            newArea.SetPixel(x, y, color);
                        }

                    }

                    break;
                case type.rightUp:
                    int x2 = 0, y2 = 0;
                    for (int x = source.Width - size; x < source.Width; x++)
                    {
                        
                        for (int y = 0; y < size; y++)
                        {
                            Color color = source.GetPixel(x, y);
                            newArea.SetPixel(x2,y2, color);
                            y2++;
                        }
                        y2 = 0;
                        x2++;

                    }

                    break;
            }
            return newArea;


        }

        public static int step = 50;

        private static Bitmap MergeImage(Bitmap img1, Bitmap img2)
        {
            int width = img1.Width + img2.Width;
            int height = img1.Height + img2.Height;

            Bitmap tmp = new Bitmap(width, height);

            for (int x = 0; x < img1.Width; x++)
            {
                for (int y = 0; y < img1.Height; y++)
                {
                    Color color1 = img1.GetPixel(x, y);
                    int grayScale = (int)((color1.R + color1.G + color1.B) / 3);
                    int val = 255;
                    if (grayScale < step)
                    {
                        val = 0;
                        weightLeft++;
                    }

                    Color nc = Color.FromArgb(color1.A, val, val, val);

                    tmp.SetPixel(x, y, nc);

                }
            }

           weightLeft = 100 - ((img1.Width * img1.Height) - weightLeft) / 100;
            for (int x = 0; x < img2.Width; x++)
            {
                for (int y = 0; y < img2.Height; y++)
                {
                    Color color1 = img2.GetPixel(x, y);
                    int grayScale = (int)((color1.R + color1.G + color1.B) / 3);
                    int val =255;
                    if (grayScale < step)
                    {
                        weightRight++;
                        val = 0;
                    }

                    Color nc = Color.FromArgb(color1.A, val, val, val);

                    tmp.SetPixel(x+ img1.Width, y, nc);

                }
            }
           weightRight = 100 - ((img2.Width * img2.Height) -  weightRight) /100;
            return tmp;

        }









    }
}
