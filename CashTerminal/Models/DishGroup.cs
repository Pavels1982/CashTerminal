using CashTerminal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;

namespace CashTerminal.Models
{
    public class DishGroup : Iitem
    {
        public List<Dish> ListDishes { get; set; } = new List<Dish>();
        public ObjectStruct ObjectStruct { get; set; } = new ObjectStruct(new Color(), 0,null);
        public string Name { get; set; }

     }
}
