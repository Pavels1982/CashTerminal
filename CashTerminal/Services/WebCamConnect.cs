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
        public byte[,] ByteArrayLeft { get; set; }
        public byte[,] ByteArrayRight { get; set; }
        public byte[] HistogramLeft { get; set; }
        public byte[] HistogramRight { get; set; }
        public int WeightLeft { get; set; }
        public int WeightRight { get; set; }

        public AreaRect(byte[,] byteArrayLeft, byte[,] byteArrayRight)
        {
            this.ByteArrayLeft = byteArrayLeft;
            this.ByteArrayRight = byteArrayRight;
            int wl = 0;
            this.HistogramLeft = GetHistogram(this.ByteArrayLeft, ref wl);
            WeightLeft = wl;
            wl = 0;
            this.HistogramRight = GetHistogram(this.ByteArrayRight, ref wl);
            WeightRight = wl;
        }

        public byte[] GetHistogram(byte[,] ByteArray, ref int weight, int size = 12)
        {
            int offset = size / 2;
            byte[] Histogram = new byte[ByteArray.Length / size];
            int avrBrightnes = 0;
            int index = 0;
            for (int x = 0; x < ByteArray.GetLength(0) - offset; x += offset)
            {
                for (int y = 0; y < ByteArray.GetLength(1) - offset; y += offset)
                {
                    for (int x1 = x; x1 < x + offset; x1++)
                    {
                        for (int y1 = y; y1 < y + offset; y1++)
                        {
                            avrBrightnes += ByteArray[x1, y1];
                        }

                    }
                    Histogram[index] = (byte)(avrBrightnes / size);
                    weight += avrBrightnes;
                    index++;
                    avrBrightnes = 0;
                }

            }
            weight /= ByteArray.Length;
            return Histogram;

            //for (int x = x0; x < x0 + offset; x++)
            //{

            //    for (int y = y0; y < y0 + offset; y++)
            //    {


            //    }

            //}




            //Histogram[i] = ( ByteArray[x, y] + ByteArray[x + 1, y] + ByteArray[x, y + 1] + ByteArray[x + 1, y + 1]) / 4;

            //for (int x = 0; x < ByteArray.GetLength(0) - offset; x = x + offset)
            //{

            //    for (int y = 0; y < ByteArray.GetLength(1) - offset; y = y + offset)
            //    {


            //    }

            //}
        }

    }


    public static class WebCamConnect
    {
        private static SynchronizationContext context = SynchronizationContext.Current;

        private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();

        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();

        private  static AreaRect upArea {  get;  set; }
        private static AreaRect upRight;

        private static List<AreaRect> AreaRectTemplates { get; set; } = new List<AreaRect>();

        public static bool IsStarted { get; set; }
        private static Bitmap StoreImage { get; set; }

        private static event NewFrame_Event newFrame;

        public static bool IsConfigurationMode;

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

        public static void AddTemplates()
        {
            AreaRectTemplates.Add(upArea);
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
        private static int framerate = 1;
        private static int countframe = 0;
        public static int? Threshold;


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
                Bitmap tmp = (Bitmap)eventArgs.Frame;
                upArea = new AreaRect(GetAreaRect(tmp, 0, 0, 96, Threshold), GetAreaRect(tmp, 1184, 0, 96, Threshold));
                if (IsConfigurationMode)
                {
                   

                    context.Post(PostImage, MergeImage(upArea.ByteArrayLeft, upArea.ByteArrayRight));
                }
                else
                {
                    //if (CheckedArea())
                    //{
                    //    StoreImage = new Bitmap(tmp, new Size(320, 240));
                    //    context.Post(PostImage, StoreImage);
                    //}
                    if (CheckeWeight())
                    {
                        StoreImage = new Bitmap(tmp, new Size(320, 240));
                        context.Post(PostImage, StoreImage);
                    }
                }



                //if (CheckAreaWeight(upLeft, upRight))
                //{


                //    if (CheckEqualsImageWeight(StoreImage, new Bitmap(tmp, new Size(100, 50))))
                //    {
                //        StoreImage = new Bitmap(tmp, new Size(100, 50));
                //        context.Post(PostImage, tmp);
                //    }


                //}



                // context.Post(PostImage, BitmapFromHistogram(upLeft.Histogram));


                countframe = 0;
            }
            countframe++;
        }

        private static bool CheckeWeight()
        {
            int coincidences = 0;
            int coincInArea = 0;
            int threshold = 5;

            foreach (var area in AreaRectTemplates)
            {

                if (Math.Abs(upArea.WeightLeft - area.WeightLeft) < threshold)
                    if (Math.Abs(upArea.WeightRight - area.WeightRight) < threshold) coincInArea++;

                if (coincInArea > 0) coincidences++;
                coincInArea = 0;
            }
            return coincidences > 0 ? true : false;
        }

        private static bool CheckedArea()
        {
            int coincidences = 0;
            int coincInArea = 0;
            int threshold = 50;
            int div = threshold / 2;
            if (AreaRectTemplates.Count > 0)
            {
                int lenghtArea = upArea.HistogramLeft.Length;

                foreach (var area in AreaRectTemplates)
                {

                    for (int index = 0; index < lenghtArea; index++)
                    {
                        if (upArea.HistogramLeft[index] < area.HistogramLeft[index] + div && upArea.HistogramLeft[index] > area.HistogramLeft[index] - div)
                        { if (upArea.HistogramRight[index] < area.HistogramRight[index] + div && upArea.HistogramRight[index] > area.HistogramRight[index] - div) coincInArea++; }

                    }
                    int per = (int)(((double)coincInArea / lenghtArea) * 100);
                    if (per > 90) coincidences++;
                    coincInArea = 0;
                }
            }
            return coincidences > 0 ? true : false;

        }



        private static Bitmap BitmapFromHistogram(byte[] histogram)
        {
            Bitmap btm = new Bitmap(histogram.GetLength(0), 255);
            int index = 0;
            for (int x = 0; x < histogram.GetLength(0); x++)
            {

                for (int y = 254; y > 0; y--)
                {
                    Color color = Color.FromArgb(255, 0, 0, 0);
                    if (y < histogram[index])
                    {
                         color = Color.FromArgb(255, 255, 255, 255);
                    }

                    btm.SetPixel(x, y, color);

                }
                index++;
            }

            return btm;


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

        private static byte[,] GetAreaRect(Bitmap source, int x, int y, int size, int? threshold = null)
        {
            byte[,] newArea = new byte[size, size];
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
                    }

                    newArea[x1, y1] = threshold != null ? (byte)val : (byte)grayScale;

                    y1++;
                }
                y1 = 0;
                x1++;
            }

            return newArea;
        }

        private static Bitmap MergeImage(byte[,] img1, byte[,] img2)
        {
    
            int width = img1.GetLength(0) + img2.GetLength(0);
            int height = img1.GetLength(1);

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
