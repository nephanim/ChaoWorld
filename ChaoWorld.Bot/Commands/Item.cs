using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Item
    {
        private readonly EmbedService _embeds;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public Item(EmbedService embeds, IDatabase db, ModelRepository repo)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
        }

        public async Task UseItem(Context ctx, Core.Item item)
        {
            ctx.CheckGarden();
            ctx.CheckOwnItem(item);
            await _repo.UseItem(item);
            // TODO: Implement ItemUseHandler to figure out what it does
            await ctx.Reply($"{Emojis.Success} Used item {item.Id} ({item.ItemCategory} -> {item.ItemType.GetDescription()})");
        }

        public async Task BuyItem(Context ctx, MarketItem item)
        {
            ctx.CheckGarden();
            var remainingInput = ctx.RemainderOrNull();
            int quantity = 1;
            if (int.TryParse(remainingInput, out int parsedQuantity))
                quantity = parsedQuantity;

            // Make sure the item is actually available for purchase
            var marketItem = await _repo.GetMarketItemByType(item.ItemType);
            if (marketItem != null)
            {
                // Make sure the quantity of the item is sufficient for purchase
                if (marketItem.Quantity >= quantity)
                {
                    // Make sure the garden can afford the transaction
                    var purchasePrice = marketItem.Price * quantity;
                    if (ctx.Garden.RingBalance >= purchasePrice)
                    {
                        // Take the item off the market (or reduce quantity as appropriate)
                        await _repo.BuyMarketItem(marketItem, quantity);

                        // Update the garden's ring balance
                        ctx.Garden.RingBalance -= purchasePrice;
                        await _repo.UpdateGarden(ctx.Garden);

                        // Now update inventory - increase quantity of existing item or add new item
                        var existingInventoryItem = await _repo.GetItemByType(ctx.Garden.Id.Value, (int)marketItem.ItemType);
                        if (existingInventoryItem != null)
                        {
                            // We already have some of this item - just update the quantity appropriately
                            existingInventoryItem.Quantity += quantity;
                            await _repo.UpdateItem(existingInventoryItem);
                        }
                        else
                        {
                            // We don't have any of this item - add it to our inventory
                            var inventoryItem = new Core.Item()
                            {
                                ItemType = marketItem.ItemType,
                                ItemCategory = marketItem.ItemCategory,
                                Quantity = quantity,
                                GardenId = ctx.Garden.Id.Value,
                            };
                            await _repo.AddItem(ctx.Garden.Id.Value, inventoryItem);
                        }
                        await ctx.Reply($"{Emojis.Success} Purchased {quantity} {item.ItemType.GetDescription()}. Your current balance is {ctx.Garden.RingBalance} rings.");
                    }
                    else
                        await ctx.Reply($"{Emojis.Error} Purchase would require {purchasePrice} rings, but you only have {ctx.Garden.RingBalance}.");
                }
                else
                    await ctx.Reply($"{Emojis.Error} Current stock of {item.ItemType.GetDescription()} is only {marketItem.Quantity}. Check `!market list` for current listings.");
            }
            else
                await ctx.Reply($"{Emojis.Error} {item.ItemType.GetDescription()} is out of stock. Check `!market list` for current listings.");
        }
    }
}