using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashTerminal.Models
{
    public class HSVColor
    {
          public double Hue { get; set; }
          public double Saturation { get; set; }
          public double Value { get; set; }

        public HSVColor(double hue, double saturation, double value)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Value = value;
        }
        public HSVColor()
        {
            this.Hue = 0;
            this.Saturation = 0;
            this.Value = 0;
        }

    }

    public class ObjectStruct
    {
        public int Radius { get; set; } = 0;
        public int[] Tone { get; set; }
    }
}
