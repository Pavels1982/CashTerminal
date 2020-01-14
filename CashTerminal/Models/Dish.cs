using CashTerminal.Services;
using System;
using System.ComponentModel;

namespace CashTerminal.Models
{
    public class Dish : Iitem, INotifyPropertyChanged
    {
        private double number;

        private static Random rnd = new Random();

        public int Id { get; set; }

        public string Name { get; set; }

        public int Index { get; set; }

        public double Number
        {
            get
            {
                return number;
            }

            set
            {
                number = value;
                this.TotalPrice = Math.Round(this.Price * value,2);
            }

        }

        public double Price { get; set; }

        public double TotalPrice { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Dish(string name)
        {
            this.Id = this.GetHashCode();
            this.Name = name;
            this.Price = rnd.Next(10, 150);
            this.Price += Math.Round(rnd.NextDouble(), 2);
            this.Number = 1;
        }

        public Dish Clone()
        {
            return new Dish(this.Name)
            {
                Id = this.GetHashCode(),
                Name = this.Name,
                Price = this.Price,
                Number = this.Number,
                TotalPrice = this.TotalPrice

            };
        }

    }
}
