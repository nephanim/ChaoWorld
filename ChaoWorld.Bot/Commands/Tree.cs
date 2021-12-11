using ChaoWorld.Core;
using Myriad.Extensions;
using NodaTime;
using System;
using System.Threading.Tasks;

namespace ChaoWorld.Bot
{
    public class Tree
    {
        private readonly EmbedService _embeds;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public Tree(EmbedService embeds, IDatabase db, ModelRepository repo)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
        }

        public async Task TreeInfo(Context ctx, Core.Tree tree)
        {
            await ctx.Reply(embed: await _embeds.CreateTreeEmbed(ctx, tree));
        }

        public async Task WaterNext(Context ctx)
        {
            ctx.CheckGarden();

            // See if there are any thirsty trees...
            var tree = await _repo.GetThirstiestTreeForGarden(ctx.Garden.Id.Value);
            if (tree != null)
                await WaterTree(ctx, tree);
            else
                await ctx.Reply($"{Emojis.Warn} You decide not to water any of your trees. It looks like they're doing fine.");
        }

        public async Task WaterAll(Context ctx)
        {
            ctx.CheckGarden();

            var trees = await _repo.GetTreesForGarden(ctx.Garden.Id.Value);
            foreach (var tree in trees)
            {
                await WaterTree(ctx, tree);
            }
        }

        public async Task WaterTree(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);

            // Only allow watering if the tree is ready
            var timeUntilWatering = tree.TimeUntilWatering;
            var now = SystemClock.Instance.GetCurrentInstant();
            if (tree.NextWatering < now)
            {
                // It's time to water - good!
                tree.Health += new Random().Next(5, 10);
                tree.NextWatering = now.Plus(Duration.FromHours(4));
                if (tree.Health > 100)
                    tree.Health = 100;

                if (tree.Health > 75)
                    await ctx.Reply($"{Emojis.Success} You water the {tree.Name}. It looks refreshed and healthy.");
                else if (tree.Health > 25)
                    await ctx.Reply($"{Emojis.Success} You water the {tree.Name}. It's starting to grow stronger.");
                else
                    await ctx.Reply($"{Emojis.Success} You water the {tree.Name}. It will need some more attention later.");
            } else
            {
                // Uh oh, we're overwatering the tree...
                await ctx.Reply($"{Emojis.Warn} You decide not to water the {tree.Name}. It looks like it's too soon. Try again in {timeUntilWatering}.");
            }
            await _repo.UpdateTree(tree);
        }

        public async Task CollectNext(Context ctx)
        {
            ctx.CheckGarden();

            // See if there are any trees with fruit...
            var tree = await _repo.GetMostBountifulTreeForGarden(ctx.Garden.Id.Value);
            if (tree != null)
                await CollectFruit(ctx, tree);
            else
                await ctx.Reply($"{Emojis.Warn} There is nothing to harvest. Try again later.");
        }

        public async Task CollectAll(Context ctx)
        {
            ctx.CheckGarden();

            var trees = await _repo.GetTreesForGarden(ctx.Garden.Id.Value);
            foreach (var tree in trees)
            {
                await CollectFruit(ctx, tree);
            }
        }

        public async Task CollectFruit(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);

            if (tree.FruitQuantity > 0)
            {
                var quantity = tree.FruitQuantity;
                var yieldType = tree.Name.Replace(" Cluster", string.Empty).Replace(" Tree", string.Empty);
                await HarvestFruit(ctx.Garden, tree);
                await ctx.Reply($"{Emojis.Success} You harvested {yieldType} x{quantity} from the {tree.Name}.");
            }
            else
                await ctx.Reply($"{Emojis.Error} There is nothing to harvest. Try again later.");
        }

        public async Task RemoveTree(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);
            await ConfirmRemoveTree(ctx, tree);

            // Collect their fruit for them at least...
            await HarvestFruit(ctx.Garden, tree);

            // Make a stump out of that sucker
            await _repo.DeleteTree(tree);
            await ctx.Reply($"{Emojis.Success} {tree.Name} was removed from your orchard.");
        }

        private async Task HarvestFruit(Core.Garden garden, Core.Tree tree)
        {
            if (tree.FruitQuantity > 0)
            {
                var quantity = tree.FruitQuantity;
                tree.FruitQuantity = 0;
                await _repo.UpdateTree(tree); // We're updating this first as a duping precaution

                var existingItem = await _repo.GetInventoryItemByTypeId(garden.Id.Value, tree.FruitTypeId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    await _repo.UpdateItem(existingItem);
                }
                else
                {
                    var item = new Core.Item()
                    {
                        GardenId = garden.Id.Value,
                        Category = Core.ItemBase.ItemCategories.Fruit,
                        TypeId = tree.FruitTypeId,
                        Quantity = quantity
                    };
                    await _repo.AddItem(garden.Id.Value, item);
                }
            }
        }

        private async Task ConfirmRemoveTree(Context ctx, Core.Tree tree)
        {
            var prompt = $"{Emojis.Warn} Are you sure you want to remove {tree.Name}?";
            if (!(await ctx.PromptYesNo(prompt, "Confirm")))
                throw Errors.GenericCancelled();
        }
    }
}