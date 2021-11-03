using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class ItemList
    {
        private readonly IDatabase _db;

        public ItemList(IDatabase db)
        {
            _db = db;
        }

        public async Task InventoryItemList(Context ctx)
        {
            // Check what we should include
            var includeCategories = new List<Core.Item.ItemCategories>();
            var includeTypes = new List<Core.Item.ItemTypes>();
            var title = "Inventory (All Items)";

            var filter = ctx.RemainderOrNull();
            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.Replace("\"", string.Empty).Replace(" ", string.Empty);

                if (System.Enum.TryParse(filter, out Core.Item.ItemCategories category))
                {
                    includeCategories.Add(category);
                    title = $"Inventory (Category: {category.GetDescription()})";
                }
                else if (System.Enum.TryParse(filter, out Core.Item.ItemTypes type))
                {
                    includeTypes.Add(type);
                    title = $"Inventory (Type: {type.GetDescription()})";
                }
            }

            await ctx.RenderInventory(_db, includeCategories.ToArray(), includeTypes.ToArray(), title);
        }

        public async Task MarketItemList(Context ctx)
        {
            await ctx.RenderMarketList(_db);
        }
    }
}