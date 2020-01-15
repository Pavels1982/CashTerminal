using CashTerminal.Data;
using CashTerminal.Models;
using CashTerminal.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CashTerminal.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Iitem> ItemList { get; set; } = new ObservableCollection<Iitem>();

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
                    CalculatorValue = selectedBasketItem.Number.ToString().Replace("," ,".");
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
                    if (o != null)
                    {
                        BasketList.Remove(o as Dish);
                        CalculateBasketPrice();
                    }

                });
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
                        MessageBox.Show("Оплата произведена!");
                        BasketList.Clear();
                        SelectedBasketItem = null;
                        CalculateBasketPrice();
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
                            Dish clone = (o as Dish).Clone();
                            BasketList.Add(clone);
                            SelectedBasketItem = clone;
                            CalculateBasketPrice();

                        }
                    }

                });
            }
        }

        /// <summary>
        /// Конструктор по-умолчанию.
        /// </summary>
        public MainWindowViewModel()
        {
            DishData = new DishData();
            ItemList = GetDishGroupList();

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
