using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WebCam
{
    /// <summary>
    /// Класс определяющий объект для хранения набора пикселей.
    /// </summary>
    public class AreaRect
    {
        public byte[] Pixels { get; set; }

        public AreaRect(byte[] pixels)
        {
            this.Pixels = pixels;
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

    /// <summary>
    /// Класс определяющий структуру цвета в HSV-формате.
    /// </summary>
    public class HSVColor
    {
        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Value { get; set; }

        public HSVColor(double hue = 0, double saturation = 0, double value = 0)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Value = value;
        }

    }

    /// <summary>
    /// Класс определяющий структуру объекта (блюда)
    /// </summary>
    public class ObjectStruct
    {
        public int Radius { get; set; } = 0;
        public HSVColor[] Tone { get; set; }
        public List<int?> Id { get; set; } = new List<int?>();
    }

    public static class WebCamConnect
    {
        private static int framerate = 1;
        private static int countframe = 0;
        
        /// <summary>
        /// Ссылка на основной поток.
        /// </summary>
        private static SynchronizationContext context = SynchronizationContext.Current;
        private static VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice();
        private static WebCamDevice currentDevice;
        private static List<WebCamDevice> deviceList = new List<WebCamDevice>();
        
        /// <summary>
        /// Изображение текущих калибровочных углов.
        /// </summary>
        private static AreaRectGroup upArea { get; set; }
        private static DispatcherTimer timer { get; set; }
       
        /// <summary>
        /// Get or set коллекция изображений калибровочных углов.
        /// </summary>
        private static List<AreaRectGroup> AreaRectTemplates { get; set; } = new List<AreaRectGroup>();

        /// <summary>
        /// Get or set коллекция объектов хранимых в базе.
        /// </summary>
        private static List<ObjectStruct> ObjectList { get; set; } = new List<ObjectStruct>();

        /// <summary>
        /// Get or set коллекция найденных объектов базы.
        /// </summary>
        private static List<ObjectStruct> FoundObjects { get; set; } = new List<ObjectStruct>();

        /// <summary>
        /// Get or set текущий фрейм.
        /// </summary>
        private static object CurrentFrame { get; set; }

        /// <summary>
        /// Get or set уменьшенное изображение последнего обработанного кадра.
        /// </summary>
        private static Bitmap StoreImage { get; set; }

        /// <summary>
        /// Get or set уменьшенное изображение текущего кадра.
        /// </summary>
        private static Bitmap NewImage { get; set; }


        private static bool IsImageRecived { get; set; }
        private static int ElapsedSec { get; set; }

        public static bool IsConfigurationMode;
        public static bool IsStarted { get; set; }

        private static event NewObject_Event newObject;
        private static event CalibrateFrame_Event calibFrame;
        private static event NewObjectImage_Event newObjectImage;

        public delegate void CalibrateFrame_Event(BitmapImage image);
        public delegate void NewObject_Event(List<int?> id);
        public delegate void NewObjectImage_Event(List<BitmapImage> image);

        public static event CalibrateFrame_Event NewFrame
        {
            add
            {
                if (calibFrame == null)
                    calibFrame += value;
            }
            remove
            {
                calibFrame -= value;
            }

        }

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

        /// <summary>
        /// Добавление в коллекцию текущих калибровочных углов.
        /// </summary>
        public static void AddTemplates()
        {
            AreaRectTemplates.Add(upArea);
        }

        /// <summary>
        /// Очистка коллекции калибровочных углов.
        /// </summary>
        public static void ClearTemplates()
        {
            AreaRectTemplates.Clear();
        }

        /// <summary>
        /// Очистка коллекций базы и найденных объектов.
        /// </summary>
        public static void ClearDataBase()
        {
            ObjectList.Clear();
            FoundObjects.Clear();

        }

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

        public static bool StartDevice()
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
                        AreaRectTemplates = ReadData<List<AreaRectGroup>>(@"corners_data.json") as List<AreaRectGroup>;

                        var result = ReadData<List<ObjectStruct>>(@"objects_data.json") as List<ObjectStruct>;
                        if (result != null) ObjectList = result;
                    }
                }
                catch
                {
                    IsStarted = false;
                }

            }
            return IsStarted;

        }

        public static void StopDevice()
        {

            videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
            try
            {
                videoCaptureDevice.Stop();
                IsStarted = false;
                SaveData(ObjectList, @"objects_data.json");
            }
            catch
            {
                IsStarted = false;
            }
        }

        /// <summary>
        /// Метод определяющий условия обработки текущего фрейма(поиска объектов на подносе).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Timer_Tick(object sender, EventArgs e)
        {
           
            if (ElapsedSec == 1 && CurrentFrame != null)
            {
                IsImageRecived = false;
            }


            if (ElapsedSec == 2 && CurrentFrame != null)
            {
                ProcessFrame(CurrentFrame);
            }

            ElapsedSec++;
        }

        /// <summary>
        /// Метод вызывается каждый раз при получении нового кадра с камеры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private static void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            if (countframe >= framerate)
            {
                Bitmap tmp = (Bitmap)eventArgs.Frame;
                AreaRect leftUpArea = GetPixelsFromArea(tmp, 0, 0, 96, 4,true);
                AreaRect RightUpArea = GetPixelsFromArea(tmp, 1184, 0, 96, 4,true);


                upArea = new AreaRectGroup(leftUpArea, RightUpArea);
                if (IsConfigurationMode)
                {
                    context.Post(PostImageConfig, MergeImage(GetBitmapFromAreaRect(leftUpArea), GetBitmapFromAreaRect(RightUpArea)));

                }
                else
                {

                    //AreaRect wbArea = GetPixelsFromArea(tmp, 0, 0, 5, 5);
                    //int tilt = (100 - wbArea.Pixels[0]);
                    //BrightnessCorrection filter2 = new BrightnessCorrection(tilt);
                    //filter2.ApplyInPlace(tmp);
                    //Color t = BwArea(tmp, 0, 0, 10);
                    //tmp = ColorBalance(tmp, t.B, t.G, t.R);
                    //context.Post(PostImageConfig, new Bitmap(tmp, new Size(640, 480)));

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
                            if (!CheckEqualsImage(new Bitmap(tmp, new Size(32, 24)), StoreImage))
                            {
                                IsImageRecived = false;
                                ElapsedSec = 0;
                            }
                        }

                    }


                }

                countframe = 0;
            }
            countframe++;
        }

        /// <summary>
        /// Метод сравнения двух изображений на предмет различий в значениях пикселей в % соотношении.
        /// </summary>
        /// <param name="current">Текущее изображение</param>
        /// <param name="last">Предыдущее изображение</param>
        /// <returns></returns>
        private static bool CheckEqualsImage(Bitmap current, Bitmap last)
        {
            int width = current.Width - 1;
            int height = current.Height - 1;
            int coincidences = 0;
            int grayScaleSource = 0;
            int grayScaleStore = 0;
            int err = 20;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    Color color1 = current.GetPixel(x, y);
                    Color color2 = last.GetPixel(x, y);

                    grayScaleSource = (int)((color1.R + color1.G + color1.B) / 3);
                    grayScaleStore = (int)((color2.R + color2.G + color2.B) / 3);

                    if (grayScaleSource > grayScaleStore - err && grayScaleSource < grayScaleStore + err) coincidences++;


                }
            }
            int per = (int)(((double)coincidences / (width * height)) * 100);

            return per < 86 ? false : true;
        }

        /// <summary>
        /// Метод проверки текущих калибровочных углов с коллекцией калибровочных углов. Если области совпадают, возвращает true.
        /// </summary>
        /// <returns></returns>
        private static bool CheckedArea()
        {
            int coincidences = 0;
            int coincInArea = 0;
            int threshold = 20;
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

        /// <summary>
        /// Метод передачи изображения калибровочной зоны делегату.
        /// </summary>
        /// <param name="o"></param>
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

            calibFrame(btm);

        }

        /// <summary>
        /// Метод для извлечение объекта из другого потока в основной.
        /// Необходим только в том случае если объект планируется использовать в основном потоке.
        /// В данном случае передается изображение, которое в последующем используется для отображение в UI.
        /// </summary>
        /// <param name="o"></param>
        public static void PostImage(object o)
        {
            CurrentFrame = o;
        }

        /// <summary>
        /// Метод определения объектов на подносе.
        /// Сначала исходное изображение конвертируется в оттенки серого, после чего производится бинаризация для детектирования и сортировки ограничивающих зон объектов. 
        /// Все найденные объекты сохраняются в массив FoundObjects, все элементы которого в свою очередь проходят проверку на идентичность палитр с объектами находящимися в базе.
        /// </summary>
        /// <param name="o"></param>
        private static void ProcessFrame(object o)
        {
            System.Drawing.Bitmap image = (System.Drawing.Bitmap)(o as Bitmap).Clone();

            Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
            image = filter.Apply(o as Bitmap);
            Threshold filterGray = new Threshold(120);
            filterGray.ApplyInPlace(image);

            BlobCounterBase bc = new BlobCounter();
            bc.FilterBlobs = true;
            bc.MinWidth = 90;
            bc.MinHeight = 90;
            bc.MaxHeight = 380;
            bc.ObjectsOrder = ObjectsOrder.Size;
            bc.ProcessImage(image);
            Blob[] blobs = bc.GetObjectsInformation();
            FoundObjects = GetObjectListFromBlobs((o as Bitmap), blobs);
            newObjectImage(GetBitmapImagesFromBlobs((o as Bitmap), blobs));
            CheckForEqualsInDataBase(FoundObjects);
            //newObject(FindedObjects);

            //BitmapImage btm = new BitmapImage();
            //using (MemoryStream memStream2 = new MemoryStream())
            //{
            //    (image).Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
            //    memStream2.Position = 0;
            //    btm.BeginInit();
            //    btm.CacheOption = BitmapCacheOption.OnLoad;
            //    btm.UriSource = null;
            //    btm.StreamSource = memStream2;
            //    btm.EndInit();
            //}

            //newFrame(btm);

        }

        /// <summary>
        /// Сравнение палитр найденных объектов с базой.
        /// </summary>
        /// <param name="foundObjects">Список найденных объектов.</param>
        private static void CheckForEqualsInDataBase(List<ObjectStruct> foundObjects)
        {
            List<int?> resultID = new List<int?>();

            foundObjects.ForEach(newObj =>
            {

                ObjectList.ForEach(baseObj =>
                {
                    if (CheckObjectPalette(baseObj, newObj))
                    {
                        if (baseObj.Id.Count == 1)
                        {
                            resultID.Add(baseObj.Id[0]);
                        }

                    }

                });

            });


            newObject(resultID);
        }

        /// <summary>
        /// Метод возвращает список, который включает в себя id только неподтверждённых блюд.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<int?> GetSortedList(List<int?> id)
        {
            List<int?> sortedID = new List<int?>();
            foreach (int? val in id)
            {
                bool isExist = false;
                ObjectList.ForEach(baseObj =>
                {
                    if (baseObj.Id.Count == 1)
                    {
                        if (baseObj.Id[0] == val)
                            isExist = true;
                    }

                });

                if (!isExist) sortedID.Add(val);
            }
            ///Здесь должна быть проверка на наличие в отсортированном списке id-блюд, которые были занесены ранее как неподтверждённые. 
            ///По-идеи  так как они ещё неподтвержденные, но уже известно что у них иная палитра то по сути они относятся к иному объекту, которые уже можно не сравнивать с новыми.
            ///Метод работает при определённых условиях, возможно нужен более глубокий анализ с применением функции сравнения палитры. 
            ///Данный метод даст точное определение одного нового объекта на фоне остальных неизвестных. 
            //ObjectList.ForEach(ob =>
            //{
            //    if (ob.Id.Count > 1)
            //    {
            //        ob.Id.ForEach(idbase =>
            //        {
            //            if (sortedID.Any(i => i == idbase) ) sortedID.Remove(idbase);
            //        });

            //    }
            //});


            return sortedID;
        }

        /// <summary>
        /// Метод сравнения списка id-блюд полученного из терминала с базой.
        /// </summary>
        /// <param name="id">Список id-блюд, переданный из терминала.</param>
        public static void CheckId(List<int?> id)
        {

            List<int?> sortedID = GetSortedList(id);
          
            FoundObjects.ForEach(newObj =>
                {
                    newObj.Id.AddRange(sortedID);
                    bool ObjExist = false;
                    ObjectList.ForEach(baseObj =>
                    {

                        //Сравнение палитры объектов
                        if (CheckObjectPalette(baseObj, newObj) && !ObjExist)
                        {
                            ObjExist = true;
                            
                            if (baseObj.Id.Count > 1)
                            {
                                var b = baseObj.Id.Intersect(sortedID);
                                baseObj.Id = new List<int?>();
                                b.ToList().ForEach(el => baseObj.Id.Add(el));
                            }

                        }

                    });

                      if (!ObjExist && newObj.Id.Count > 0) ObjectList.Add(newObj);

                });
            SortedBase();

        }

        /// <summary>
        /// Метод сортировки базы данных объектов. 
        /// Включает итеративный блок while, который работает до тех пор пока база содержит в списках (неподтверждённых объектов) id-блюд, которые уже подтвердились.
        /// </summary>
        private static void SortedBase()
        {
            int consist = 0;
            bool count = true;

            while (count)
            {
                ObjectList.Where(o => o.Id.Count == 1).ToList().ForEach(el =>
                {
                    ObjectList.ForEach(ob => 
                    {
                        if (ob != el && el.Id != null)
                        {
                            ///Костыль! Здесь блок try так как при определённых условиях коллекция el.Id == null. Нужно дебажить.
                            try
                            {
                                if (ob.Id.Remove(el.Id[0])) consist++;
                            } catch
                            { }
                        }
                    });
                });

                count = consist >= 1 ? true : false;
                consist = consist > 0 ? consist -= 1: 0;
            }
        }

        /// <summary>
        /// Метод сравниение двух объектов ObjectStruct между собой на совпадение палитры.
        /// </summary>
        /// <param name="based">Объект хранимый в базе.</param>
        /// <param name="current">Найденный объект.</param>
        /// <returns></returns>        
        private static bool CheckObjectPalette(ObjectStruct based, ObjectStruct current)
        {
            if (current.Tone != null && based.Tone != null)
            {
                int err = 9;
                int index = 0;
                int considence = 0;
                foreach (var tone in current.Tone)
                {
                    if (based.Tone.Length == current.Tone.Length)
                    {
                        if (tone.Hue >= based.Tone[index].Hue - err && tone.Hue <= based.Tone[index].Hue + err)//8
                            if (tone.Saturation >= based.Tone[index].Saturation - 0.3f && tone.Saturation <= based.Tone[index].Saturation + 0.3f)
                                considence++;
                        index++;
                    }
                }
                int per = (int)(((double)considence / based.Tone.Length) * 100);
                if (per >= 65)
                    if (current.Radius > based.Radius - 10 && current.Radius < based.Radius + 10) return true;
            }
            return false;
        }

        /// <summary>
        /// Метод возвращает коллецию ObjectStruct на основе передаваемых параметров. 
        /// </summary>
        /// <param name="source">Источник.</param>
        /// <param name="blobs">Коллекция Blob.</param>
        /// <returns></returns>
        private static List<ObjectStruct> GetObjectListFromBlobs(Bitmap source, Blob[] blobs)
        {
            List<ObjectStruct> ImgList = new List<ObjectStruct>();

            Color t = BwArea(source, 0, 0, 10);
            source = ColorBalance(source as Bitmap, t.B, t.G, t.R);

            foreach (Blob blob in blobs)
            {
                ImgList.Add(GetObjectStructFromBlob(source, blob));
            }
            return ImgList;

        }

        /// <summary>
        /// Возвращает изображение формата Bitmap из зоны ограниченной квадратом структуры Blob.
        /// Нужено для отладки.
        /// </summary>
        /// <param name="source">Источник.</param>
        /// <param name="blob">Блоб структура.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Возвращает объект типа ObjectStruct на основе передаваемых параметров включающих источник изображения и Blob-структуру.
        /// Производит вычисление палитры изображения методом квантизации(уменьшение цветов), и запись полученых значений в структуру ObjectStruct.
        /// </summary>
        /// <param name="source">Источник изображения.</param>
        /// <param name="blob">Блоб структура</param>
        /// <returns></returns>
        private static ObjectStruct GetObjectStructFromBlob(Bitmap source, Blob blob)
        {

            int rX = blob.Rectangle.Width / 2;
            int rY = blob.Rectangle.Height / 2;
            int r = blob.Rectangle.Width / 4;

            IColorQuantizer quantizer = new MedianCutQuantizer();

            for (int x = 0; x < blob.Rectangle.Width - 1; x++)
            {

                for (int y = 0; y < blob.Rectangle.Height - 1; y++)
                {
                    Color color = source.GetPixel(blob.Rectangle.Location.X + x, blob.Rectangle.Location.Y + y);

                    if (x > rX - r && x < rX + r && y > rY - r && y < rY + r)
                    {
                        double hue;
                        double saturation;
                        double value;
                        ColorToHSV(color, out hue, out saturation, out value);
                        Color alignedСolor = ColorFromHSV(hue, saturation, value);

                        quantizer.AddColor(alignedСolor);
                    }

                }

            }
            int paletteLenght = 256;

            Color[] color1 = quantizer.GetPalette(paletteLenght);

            ObjectStruct obj = new ObjectStruct();
            int lenght = paletteLenght;
            obj.Tone = new HSVColor[lenght];
            int index = 0;
            foreach (Color color in color1)
            {
                double hue;
                double saturation;
                double value;
                ColorToHSV(color, out hue, out saturation, out value);
                obj.Tone[index] = new HSVColor(hue, saturation,value);
                index++;
            }
            obj.Radius = rX;
            return obj;

        }

        /// <summary>
        /// Метод возвращает коллекцию изображений ограниченных зонами квадратов, передаваемых структурой Blob. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="blobs"></param>
        /// <returns>Коллекция изображений.</returns>
        private static List<BitmapImage> GetBitmapImagesFromBlobs(Bitmap source, Blob[] blobs)
        {

            List<BitmapImage> ImgList = new List<BitmapImage>();

            foreach (Blob blob in blobs)
            {
                ImgList.Add(GetBitmapImage(GetBitmapFromBlob(source, blob)));
            }
            return ImgList;

        }

        /// <summary>
        /// Метод конвертирует изображение из Bitmap в BitmapImage.
        /// Необходим для отображения изображения в XAML-коде.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>BitmapImage</returns>
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

        /// <summary>
        /// Возвращает объект класса Color, который является средним значением цвета области передаваемых параметров.
        /// </summary>
        /// <param name="source">Источник</param>
        /// <param name="x">Координата по X</param>
        /// <param name="y">Координата по Y</param>
        /// <param name="size">Размерность сетки.(Квадрат ширины и высоты)</param>
        /// <returns>Среднее значение цвета.</returns>
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

            Color tmp = Color.FromArgb(255, R / lenght, G / lenght, B / lenght);
            return tmp;
        }

        /// <summary>
        /// Метод возвращает объект класса AreaRect.
        /// </summary>
        /// <param name="source">Источник изображения.</param>
        /// <param name="x">Начальная координата X.</param>
        /// <param name="y">Начальная координата Y.</param>
        /// <param name="size">Размерность сетки.</param>
        /// <param name="scale">Масштаб.</param>
        /// <param name="isBoundary">Флаг определяющий тип выходных данных.</param>
        /// <returns></returns>
        private static AreaRect GetPixelsFromArea(Bitmap source, int x, int y, int size, int scale, bool isBoundary = false)
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

                    if (isBoundary)
                    {
                        if (pixels[index] > 60)
                        {
                            pixels[index] = 255;
                        }
                        else
                        {
                            pixels[index] = 0;
                        }
                    }

                    index++;

                }
            }
            return new AreaRect(pixels);
        }

        /// <summary>
        /// Метод возвращает объект класса Bitmap из 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static Bitmap GetBitmapFromAreaRect(AreaRect source)
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

        /// <summary>
        /// Метод объединения двух изображений. 
        /// Необходим для отображения калибровочной зоны в режиме калибровки.
        /// </summary>
        /// <param name="img1">Изображение левой зоны.</param>
        /// <param name="img2">Изображение правой зоны.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Метод конвертации из HSV в RGB формат. 
        /// </summary>
        /// <param name="hue">Значение тона.</param>
        /// <param name="saturation">Значение контрастности.</param>
        /// <param name="value">Значение светлости.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Метод конвертации RGB цвета в HSV структуру.
        /// Возвращает значения в выходные параметры.
        /// </summary>
        /// <param name="color">Источник</param>
        /// <param name="hue">Возвращаемое значение тона</param>
        /// <param name="saturation">Возвращаемое значение насыщенности</param>
        /// <param name="value">Возвращаемое значение светлости</param>
        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        /// <summary>
        /// Возвращает изображение с откорректированным цветовым балансом по передаваемым параметрам.
        /// /// </summary>
        /// <param name="sourceBitmap">Источник изображения</param>
        /// <param name="blueLevel">Значение голубого канала.</param>
        /// <param name="greenLevel">Значение зелёного канала.</param>
        /// <param name="redLevel">Значение красного канала.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Сохраняет коллекцию калибровочных углов в файл.
        /// </summary>
        public static void SaveCorners()
        {
            SaveData(AreaRectTemplates, @"corners_data.json");
        }

        /// <summary>
        /// Метод сериализации и сохранения объекта в файл.
        /// </summary>
        /// <param name="obj">Сериализуемый объект.</param>
        /// <param name="fileName">Имя файла.</param>
        public static void SaveData(object obj, string fileName)
        {
            try
            {
                using (StreamWriter file = File.CreateText(fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, obj);
                }
            }
            catch
            { }

        }

        /// <summary>
        /// Метод десериализации объекта из файла.
        /// </summary>
        /// <param name="fileName">Имя файла.</param>
        public static object ReadData<T>(string fileName)
        {
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader file = File.OpenText(fileName))
                {
                    return serializer.Deserialize(file, typeof(T));
                }

            }
            return null;
        }

        /// <summary>
        /// Метод очистки коллекции калибровочных углов.
        /// </summary>
        public static void ClearCorners()
        {
            AreaRectTemplates.Clear();
            SaveCorners();
        }

    }
}
