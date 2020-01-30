using CashTerminal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCam;

namespace CashTerminal.Data
{
    public class DishData
    {
        private readonly string[] fruits = { "Апельсин", "Яблоко зеленое", "Мандарин", "Банан"};
        private readonly string[] bakery = { "Хлеб ржаной", "Хлеб пшеничный", "Булочки с маком", "Хлебцы оренбургские", "Булочки детские", "Булочки октябренок", "Булочки колобок", "Булочки горчичные", "Булочки столичные", "Булочки с тмином", "Рожки сдобные", "Пицца" };
        private readonly string[] soups = { "Борщ", "Борщ полупорция", "Солянка", "Солянка полупорция", "Куриный бульон", "Куриный бульон полупорция" };
        private readonly string[] sidedish = { "Картофель отварной", "Картофель по-деревенски", "Картофельное пюре", "Гречка", "Рис", "Овощи по-итальянски", "Спагетти" };
        private readonly string[] drinkables = { "Чай", "Кофе", "Морс", "Апельсиновый сок", "Мультифруктовый сок", "Облепиховый сок", "Кисель" };
        private readonly string[] porridge = { "Перловая каша", "Рисовая каша", "Гречневая каша", "Пшенная каша" };
        private readonly string[] salads = { "Салат Цезарь", "Салат Оливье", "Салат Мимоза", "Салат пальчики оближешь", "Салат мясной с гранатом", "Салат Грузинский", "Салат Римский", "Салат Императорский", "Салат с морской капустой" };
        private readonly string[] other = { "Яйцо", "Сухарики ржаные", "Сухарики пшеничные", "Соус тар-тар", "Соус майонез", "Соус сметана", "Соус кетчуп" };



        public List<DishGroup> DishGroup = new List<DishGroup>();


        public List<Dish> GetGroup(string[] items)
        {
            List<Dish> result = new List<Dish>();
            result.Add(new Dish("...Назад"));
            foreach (var item in items)
            {
                result.Add(new Dish(item));
            }
            return result;
        }

        //private List<DishGroup> GetDishGroup()
        //{
        //    List<DishGroup> ListGroup = new List<DishGroup>();
        //    ListGroup.Add(new DishGroup() { Name = "Фрукты", ListDishes = GetGroup(fruits) });
        //    ListGroup.Add(new DishGroup() { Name = "Выпечка", ListDishes = GetGroup(bakery) });
        //    ListGroup.Add(new DishGroup() { Name = "Супы", ListDishes = GetGroup(soups) });
        //    ListGroup.Add(new DishGroup() { Name = "Гарниры", ListDishes = GetGroup(sidedish) });
        //    ListGroup.Add(new DishGroup() { Name = "Напитки", ListDishes = GetGroup(drinkables) });
        //    ListGroup.Add(new DishGroup() { Name = "Каши", ListDishes = GetGroup(porridge) });
        //    ListGroup.Add(new DishGroup() { Name = "Салаты", ListDishes = GetGroup(salads) });
        //    ListGroup.Add(new DishGroup() { Name = "Прочее", ListDishes = GetGroup(other) });

        //    return ListGroup;
        //}


        private List<DishGroup> GetDishGroup()
        {
            List<DishGroup> ListGroup = new List<DishGroup>();
            var result = WebCamConnect.ReadData<List<DishGroup>>(@"dish_data.json") as List<DishGroup>;
            if (result != null)
            {
                return  result;
            }
            else
            {
               return CreateDishData();
            }


           }

        private List<DishGroup> CreateDishData()
        {

            List<DishGroup> ListGroup = new List<DishGroup>();
            ListGroup.Add(new DishGroup() { Name = "Фрукты", ListDishes = GetGroup(fruits) });
            ListGroup.Add(new DishGroup() { Name = "Выпечка", ListDishes = GetGroup(bakery) });
            ListGroup.Add(new DishGroup() { Name = "Супы", ListDishes = GetGroup(soups) });
            ListGroup.Add(new DishGroup() { Name = "Гарниры", ListDishes = GetGroup(sidedish) });
            ListGroup.Add(new DishGroup() { Name = "Напитки", ListDishes = GetGroup(drinkables) });
            ListGroup.Add(new DishGroup() { Name = "Каши", ListDishes = GetGroup(porridge) });
            ListGroup.Add(new DishGroup() { Name = "Салаты", ListDishes = GetGroup(salads) });
            ListGroup.Add(new DishGroup() { Name = "Прочее", ListDishes = GetGroup(other) });

            WebCamConnect.SaveData(ListGroup, @"dish_data.json");

            return ListGroup;
        }





        public DishData()
        {
           DishGroup.AddRange( GetDishGroup());
        }
    }
}
