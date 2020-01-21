using CashTerminal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashTerminal.Services
{
    public interface Iitem
    {
         string Name { get; set; }
         ObjectStruct ObjectStruct { get; set; }
    }
}
