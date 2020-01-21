using CashTerminal.Services;
using System;
using System.ComponentModel;
using System.Drawing;

namespace CashTerminal.Models
{
    public class Dish : Iitem, INotifyPropertyChanged
    {
        #region Variables
       
        /// <summary>
        /// Объект генерации случайных чисел.
        /// </summary>
        private static Random rnd = new Random();

        /// <summary>
        /// количество блюд в корзине.
        /// </summary>
        private double number;
        #endregion

        #region Properties
        /// <summary>
        /// Get or set ID блюда.
        /// </summary>
        public int Id { get; set; }


        public ObjectStruct ObjectStruct { get; set; } = new ObjectStruct(new Color(), 0);
        /// <summary>
        /// Get or set наименование блюда.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set порядковый номер в корзине.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Get or set количество блюд в корзине. При изменении выполняет автоматическое вычесление общей стоимости блюда.
        /// </summary>
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

        /// <summary>
        /// Get or set цена блюда за единицу.
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Get or set цена товара с учётом количества.
        /// </summary>
        public double TotalPrice { get; set; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Конструктор по-умолчанию.
        /// </summary>
        /// <param name="name">Имя </param>
        public Dish(string name)
        {
            this.Id = this.GetHashCode();
            this.Name = name;
            this.Price = rnd.Next(10, 150);
            this.Price += Math.Round(rnd.NextDouble(), 2);
            this.Number = 1;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Возвращает копию экземпляра в виде нового объекта.
        /// </summary>
        /// <returns></returns>
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
        #endregion

    }
}
