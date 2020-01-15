using AForge.Video.DirectShow;
using CashTerminal.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace WebCam
{
    public class AreaRect
    {
        public byte[,] ByteArray { get; set; }
        public int Weight { get; set; }

        public AreaRect(byte[,] byteArray, int weight)
        {
            this.ByteArray = byteArray;
            this.Weight = weight;
        }

    }


    public static class WebCamConnect
    {
        private static SynchronizationContext context = SynchronizationContext.Current;

        private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();

        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();

        public static bool IsStarted { get; set; }
        private static Bitmap StoreImage { get; set; }

        private static event NewFrame_Event newFrame;

      //  public delegate void NewFrame_Event(BitmapImage image, int weightLeft, int weightRight);
        public delegate void NewFrame_Event(BitmapImage image);

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

        enum type { leftUp, rightUp };
        private static int framerate = 2;
        private static int countframe = 0;
        private static int weightLeft = 0;
        private static int weightRight = 0;

        private static int AreaSize = 100;
        public static int threshold = 20;
        static bool isBinary = true;


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



        private static void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            if (countframe >= framerate)
            {
                Bitmap tmp = (Bitmap)eventArgs.Frame.Clone();

                AreaRect upLeft = GetAreaRect(tmp, 0, 0, 100, 50);
                AreaRect upRight = GetAreaRect(tmp, 1180, 0, 100, 50);

                if (CheckAreaWeight(upLeft, upRight))
                {


                    if (CheckEqualsImageWeight(StoreImage, new Bitmap(tmp, new Size(100, 50))))
                    {
                        StoreImage = new Bitmap(tmp, new Size(100, 50));
                        context.Post(PostImage, tmp);
                    }
                             
                   
                }

                //  context.Post(PostImage, MergeImage(upLeft.ByteArray, upRight.ByteArray));


                countframe = 0;
            }
            countframe++;
        }

        private static bool CheckEqualsImageWeight(Bitmap img1, Bitmap img2)
        {
            bool result = false;
            if (img1 != null)
            {
                int img1Weight = 0;
                int img2Weight = 0;

                for (int x = 0; x < img1.Width; x++)
                {
                    for (int y = 0; y < img1.Height; y++)
                    {
                        img1Weight += img1.GetPixel(x, y).R;
                        img2Weight += img2.GetPixel(x, y).R;
                    }
                }
                Debug.WriteLine(img1Weight + "    " + img2Weight);
                if (img1Weight != img2Weight) result = true;
            }
            else
            {
                result = true;
            }


            return result;
        }
        private static bool CheckAreaWeight(AreaRect area1, AreaRect area2) => (area1.Weight > 20 && area2.Weight > 20) ? true : false;

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

            newFrame(btm);

        }

        private static AreaRect GetAreaRect(Bitmap source, int x, int y, int size, int threshold)
        {
            byte[,] newArea = new byte[size, size];
            int weight = 0;
            int x1 = 0, y1 = 0;

            for (int x0 = x; x0 < x + size; x0++)
            {

                for (int y0 = y; y0 < y+size; y0++)
                {
                    Color color = source.GetPixel(x0, y0);

                    int grayScale = (int)((color.R + color.G + color.B) / 3);
                    int val = 255;
                    if (grayScale < threshold)
                    {
                        val = 0;
                        weight++;
                    }
                    newArea[x1, y1] = isBinary ? (byte)val : (byte)grayScale;
                    y1++;
                }
                y1 = 0;
                x1++;
            }

            weight = weight * 100 / (size * size);

            return new AreaRect(newArea, weight);



        }


     






        private static byte[,] GetBitmapArea(Bitmap source, type type, int size)
        {
            byte[,] newArea = new byte[size, size];



            switch (type)
            {
                case type.leftUp:
                    weightLeft = 0;
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            Color color = source.GetPixel(x, y);

                            int grayScale = (int)((color.R + color.G + color.B) / 3);
                            int val = 255;
                            if (grayScale < threshold)
                            {
                                val = 0;
                                weightLeft++;
                            }
                            newArea[x, y] = isBinary ? (byte)val : (byte)grayScale;
                        }

                    }

                    weightLeft = weightLeft * 100 / (size * size);
                    break;
                case type.rightUp:
                    weightRight = 0;
                    int x2 = 0, y2 = 0;
                    for (int x = source.Width - size; x < source.Width; x++)
                    {
                        
                        for (int y = 0; y < size; y++)
                        {
                            Color color = source.GetPixel(x, y);

                            int grayScale = (int)((color.R + color.G + color.B) / 3);
                            int val = 255;
                            if (grayScale < threshold)
                            {
                                val = 0;
                                weightRight++;
                            }
                            newArea[x2, y2] = isBinary ? (byte)val : (byte)grayScale;


                            y2++;
                        }
                        y2 = 0;
                        x2++;

                    }
                    weightRight =  weightRight * 100 / (size * size);
                    break;
            }
            return newArea;


        }


        private static Bitmap MergeImage(byte[,] img1, byte[,] img2)
        {
    
            int width = img1.GetLength(0) + img2.GetLength(0);
            int height = img1.GetLength(1) + img2.GetLength(1);

            Bitmap tmp = new Bitmap(width, height);

            for (int x = 0; x < img1.GetLength(0); x++)
            {
                for (int y = 0; y < img1.GetLength(1); y++)
                {
                    Color color = Color.FromArgb(255, img1[x, y], img1[x, y], img1[x, y]);

                    tmp.SetPixel(x, y, color);

                }
            }

            for (int x = 0; x < img2.GetLength(0); x++)
            {
                for (int y = 0; y < img2.GetLength(1); y++)
                {

                    Color color = Color.FromArgb(255, img2[x, y], img2[x, y], img2[x, y]);
                    tmp.SetPixel(x+ img1.GetLength(0), y, color);

                }
            }
            return tmp;

        }









    }
}
