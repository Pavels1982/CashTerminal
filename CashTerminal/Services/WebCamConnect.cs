using CashTerminal.Models;
using OpenCvSharp;
using OpenTK.Graphics.OpenGL;
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
        public byte[] Pixels { get; set; }
        public int BoundWidth { get; set; }

        public AreaRect(byte[] pixels,  int width)
        {
            this.Pixels = pixels;
            this.BoundWidth = width;
        }

    }

    public class AreaRectGroup
    {
        public AreaRect LeftAreaRect { get; set; }
        public AreaRect RightAreaRect { get; set; }

        public AreaRectGroup(AreaRect leftArea, AreaRect rightArea)
        {
            this.LeftAreaRect = leftArea;
            this.RightAreaRect = rightArea;
        }
    }



    public static class WebCamConnect
    {
        private static SynchronizationContext context = SynchronizationContext.Current;

       // private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();

        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();

        private static AreaRectGroup upArea { get; set; }

        private static VideoCapture _capture;
        private static  Mat _frame;

        private static List<AreaRectGroup> AreaRectTemplates { get; set; } = new List<AreaRectGroup>();

        public static bool IsStarted { get; set; }

        private static Bitmap StoreImage { get; set; }
        private static Bitmap NewImage { get; set; }
        private static bool IsImageRecived { get; set;}

        private static event NewFrame_Event newFrame;

        public static bool IsConfigurationMode;

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

        public static bool IsWeightMode { get; set; }

        public static void AddTemplates()
        {
            AreaRectTemplates.Add(upArea);
        }

        public static void ClearTemplates()
        {
            AreaRectTemplates.Clear();
        }


 

        //private static WebCamDevice currentDevice;
        //public static WebCamDevice CurrentDevice
        //{
        //    get
        //    {
        //        return currentDevice;
        //    }

        //    private set
        //    {
        //        if (value != null)
        //        {
        //            if (videoCaptureDevice.IsRunning)
        //            {
        //                videoCaptureDevice.Stop();
        //            }

        //            videoCaptureDevice.Source = value.Moniker;
        //            videoCaptureDevice.VideoResolution = videoCaptureDevice.VideoCapabilities[18];
        //        }

        //        currentDevice = value;

        //    }

        //}

        enum type { leftUp, rightUp };
        private static int framerate = 1;
        private static int countframe = 0;
        public static int? Threshold;


        public static List<WebCamDevice> GetDevices()
        {
            //FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //foreach (FilterInfo device in videoDevices)
            //{
            //    deviceList.Add(new WebCamDevice(device.Name, device.MonikerString));
            //}
            return deviceList;
        }

        public static void SetDevice(WebCamDevice device)
        {
            if (device != null)
            {
       
                //   CurrentDevice = device;
            }
        }

        private static Mat GrabFrame()
        {
            Mat image = new Mat();
            _capture.Read(image);
            return image;
        }



        public static void Start()
        {
            if (_capture == null)
            {
                Mat image;
                _capture = new VideoCapture(0);
                _capture.Set(CaptureProperty.FrameWidth, 1280);
                _capture.Set(CaptureProperty.FrameHeight, 960);
                new Thread(() => 
                {

                while (true)
                {
                        image = GrabFrame();

                        VideoCaptureDevice_NewFrame(MatToBitmap(image));
                      //  VideoCaptureDevice_NewFrame(MatToBitmap(image));
                    }
                }).Start();
            }

        }

        public static Bitmap MatToBitmap(Mat image)
        {

            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
        } 



        public static void Stop()
        {

                //videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;

                //try
                //{
                //    videoCaptureDevice.Stop();
                //    IsStarted = false;
                //}
                //catch
                //{
                //IsStarted = false;
                //}
        }



        private static void VideoCaptureDevice_NewFrame(Bitmap frame)
        {

            if (countframe >= framerate)
            {
                Bitmap tmp = frame;

                AreaRect leftUpArea = GetPixelsFromArea(tmp, 0, 0, 96, 4);
                AreaRect RightUpArea = GetPixelsFromArea(tmp, 1184, 0, 96, 4);
                upArea = new AreaRectGroup(leftUpArea, RightUpArea);

                if (IsConfigurationMode)
                {
                    context.Post(PostImage, MergeImage(GetBitmapFrom(leftUpArea), GetBitmapFrom(RightUpArea)));
                }
                else
                {
                    if (CheckedArea())
                    {
                        if (!IsImageRecived)
                        {
                            //PostImage(new Bitmap(tmp, new System.Drawing.Size(640, 480)));
                            context.Post(PostImage, new Bitmap(tmp, new System.Drawing.Size(640, 480)));
                            StoreImage = new Bitmap(tmp, new System.Drawing.Size(32, 24));
                            IsImageRecived = true;
                        }
                        else
                        {
                           if (!CheckEqualsImage(new Bitmap(tmp, new System.Drawing.Size(32, 24)), StoreImage)) IsImageRecived = false;
                        }

                    }


                }

               countframe = 0;
            }
            countframe++;
        }

        private static bool CheckEqualsImage(Bitmap source, Bitmap store)
        {
            int width = source.Width-1;
            int height = source.Height-1;
            int coincidences = 0;
            int grayScaleSource = 0;
            int grayScaleStore = 0;
            int err = 20;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    Color color1 = source.GetPixel(x, y);
                    Color color2 = store.GetPixel(x, y);

                    grayScaleSource = (int)((color1.R + color1.G + color1.B) / 3);
                    grayScaleStore = (int)((color2.R + color2.G + color2.B) / 3);

                    if (grayScaleSource > grayScaleStore - err && grayScaleSource < grayScaleStore + err) coincidences++;


                }
            }
            int per = (int)(((double)coincidences / (width * height)) * 100);

            return per < 90 ? false : true;
        }
     
        private static bool CheckedArea()
        {
            int coincidences = 0;
            int coincInArea = 0;
            int threshold = 100;
            int div = threshold / 2;
            if (AreaRectTemplates.Count > 0)
            {
                int lenghtArea = upArea.LeftAreaRect.Pixels.Length;

                foreach (var area in AreaRectTemplates)
                {

                    for (int index = 0; index < lenghtArea; index++)
                    {
                        if (upArea.LeftAreaRect.Pixels[index] < area.LeftAreaRect.Pixels[index] + div && upArea.LeftAreaRect.Pixels[index] > area.LeftAreaRect.Pixels[index] - div)
                        { if (upArea.RightAreaRect.Pixels[index] < area.RightAreaRect.Pixels[index] + div && upArea.RightAreaRect.Pixels[index] > area.RightAreaRect.Pixels[index] - div) coincInArea++; }

                    }
                    int per = (int)(((double)coincInArea / lenghtArea) * 100);
                    if (per > 80) { coincidences++; continue; }

                    coincInArea = 0;
                }
            }
            return coincidences > 0 ? true : false;

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

        private static AreaRect GetPixelsFromArea(Bitmap source, int x, int y, int size, int scale)
        {
            int areaWidth = (size / scale);
            byte[] pixels = new byte[areaWidth * areaWidth];
            int offSet = size - scale;
            int index = 0;
            int grayScaleSum;

            for ( int x0 = x; x0 < x + size; x0 += scale)
            {
                for (int y0 = y; y0 < y + size; y0 += scale)
                {
                       grayScaleSum = 0;
                     for (int y1 = y0; y1 < y0 + scale; y1++)
                    {
                       
                        for (int x1 = x0; x1 < x0 + scale; x1++)
                        {
                            Color color = source.GetPixel(x1, y1);
                           grayScaleSum += (int)((color.R + color.G + color.B) / 3);
                        }

                    }
                    pixels[index] = (byte)(grayScaleSum / (scale * scale));
                    index++;

                }
            }
            return new AreaRect(pixels, areaWidth);
        }

        private static Bitmap GetBitmapFrom(AreaRect source)
        {
            int size = source.BoundWidth;

            Bitmap tmp = new Bitmap(size, size);
            int index = 0; 

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var val = source.Pixels[index];
                    Color color = Color.FromArgb(255, val, val, val);
                    tmp.SetPixel(x, y, color);
                    index++;
                }
            }

            return tmp;
        }

        private static Bitmap MergeImage(Bitmap img1, Bitmap img2)
        {
            int width = img1.Width + img2.Width;
            int offSet = img1.Width;
            int height = img1.Height >= img2.Height ? img1.Height : img2.Height;

            Bitmap tmp = new Bitmap(width, height);
            for (int x = 0; x < img1.Width-1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    tmp.SetPixel(x, y, img1.GetPixel(x, y));
                    tmp.SetPixel(x + offSet - 1,y, img2.GetPixel(x, y));
                }

            }
            return tmp;

        }


    }
}
