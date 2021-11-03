using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChaoWorld.Core;

namespace ChaoWorld.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ItemCategoryAttribute: Attribute
    {
        public ItemBase.ItemCategories Category { get; }
        public ItemCategoryAttribute(ItemBase.ItemCategories category)
        {
            Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PriceAttribute : Attribute
    {
        public int Price { get; }
        public PriceAttribute(int price)
        {
            Price = price;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PrimaryColorAttribute: Attribute
    {
        public Chao.Colors Color { get; }
        public PrimaryColorAttribute(Chao.Colors color)
        {
            Color = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SecondaryColorAttribute: Attribute
    {
        public Chao.Colors Color { get; }
        public SecondaryColorAttribute(Chao.Colors color)
        {
            Color = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShinyAttribute: Attribute
    {
        public ShinyAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TwoToneAttribute: Attribute
    {
        public TwoToneAttribute()
        {
        }
    }


}
