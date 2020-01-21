using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashTerminal.Models
{
    public class ObjectStruct
    {
        public Color Color { get; set; }
        public int Radius { get; set; }


        public ObjectStruct(Color color, int radius) 
        {
            this.Color = color;
            this.Radius = radius;

        }
    }
}
