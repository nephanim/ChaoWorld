using ChaoWorld.Core;
using System;
using System.Threading.Tasks;

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

        public async Task ItemInfo(Context ctx, ItemBase item)
        {
            await ctx.Reply(embed: await _embeds.CreateItemEmbed(item));
        }

        public async Task UseItem(Context ctx, Core.Item item)
        {
            ctx.CheckGarden();
            ctx.CheckOwnItem(item);

            // Since many items will require a chao target, go ahead and decide who we would use
            // Very important that this is scoped to the garden context! Item handlers should use this one rather than fetching their own
            var chao = await ctx.MatchChao(ctx.Garden.Id) ?? await _repo.GetActiveChaoForGarden(ctx.Garden.Id.Value);
            if (chao != null)
                ctx.CheckOwnChao(chao);

            // See if we can get a quantity from the input
            var input = ctx.RemainderOrNull();
            int.TryParse(input, out int quantity);
            if (quantity < 1)
                quantity = 1;

            if (item.Quantity >= quantity)
            {
                // TODO: Implement remaining item types
                //  * Seeds - I want to do more than just give them a bunch of fruits. Make them water their plants daily, and it will yield fruit that can be sold or used (until it dies).
                //  * Special - suspicious potions for reincarnation, chaos juice for better stat yield when reincarnating, negative mirrors, toy parts for building omochao...
                //  * Hats/Clothing/etc - Might hold off for a while on this so we can have proper images
                var success = false;
                var category = item.Category;
                switch (category)
                {
                    case ItemBase.ItemCategories.Egg:
                        quantity = 1; // We're not going to hatch multiple eggs at once, just deal with it
                        success = await TryHandleUseEgg(ctx, item);
                        break;
                    case ItemBase.ItemCategories.Fruit:
                        success = await TryHandleUseFruit(ctx, item, chao, quantity);
                        break;
                    case ItemBase.ItemCategories.Special:
                        success = await TryHandleUseSpecial(ctx, item, chao, quantity);
                        break;
                    default:
                        if (chao != null)
                            await ctx.Reply($"{Emojis.Error} {chao.Name} refused the {item.Name}. Maybe it can't be used right now.");
                        else
                            await ctx.Reply($"{Emojis.Error} {item.Name} didn't seem to do anything. Maybe it can't be used right now.");
                        break;
                }

                // Only consume the item if we were actually able to handle it
                if (success)
                    await _repo.UseItem(item, quantity);
            }
            else
            {
                await ctx.Reply($"{Emojis.Error} You only have {item.Quantity} of {item.Name}.");
            }
        }

        private async Task<bool> TryHandleUseEgg(Context ctx, Core.Item item)
        {
            await ConfirmUseItem(ctx, item, chao: null, quantity: 1);

            var chao = new Core.Chao();
            chao.Initialize(item.PrimaryColor.GetValueOrDefault(Core.Chao.Colors.Normal), item.SecondaryColor, item.IsShiny.GetValueOrDefault(false), item.IsTwoTone.GetValueOrDefault(true));
            chao = await _repo.CreateChao(ctx.Garden.Id, chao);
            await ctx.Reply($"{Emojis.Success} {item.Name} hatched! Your chao (ID: `{chao.Id}`) is currently unnamed. Use `!chao {chao.Id} rename {{new name}}` to give it a name.");
            return true;
        }

        private async Task<bool> TryHandleUseFruit(Context ctx, Core.Item item, Core.Chao chao, int quantity)
        {
            var effect = string.Empty;
            if (chao != null)
            {
                await ConfirmUseItem(ctx, item, chao, quantity);
                var affinityIncreaseAmount = 10 * quantity;
                var statIncreaseAmount = item.MarketPrice.GetValueOrDefault(80) * quantity / 10; // Using 80 here (tasty fruit value) so it at least does SOMETHING
                switch (item.EffectType)
                {
                    case ItemBase.ItemEffects.HeroAlignment:
                        chao.AlignmentValue += affinityIncreaseAmount;
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's alignment shifted slightly.";
                        break;
                    case ItemBase.ItemEffects.DarkAlignment:
                        chao.AlignmentValue -= affinityIncreaseAmount;
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's alignment shifted slightly.";
                        break;
                    case ItemBase.ItemEffects.StaminaProgressIncrease:
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's stamina improved.";
                        break;
                    case ItemBase.ItemEffects.Mushroom:
                        var random = new Random();
                        if (random.Next(1, 3) == 1) {
                            chao.RaiseLuck(statIncreaseAmount);
                            chao.RaiseStamina(statIncreaseAmount);
                            effect = " Your chao's luck improved.";
                        } else
                        {
                            chao.RaiseStamina(statIncreaseAmount);
                            effect = " Your chao's stamina improved.";
                        }
                        break;
                    case ItemBase.ItemEffects.AllStatsProgressIncrease:
                        chao.RaiseSwim(statIncreaseAmount);
                        chao.RaiseFly(statIncreaseAmount);
                        chao.RaiseRun(statIncreaseAmount);
                        chao.RaisePower(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.RaiseIntelligence(statIncreaseAmount);
                        chao.RaiseLuck(statIncreaseAmount);
                        effect = " Your chao's stats all improved.";
                        break;
                    case ItemBase.ItemEffects.SwimProgressIncrease:
                        chao.RaiseSwim(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's swimming improved.";
                        chao.FlySwimAffinity += affinityIncreaseAmount;
                        break;
                    case ItemBase.ItemEffects.FlyProgressIncrease:
                        chao.RaiseFly(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's flying improved.";
                        chao.FlySwimAffinity -= affinityIncreaseAmount;
                        break;
                    case ItemBase.ItemEffects.RunProgressIncrease:
                        chao.RaiseRun(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's running improved.";
                        chao.RunPowerAffinity -= affinityIncreaseAmount;
                        break;
                    case ItemBase.ItemEffects.PowerProgressIncrease:
                        chao.RaisePower(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's climbing improved.";
                        chao.RunPowerAffinity += affinityIncreaseAmount;
                        break;
                    case ItemBase.ItemEffects.IntelligenceProgressIncrease:
                        chao.RaiseIntelligence(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's intelligence improved.";
                        break;
                    case ItemBase.ItemEffects.LuckProgressIncrease:
                        chao.RaiseLuck(statIncreaseAmount);
                        chao.RaiseStamina(statIncreaseAmount);
                        effect = " Your chao's luck improved.";
                        break;
                    case ItemBase.ItemEffects.SwimGradeIncrease:
                        if (chao.SwimGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.SwimGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden swimming potential.";
                        break;
                    case ItemBase.ItemEffects.FlyGradeIncrease:
                        if (chao.FlyGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.FlyGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden flying potential.";
                        break;
                    case ItemBase.ItemEffects.RunGradeIncrease:
                        if (chao.RunGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.RunGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden running potential.";
                        break;
                    case ItemBase.ItemEffects.PowerGradeIncrease:
                        if (chao.PowerGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.PowerGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden might.";
                        break;
                    case ItemBase.ItemEffects.StaminaGradeIncrease:
                        if (chao.StaminaGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.StaminaGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden endurance.";
                        break;
                    case ItemBase.ItemEffects.IntelligenceGradeIncrease:
                        if (chao.IntelligenceGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.IntelligenceGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden genius.";
                        break;
                    case ItemBase.ItemEffects.LuckGradeIncrease:
                        if (chao.LuckGrade != Core.Chao.StatGrades.S)
                            return false;
                        if (quantity > 1)
                            return false;
                        chao.RaiseStamina(statIncreaseAmount);
                        chao.LuckGrade = Core.Chao.StatGrades.X;
                        effect = " Chaos energy unlocks your chao's hidden fortune.";
                        break;
                    default:
                        return false; // Clearly we don't know what to do with this
                }
                await _repo.UpdateChao(chao);
                var quantityText = quantity > 1 ? $" x{quantity}" : string.Empty;
                await ctx.Reply($"{Emojis.Success} {chao.Name} ate the {item.Name}{quantityText}!{effect}");
                return true;
            }
            else
            {
                // No chao specified, and we couldn't find an active chao...
                await ctx.Reply($"{Emojis.Error} You can't eat {item.Name}. Did you mean to give this to your chao? (Try `!item \"{item.Name}\" use {{chao id/name}}` instead.)");
                return false;
            }
        }

        public async Task<bool> TryHandleUseSpecial(Context ctx, Core.Item item, Core.Chao chao, int quantity)
        {
            if (chao != null)
            {
                await ConfirmUseItem(ctx, item, chao, quantity);
                var effect = string.Empty;
                switch (item.EffectType)
                {
                    case ItemBase.ItemEffects.Reincarnation:
                        if (quantity > 1)
                            return false;
                        chao.Reincarnate();
                        effect = " Your chao disappears into a cocoon, only to reemerge as a child.";
                        break;
                    case ItemBase.ItemEffects.ReincarnationFactorIncrease:
                        // TODO: Make a conscious decision about what this threshold should be, stats can get up to 9797 at 70%
                        if (chao.ReincarnationStatFactor + (0.01 * quantity) > 0.7)
                            return false;
                        chao.ReincarnationStatFactor += 0.01 * quantity;
                        effect = " Chaos energy strengthens your chao's soul.";
                        break;
                    case ItemBase.ItemEffects.Negativity:
                        if (quantity > 1)
                            return false;
                        chao.IsReversed = !chao.IsReversed;
                        effect = " Inside the mirror is the light to your chao's darkness, the darkness to their light. They trade places and the mirror shatters.";
                        break;
                }
                await _repo.UpdateChao(chao);
                var quantityText = quantity > 1 ? $" x{quantity}" : string.Empty;
                await ctx.Reply($"{Emojis.Success} {chao.Name} used the {item.Name}{quantityText}!{effect}");
                return true;
            }
            else
            {
                // No chao specified, and we couldn't find an active chao...
                await ctx.Reply($"{Emojis.Error} You can't use {item.Name} on yourself. Did you mean to give this to your chao? (Try `!item \"{item.Name}\" use {{chao id/name}}` instead.)");
                return false;
            }
        }

        public async Task ConfirmUseItem(Context ctx, Core.Item item, Core.Chao chao, int quantity)
        {
            var prompt = string.Empty;
            var quantityText = quantity > 1 ? $" x{quantity}" : string.Empty;
            if (chao != null)
                prompt = $"{Emojis.Warn} Are you sure you want to use {item.Name}{quantityText} on {chao.Name}?";
            else
                prompt = $"{Emojis.Warn} Are you sure you want to use {item.Name}{quantityText}?";

            if (!(await ctx.PromptYesNo(prompt, "Confirm")))
                throw Errors.GenericCancelled();
        }

        public async Task BuyItem(Context ctx, MarketItem item)
        {
            ctx.CheckGarden();
            var remainingInput = ctx.RemainderOrNull();
            int quantity = 1;
            if (int.TryParse(remainingInput, out int parsedQuantity))
                quantity = parsedQuantity;

            // Make sure the item is actually available for purchase
            var marketItem = await _repo.GetMarketItemByTypeId(item.TypeId);
            if (marketItem != null)
            {
                // Make sure the quantity of the item is sufficient for purchase
                if (marketItem.Quantity >= quantity)
                {
                    // Make sure the garden can afford the transaction
                    var purchasePrice = marketItem.MarketPrice.GetValueOrDefault(10000000) * quantity; // Mostly just a safeguard in case of missing prices...
                    if (ctx.Garden.RingBalance >= purchasePrice)
                    {
                        // Take the item off the market (or reduce quantity as appropriate)
                        await _repo.BuyMarketItem(marketItem, quantity);

                        // Update the garden's ring balance
                        ctx.Garden.RingBalance -= purchasePrice;
                        await _repo.UpdateGarden(ctx.Garden);

                        // Now update inventory - increase quantity of existing item or add new item
                        var existingInventoryItem = await _repo.GetInventoryItemByTypeId(ctx.Garden.Id.Value, marketItem.TypeId);
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
                                TypeId = marketItem.TypeId,
                                CategoryId = marketItem.CategoryId,
                                Quantity = quantity,
                                GardenId = ctx.Garden.Id.Value,
                            };
                            await _repo.AddItem(ctx.Garden.Id.Value, inventoryItem);
                        }
                        await ctx.Reply($"{Emojis.Success} Purchased {item.Name} x{quantity}. Your current balance is {ctx.Garden.RingBalance} rings.");
                    }
                    else
                        await ctx.Reply($"{Emojis.Error} Purchase would require {purchasePrice} rings, but you only have {ctx.Garden.RingBalance}.");
                }
                else
                    await ctx.Reply($"{Emojis.Error} Current stock of {item.Name} is only {marketItem.Quantity}. Check `!market list` for current listings.");
            }
            else
                await ctx.Reply($"{Emojis.Error} {item.Name} is out of stock. Check `!market list` for current listings.");
        }

        public async Task GiveItem(Context ctx, Core.Item item)
        {
            ctx.CheckOwnItem(item);
            if (await ctx.MatchUser() is { } targetAccount)
            {
                if (!await ctx.PromptYesNo($"{targetAccount.NameAndMention()} Would you like to accept the {item.Name} from {ctx.Author}?", "Accept", user: targetAccount, matchFlag: false))
                    throw Errors.GiveItemCanceled();

                var targetGarden = await _repo.GetGardenByAccount(targetAccount.Id);
                if (targetGarden != null)
                {
                    // We know who to give it to, so should be safe to proceed
                    await _repo.UseItem(item, 1); // The original item gets deleted first thing to eliminate risk of duping items
                    await _repo.AddItem(targetGarden.Id.Value, item);

                    // Now update the target inventory - increase quantity of existing item or add new item
                    var existingInventoryItem = await _repo.GetInventoryItemByTypeId(targetGarden.Id.Value, item.TypeId);
                    if (existingInventoryItem != null)
                    {
                        // We already have some of this item - just update the quantity appropriately
                        existingInventoryItem.Quantity += 1;
                        await _repo.UpdateItem(existingInventoryItem);
                    }
                    else
                    {
                        // We don't have any of this item - add it to our inventory
                        var inventoryItem = new Core.Item()
                        {
                            TypeId = item.TypeId,
                            CategoryId = item.CategoryId,
                            Quantity = 1,
                            GardenId = targetGarden.Id.Value
                        };
                        await _repo.AddItem(targetGarden.Id.Value, inventoryItem);
                    }
                }
                else
                    await ctx.Reply($"{Emojis.Error} Failed to deliver {item.Name} to {targetAccount.NameAndMention()}.");
            }
            else
            {
                await ctx.Reply($"{Emojis.Error} Please specify a user to give the {item.Name} to.");
            }
        }
    }
}