using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChaoWorld.Core;

namespace ChaoWorld.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PriceAttribute : Attribute
    {
        public int Price { get; }
        public PriceAttribute(int price)
        {
            Price = price;
        }
    }
}
