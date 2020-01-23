﻿using AForge;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using CashTerminal.Models;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WebCam
{
    public class AreaRect
    {
        public byte[] Pixels { get; set; }
        public System.Windows.Point AbsolutePos { get; set; }

        public AreaRect(byte[] pixels, System.Windows.Point absolutePos)
        {
            this.Pixels = pixels;
            this.AbsolutePos = absolutePos;
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

        private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();

        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();

        private static AreaRectGroup upArea { get; set; }

        private static DispatcherTimer timer { get; set; }

        private static List<AreaRectGroup> AreaRectTemplates { get; set; } = new List<AreaRectGroup>();

        public static bool IsStarted { get; set; }

        private static Bitmap StoreImage { get; set; }
        private static Bitmap NewImage { get; set; }
        private static bool IsImageRecived { get; set; }
        private static int ElapsedSec { get; set; }

        public static bool IsConfigurationMode;

        private static event NewFrame_Event newFrame;

        public delegate void NewFrame_Event(BitmapImage image);

        private static object CurrentFrame { get; set; }

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


        private static event NewObject_Event newObject;

        public delegate void NewObject_Event(List<ObjectStruct> image);

        public static event NewObject_Event NewObject
        {
            add
            {
                if (newObject == null)
                    newObject += value;
            }
            remove
            {
                newObject -= value;
            }

        }


        private static event NewObjectImage_Event newObjectImage;

        public delegate void NewObjectImage_Event(List<BitmapImage> image);

        public static event NewObjectImage_Event NewObjectImage
        {
            add
            {
                if (newObjectImage == null)
                    newObjectImage += value;
            }
            remove
            {
                newObjectImage -= value;
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

                    //videoCaptureDevice.SetCameraProperty(CameraControlProperty.Exposure, 4, AForge.Video.DirectShow.CameraControlFlags.Manual);
                   // videoCaptureDevice.SetCameraProperty(CameraControlProperty.Focus, 8, AForge.Video.DirectShow.CameraControlFlags.Manual);
                    //videoCaptureDevice.SetCameraProperty(CameraControlProperty.Iris, 16, AForge.Video.DirectShow.CameraControlFlags.Manual);

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
                    if (timer == null)
                    {
                        timer  = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
                        timer.Tick += Timer_Tick;
                        timer.Start();
                    }
                }
                catch
                {
                    IsStarted = false;
                }

            }
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
           
            if (ElapsedSec == 1 && CurrentFrame != null)
            {
                IsImageRecived = false;
           //     Debug.WriteLine(string.Format("--"));
           //     GetDominantColor(CurrentFrame);
            }
            if (ElapsedSec == 2 && CurrentFrame != null)
            {

                //if (CheckEqualsImage(new Bitmap((CurrentFrame as Bitmap), new Size(32, 24)), StoreImage))
                //{
                 Debug.WriteLine(string.Format("--"));
                // Color t = GetBwColor(CurrentFrame as Bitmap);
                // CurrentFrame = ColorBalance(CurrentFrame as Bitmap, t.B, t.G, t.R);

                //Color t = BwArea(CurrentFrame as Bitmap, 0, 0, 5);
                ////Color t = GetBwColor(CurrentFrame as Bitmap);
                //CurrentFrame = ColorBalance(CurrentFrame as Bitmap, t.B, t.G, t.R);

                GetDominantColor(CurrentFrame);
                //}



            }

            ElapsedSec++;
        }

        public static void Stop()
        {

            videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
          //  videoCaptureDevice.SetCameraProperty(CameraControlProperty.Exposure, 0, AForge.Video.DirectShow.CameraControlFlags.Auto);

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


        private static Color GetBwColor(Bitmap source)
        {
            byte R = 0;
            byte G = 0;
            byte B = 0;
            for (int x = 0; x < source.Width; x++)
            {
                for (int y = 0; y < source.Height; y++)
                {
                    Color c1 = source.GetPixel(x, y);
                    if (c1.R > R) R = c1.R;
                    if (c1.G > G) G = c1.G;
                    if (c1.B > B) B = c1.B;
                }
            }

            return Color.FromArgb(255, R, G, B);
        }


        private static void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            //  HistogramEqualization filter = new HistogramEqualization();
            //// ContrastCorrection filter2 = new ContrastCorrection(int.MaxValue);
            //BrightnessCorrection filter2 = new BrightnessCorrection(-50);
            //// process image
           // GammaCorrection filter = new GammaCorrection(0.8);
            // apply the filter
            




            if (countframe >= framerate)
            {
                Bitmap tmp = (Bitmap)eventArgs.Frame;
                //   filter.ApplyInPlace(tmp);

                AreaRect wbArea = GetPixelsFromArea(tmp, 0, 0, 5, 5);
                int tilt = (130 - wbArea.Pixels[0]);
                BrightnessCorrection filter2 = new BrightnessCorrection(tilt);
               // filter2.ApplyInPlace(tmp);

                //   double tilt = (wbArea.Pixels[0] / 100 );

                //Color t = BwArea(tmp, 0, 0, 5);




                //Color t = GetBwColor(tmp);
                //tmp = ColorBalance(tmp, t.B, t.G, t.R);


                AreaRect leftUpArea = GetPixelsFromArea(tmp, 0, 0, 96, 4);
                AreaRect RightUpArea = GetPixelsFromArea(tmp, 1184, 0, 96, 4);


                upArea = new AreaRectGroup(leftUpArea, RightUpArea);
                if (IsConfigurationMode)
                {
                    context.Post(PostImageConfig, MergeImage(GetBitmapFrom(leftUpArea), GetBitmapFrom(RightUpArea)));

                }
                else
                {

                    //  Color t = BwArea(tmp, 0, 0, 10);
                    //  tmp = ColorBalance(tmp, t.B, t.G, t.R);

                    // context.Post(PostImageConfig, new Bitmap(tmp, new Size(640, 480)));

                    if (CheckedArea())
                    {
                        if (!IsImageRecived)
                        {

                            context.Post(PostImage, new Bitmap(tmp, new Size(640, 480)));
                            StoreImage = new Bitmap(tmp, new Size(32, 24));
                            IsImageRecived = true;
                        }
                        else
                        {
                            if (!CheckEqualsImage(new Bitmap(tmp, new Size(32, 24)), StoreImage)) { IsImageRecived = false; ElapsedSec = 0; }
                        }

                    }


                }

                countframe = 0;
            }
            countframe++;
        }

        private static bool CheckEqualsImage(Bitmap source, Bitmap store)
        {
            int width = source.Width - 1;
            int height = source.Height - 1;
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

            return per < 86 ? false : true;
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
                    if (per > 60) { coincidences++; continue; }

                    coincInArea = 0;
                }
            }
            return coincidences > 0 ? true : false;

        }

        public static void PostImageConfig(object o)
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



        public static void PostImage(object o)
        {
            CurrentFrame = o;
            //  var imginput = new Image<Bgr, byte>(o as Bitmap);

            //Image<Gray, Byte> myImage = new Image<Gray, Byte>(o as Bitmap).ThresholdBinary(new Gray(150), new Gray(255));
            //Emgu.CV.Util.VectorOfVectorOfPoint countours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            //Mat hier = new Mat();
            //CvInvoke.FindContours(myImage, countours, hier, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            //CvInvoke.DrawContours(imginput, countours, -1, new MCvScalar(255, 0, 0, 0));



        }

        private static void GetDominantColor(object o)
        {
            System.Drawing.Bitmap image = (System.Drawing.Bitmap)(o as Bitmap).Clone();




           // Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
            Grayscale filter = new Grayscale(0.2126, 0.7152, 0.0722);
            image = filter.Apply(o as Bitmap);
            //Invert filterInvert = new Invert();
            //filterInvert.Apply(image);

            Threshold filterGray = new Threshold(120);
            filterGray.ApplyInPlace(image);

            //System.Drawing.Bitmap image2 = (System.Drawing.Bitmap)(o as Bitmap).Clone();
            //ColorImageQuantizer ciq = new ColorImageQuantizer(new MedianCutQuantizer());
            //// ... or just reduce colors in the specified image
            //image2 = ciq.ReduceColors(image2, 256);



            BlobCounterBase bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinWidth = 80;
            bc.MinHeight = 80;
            bc.MaxHeight = 380;
             bc.ObjectsOrder = ObjectsOrder.Size;
            bc.ProcessImage(image);
            Blob[] blobs = bc.GetObjectsInformation();
            newObjectImage(GetBitmapImagesFromBlobs((o as Bitmap), bc.GetObjectsInformation()));
            newObject(GetObjectListFromBlobs((o as Bitmap), bc.GetObjectsInformation()));

            BitmapImage btm = new BitmapImage();
            using (MemoryStream memStream2 = new MemoryStream())
            {
                (image).Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
                memStream2.Position = 0;
                btm.BeginInit();
                btm.CacheOption = BitmapCacheOption.OnLoad;
                btm.UriSource = null;
                btm.StreamSource = memStream2;
                btm.EndInit();
            }

            newFrame(btm);

        }


        private static List<ObjectStruct> GetObjectListFromBlobs(Bitmap source, Blob[] blobs)
        {

            List<ObjectStruct> ImgList = new List<ObjectStruct>();

            //AreaRect wbArea = GetPixelsFromArea(source, 0, 0, 5, 5);
            //int tilt = (120 - wbArea.Pixels[0]);
            //BrightnessCorrection filter2 = new BrightnessCorrection(tilt);
            //filter2.ApplyInPlace(source);




            Color t = BwArea(source, 0, 0, 5);
            source = ColorBalance(source as Bitmap, t.B, t.G, t.R);



            foreach (Blob blob in blobs)
            {
                ImgList.Add(GetDominantColorFromBlob(source, blob));
            }
            return ImgList;

        }



        private static Bitmap GetBitmapFromBlob(Bitmap source, Blob blob)
        {
            int rX = blob.Rectangle.Width / 2;
            int rY = blob.Rectangle.Height / 2;

            Bitmap result = new Bitmap(blob.Rectangle.Width, blob.Rectangle.Height);
            IColorQuantizer quantizer = new MedianCutQuantizer();

            for (int x = 0; x < blob.Rectangle.Width - 1; x++)
            {

                for (int y = 0; y < blob.Rectangle.Height - 1; y++)
                {
                    Color color = source.GetPixel(blob.Rectangle.Location.X + x, blob.Rectangle.Location.Y + y);

                    if (x > (rX / 2) && x < blob.Rectangle.Width - (rX / 2) && y > (rY / 2) && y < blob.Rectangle.Height - (rY / 2))
                    {
                        quantizer.AddColor(color);
                    }
                    result.SetPixel(x, y, color);

                }

            }

            Color[] palette = quantizer.GetPalette(1);
           
            return result;

        }


        private static ObjectStruct GetDominantColorFromBlob(Bitmap source, Blob blob)
        {

            int rX = blob.Rectangle.Width / 2;
            int rY = blob.Rectangle.Height / 2;
           // int r = blob.Rectangle.Width > 200 ? 40 : 2;
            int r = blob.Rectangle.Width / 5;



            IColorQuantizer quantizer = new MedianCutQuantizer();

            for (int x = 0; x < blob.Rectangle.Width - 1; x++)
            {

                for (int y = 0; y < blob.Rectangle.Height - 1; y++)
                {
                    Color color = source.GetPixel(blob.Rectangle.Location.X + x, blob.Rectangle.Location.Y + y);

                    if (x > rX - r && x < rX + r && y > rY - r && y < rY + r)
                    {
                        quantizer.AddColor(color);
                    }

                }

            }
         //   int paletteLenght = blob.Rectangle.Width > 200 ? 24 : 24;
            int paletteLenght = 24;

            Color[] color1 = quantizer.GetPalette(paletteLenght);
            ObjectStruct obj = new ObjectStruct();
            int lenght = color1.Length;
            obj.Tone = new int[lenght];
            int index = 0;
            foreach (Color color in color1)
            {
                double hue;
                double saturation;
                double value;
                ColorToHSV(color, out hue, out saturation, out value);
                Color alignedСolor = ColorFromHSV(hue, 1, 1);
                obj.Tone[index] = (int)hue;
                //if (blob.Rectangle.Width < 200 && index > 1)
                //{
                //    obj.Tone[index] = 0;
                //}
                //else
                //{
                //    obj.Tone[index] = (int)hue;
                //}
                index++;
            }
            //ColorToHSV(color1, out hue, out saturation, out value);
            //color1 = ColorFromHSV(hue, 1, 1);
            //ColorToHSV(color1, out hue, out saturation, out value);
            //hue = (double)((double)(48f / 100f) * (double)(hue / 3.6f));
            ////    Debug.WriteLine(string.Format("Color: {0}, {1}, {2}", palette[0].R, palette[0].G, palette[0].B));

            return obj;

        }



        private static List<BitmapImage> GetBitmapImagesFromBlobs(Bitmap source, Blob[] blobs)
        {

            List<BitmapImage> ImgList = new List<BitmapImage>();

            foreach (Blob blob in blobs)
            {
                ImgList.Add(GetBitmapImage(GetBitmapFromBlob(source, blob)));
            }
            return ImgList;

        }


        private static BitmapImage GetBitmapImage(Bitmap source)
        {
            BitmapImage btm = new BitmapImage();
            using (MemoryStream memStream2 = new MemoryStream())
            {
                (source).Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
                memStream2.Position = 0;
                btm.BeginInit();
                btm.CacheOption = BitmapCacheOption.OnLoad;
                btm.UriSource = null;
                btm.StreamSource = memStream2;
                btm.EndInit();
            }

            return btm;

        }





        private static Color BwArea(Bitmap source, int x, int y, int size)
        {
            int lenght = size * size;
            int R = 0, G = 0, B = 0;
            for (int y1 = y; y1 < y + size; y1++)
            {

                for (int x1 = x; x1 < x + size; x1++)
                {
                    Color color = source.GetPixel(x1, y1);
                    R += color.R;
                    G += color.G;
                    B += color.B;
                }

            }

            return Color.FromArgb(255, R / lenght, G / lenght, B / lenght); 
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
            return new AreaRect(pixels, new System.Windows.Point(x,y));
        }

        private static Bitmap GetBitmapFrom(AreaRect source)
        {
            int size = (int)Math.Sqrt(source.Pixels.Count());

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



        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }


        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }



        public static Bitmap ColorBalance(this Bitmap sourceBitmap, byte blueLevel,
                                    byte greenLevel, byte redLevel)
        {
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0,
                                        sourceBitmap.Width, sourceBitmap.Height),
                                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            float blue = 0;
            float green = 0;
            float red = 0;


            float blueLevelFloat = blueLevel;
            float greenLevelFloat = greenLevel;
            float redLevelFloat = redLevel;


            for (int k = 0; k + 4 < pixelBuffer.Length; k += 4)
            {
                blue = 255.0f / blueLevelFloat * (float)pixelBuffer[k];
                green = 255.0f / greenLevelFloat * (float)pixelBuffer[k + 1];
                red = 255.0f / redLevelFloat * (float)pixelBuffer[k + 2];

                if (blue > 255) { blue = 255; }
                else if (blue < 0) { blue = 0; }

                if (green > 255) { green = 255; }
                else if (green < 0) { green = 0; }

                if (red > 255) { red = 255; }
                else if (red < 0) { red = 0; }

                pixelBuffer[k] = (byte)blue;
                pixelBuffer[k + 1] = (byte)green;
                pixelBuffer[k + 2] = (byte)red;
            }


            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                        resultBitmap.Width, resultBitmap.Height),
                                       ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }



    }
}
