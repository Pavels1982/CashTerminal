﻿using System;
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

        public HSVColor(double hue = 0, double saturation = 0, double value = 0)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Value = value;
        }
      
    }

    public class ObjectStruct
    {
        public int Radius { get; set; } = 0;
        public HSVColor[] Tone { get; set; }
        public List<int?> Id { get; set; } = new List<int?>();
    }
}
