using CashTerminal.Data;
using CashTerminal.Models;
using CashTerminal.Services;
using CashTerminal.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WebCam;

namespace CashTerminal.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Iitem> ItemList { get; set; } = new ObservableCollection<Iitem>();
        public ObservableCollection<BitmapImage> ObjectList { get; set; } = new ObservableCollection<BitmapImage>();
        private ObservableCollection<Dish> basketList = new ObservableCollection<Dish>();
        public ObservableCollection<Dish> BasketList
        {
            get
            {
                return basketList;
            }

            set
            {
                basketList = value;
            }

        }

        private double totalPrice;

        public double TotalPrice
        {
            get
            {
                return totalPrice;
            }
            set
            {
                totalPrice = value;
            }
        }

        private Dish selectedBasketItem;

        public Dish SelectedBasketItem
        {
            get
            {
                return selectedBasketItem;
            }

            set
            {
                selectedBasketItem = value;
                if (value != null)
                {
                    CalculatorValue = selectedBasketItem.Number.ToString().Replace(",", ".");
                }
            }

        }

        private DishData DishData { get; set; }

        public string CalculatorValue { get; set; } = "0";

        public ICommand CalValCommand
        {
            get
            {
                return new RelayCommand((param) => CalButtonClick(param));
            }
        }

        public ICommand SetNumberCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    if (SelectedBasketItem != null)
                    {
                        (SelectedBasketItem as Dish).Number = Double.Parse(CalculatorValue.Replace(".", ","));
                        CalculateBasketPrice();
                    }
                });
                     
            }
        }

        public ICommand DeleteItemCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    DeleteItemFromBasket(o);
                });
            }
        }

        private void DeleteItemFromBasket(object o)
        {
            if (o != null)
            {
                Dish removableObject = o as Dish;
                BasketList.Remove(removableObject);
                CalculateBasketPrice();
            }
        }

        private void CalculateBasketPrice()
        {
           this.TotalPrice = basketList.Select(e => e.TotalPrice).Sum();
            int i = 1;
            foreach (var item in basketList)
            {
                item.Index = i;
                i++;
            }
        }

        public ICommand PaymentCommand
        {
            get
            {
                return new RelayCommand((o) => 
                {
                    if (BasketList.Count == 0)
                    {
                        MessageBox.Show("Корзина пуста! Добавьте товар.");
                    }
                    else
                    {
                        PostData(BasketList);
                        BasketList.Clear();
                        SelectedBasketItem = null;
                        CalculateBasketPrice();
                        MessageBox.Show("Оплата произведена!");
                    }

                });
            }
        }

        public ICommand ItemClickCommand
        {
            get
            {
                return new RelayCommand((o) =>
                {
                    if (o.GetType().Equals(typeof(DishGroup)))
                    {
                        ItemList = GetDishesList(o);
                    }else if (o.GetType().Equals(typeof(Dish)))
                    {
                        if ((o as Dish).Name == "...Назад")
                        {
                            ItemList = GetDishGroupList();
                        }
                        else
                        {
                            AddToBasket(o);
                         
                        }
                    }

                });
            }
        }

        private void AddToBasket(object o, bool isNewData = true)
        {
            Dish clone = (o as Dish).Clone();

            BasketList.Add(clone);
            SelectedBasketItem = clone;
            CalculateBasketPrice();
        }

        private void PostData(ObservableCollection<Dish> basketList)
        {
            List<int?> id = new List<int?>();
            basketList.ToList().ForEach(dish => id.Add(dish.Id));
            WebCamConnect.CheckId(id);
        }


        /// <summary>
        /// Конструктор по-умолчанию.
        /// </summary>
        public MainWindowViewModel()
        {
            DishData = new DishData();
            ItemList = GetDishGroupList();

            WebCamWindow win = new WebCamWindow();
            win.Show();
            WebCamConnect.NewObject += WebCamConnect_NewObject;
        }

        private BitmapImage ImageFromStruct(ObjectStruct obj)
        {
            int size = (int)Math.Sqrt(obj.Tone.Length);

            Bitmap img = new Bitmap(size,size);
            int index = 0;
            for (int x = 0; x < img.Height; x++)
            {
                for (int y = 0; y < img.Width; y++)
                {
                    double hue = obj.Tone[index].Hue;
                    double sat = obj.Tone[index].Saturation;
                    double val = obj.Tone[index].Value;
                    Color color = WebCamConnect.ColorFromHSV(hue,sat,val);
                    img.SetPixel(x, y, color);
                    index++;
                }
            }

            BitmapImage btm = new BitmapImage();
            using (MemoryStream memStream2 = new MemoryStream())
            {
                (img).Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
                memStream2.Position = 0;
                btm.BeginInit();
                btm.CacheOption = BitmapCacheOption.OnLoad;
                btm.UriSource = null;
                btm.StreamSource = memStream2;
                btm.EndInit();
            }

            return btm;
        }

        private void WebCamConnect_NewObject(List<int?> id)
        {
            BasketList.Clear();
            foreach (int? recId in id)
            {

                foreach (var group in DishData.DishGroup)
                {
                    if (group.ListDishes.Any(d => d.Id == recId))
                    {
                        AddToBasket(group.ListDishes.Find(d => d.Id == recId));
                        break;
                    }
                }

            }

           }

        private ObservableCollection<Iitem> GetDishGroupList()
        {
            ObservableCollection<Iitem> result = new ObservableCollection<Iitem>();
            DishData.DishGroup.ToList().ForEach(item => result.Add(item));
            return result;
        }

        private ObservableCollection<Iitem> GetDishesList(object o)
        {
            ObservableCollection<Iitem> result = new ObservableCollection<Iitem>();
            (o as DishGroup).ListDishes.ToList().ForEach(item => result.Add(item));
            return result;
        }

        private void CalButtonClick(object param)
        {
            int num;
            string s = param.ToString();

                bool result = Int32.TryParse(s, out num);
                if (result)
                {
                CalculatorValue += param;
                }
                else if (s == "Del")
                {
                    if (CalculatorValue.Count() > 0)
                    {
                    CalculatorValue = CalculatorValue.Remove(CalculatorValue.Count() - 1, 1);
                    }
                }
                else if (s == ".")
                {
                    if (!CalculatorValue.ToArray().Any(c => c.Equals(Char.Parse("."))))
                    CalculatorValue += param;
                }

            if (CalculatorValue.Count() > 1)
                if (CalculatorValue.ToArray().First().Equals(Char.Parse("0")) && !CalculatorValue.ToArray().Any(c => c.Equals(Char.Parse("."))) ) CalculatorValue = CalculatorValue.Remove(0, 1);

            if (CalculatorValue == string.Empty) CalculatorValue = "0";

        }

    }
}
