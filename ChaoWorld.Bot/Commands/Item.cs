using System;
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

            // TODO: Implement remaining item types
            //  * Seeds - I want to do more than just give them a bunch of fruits. Make them water their plants daily, and it will yield fruit that can be sold or used (until it dies).
            //  * Special - suspicious potions for reincarnation, chaos juice for better stat yield when reincarnating, negative mirrors, toy parts for building omochao...
            //  * Hats/Clothing/etc - Might hold off for a while on this so we can have proper images
            try
            {
                var category = ItemBase.GetCategory(item.ItemType);
                switch (category)
                {
                    case ItemBase.ItemCategories.Egg:
                        await HandleUseEgg(ctx, item);
                        break;
                    case ItemBase.ItemCategories.Fruit:
                        await HandleUseFruit(ctx, item);
                        break;
                    default:
                        await ctx.Reply($"{Emojis.Error} {item.ItemType.GetDescription()} cannot be used right now.");
                        break;
                }
            }
            catch (Exception e)
            {
                await _repo.LogMessage($"Item handler failed for item {item.Id} ({item.ItemType}): {e.Message}");
                await ctx.Reply($"{Emojis.Error} {item.ItemType.GetDescription()} cannot be used right now.");
            }
            finally
            {
                // Ensure we consume the item regardless of any errors handling it - otherwise people can get infinite use items...
                await _repo.UseItem(item);
            }
        }

        private async Task HandleUseEgg(Context ctx, Core.Item egg)
        {
            var chao = new Core.Chao();
            chao.Initialize(egg.ItemType);
            chao = await _repo.CreateChao(ctx.Garden.Id, chao);
            await ctx.Reply($"{Emojis.Success} {egg.ItemType.GetDescription()} hatched! Your chao (ID: `{chao.Id}`) is currently unnamed. Use `!chao {chao.Id} rename {{new name}}` to give it a name.");
        }

        private async Task HandleUseFruit(Context ctx, Core.Item fruit)
        {
            var chao = await ctx.MatchChao() ?? await _repo.GetActiveChaoForGarden(ctx.Garden.Id.Value);
            var effect = string.Empty;
            if (chao != null)
            {
                var statIncreaseAmount = ItemBase.GetPrice(fruit.ItemType) / 10;
                switch (fruit.ItemType)
                {
                    case ItemBase.ItemTypes.RoundFruit:
                    case ItemBase.ItemTypes.SquareFruit:
                    case ItemBase.ItemTypes.TriangleFruit:
                    case ItemBase.ItemTypes.TastyFruit:
                    case ItemBase.ItemTypes.StrongFruit:
                    case ItemBase.ItemTypes.HeartFruit: // TODO: Implement breeding
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's stamina improved.";
                        break;
                    case ItemBase.ItemTypes.Mushroom:
                        var random = new Random();
                        if (random.Next(1, 3) == 1) {
                            chao.RaiseLuck(statIncreaseAmount);
                            effect = " Your chao's luck improved.";
                        } else
                        {
                            chao.RaiseStamina(statIncreaseAmount);
                            effect = " Your chao's stamina improved.";
                        }
                        break;
                    case ItemBase.ItemTypes.ChaoFruit:
                    case ItemBase.ItemTypes.MintCandy:
                        chao.RaiseSwim(statIncreaseAmount);
                        chao.RaiseFly(statIncreaseAmount);
                        chao.RaiseRun(statIncreaseAmount);
                        chao.RaisePower(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.RaiseIntelligence(statIncreaseAmount);
                        chao.RaiseLuck(statIncreaseAmount);
                        effect = " Your chao's stats all improved.";
                        break;
                    case ItemBase.ItemTypes.SwimFruit:
                        chao.RaiseSwim(statIncreaseAmount);
                        effect = " Your chao's swimming improved.";
                        break;
                    case ItemBase.ItemTypes.FlyFruit:
                        chao.RaiseFly(statIncreaseAmount);
                        effect = " Your chao's flying improved.";
                        break;
                    case ItemBase.ItemTypes.RunFruit:
                        chao.RaiseRun(statIncreaseAmount);
                        effect = " Your chao's running improved.";
                        break;
                    case ItemBase.ItemTypes.PowerFruit:
                        chao.RaisePower(statIncreaseAmount);
                        effect = " Your chao's climbing improved.";
                        break;
                    case ItemBase.ItemTypes.SmartFruit:
                        chao.RaiseIntelligence(statIncreaseAmount);
                        effect = " Your chao's intelligence improved.";
                        break;
                    case ItemBase.ItemTypes.LuckyMushroom:
                        chao.RaiseLuck(statIncreaseAmount);
                        effect = " Your chao's luck improved.";
                        break;
                    case ItemBase.ItemTypes.HyperSwimFruit:
                        if (chao.SwimGrade == Core.Chao.StatGrades.S)
                        {
                            chao.SwimGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden swimming potential.";
                        } else
                        {
                            chao.RaiseSwim(100);
                            effect = " Your chao's swimming improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperFlyFruit:
                        if (chao.FlyGrade == Core.Chao.StatGrades.S)
                        {
                            chao.FlyGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden flying potential.";
                        }
                        else
                        {
                            chao.RaiseFly(100);
                            effect = " Your chao's flying improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperRunFruit:
                        if (chao.RunGrade == Core.Chao.StatGrades.S)
                        {
                            chao.RunGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden running potential.";
                        }
                        else
                        {
                            chao.RaiseRun(100);
                            effect = " Your chao's running improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperPowerFruit:
                        if (chao.PowerGrade == Core.Chao.StatGrades.S)
                        {
                            chao.PowerGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden might.";
                        }
                        else
                        {
                            chao.RaisePower(100);
                            effect = " Your chao's climbing improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperStaminaFruit:
                        if (chao.StaminaGrade == Core.Chao.StatGrades.S)
                        {
                            chao.StaminaGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden endurance.";
                        }
                        else
                        {
                            chao.RaiseStamina(100);
                            effect = " Your chao's stamina improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperSmartFruit:
                        if (chao.IntelligenceGrade == Core.Chao.StatGrades.S)
                        {
                            chao.IntelligenceGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden genius.";
                        }
                        else
                        {
                            chao.RaiseIntelligence(100);
                            effect = " Your chao's intelligence improved, but you were expecting more.";
                        }
                        break;
                    case ItemBase.ItemTypes.HyperLuckyMushroom:
                        if (chao.LuckGrade == Core.Chao.StatGrades.S)
                        {
                            chao.LuckGrade = Core.Chao.StatGrades.X;
                            effect = " Chaos energy unlocks your chao's hidden fortune.";
                        }
                        else
                        {
                            chao.RaiseLuck(100);
                            effect = " Your chao's luck improved, but you were expecting more.";
                        }
                        break;

                }
                await _repo.UpdateChao(chao);
                await ctx.Reply($"{Emojis.Success} {chao.Name} ate the {fruit.ItemType.GetDescription()}!{effect}");
            }
            else
            {
                // No chao specified, and we couldn't find an active chao...
                await ctx.Reply($"{Emojis.Error} You can't eat {fruit.ItemType.GetDescription()}. Did you mean to give this to your chao? (Try `!item \"{fruit.ItemType.GetDescription()}\" use {{chao id/name}}` instead.)");
            }
            await _repo.UpdateChao(chao);
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