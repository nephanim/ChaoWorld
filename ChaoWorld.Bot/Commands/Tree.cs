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

        public async Task WaterTree(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);

            // They can water as often as they want, but depending on the frequency, it may have positive or negative effects...
            var now = SystemClock.Instance.GetCurrentInstant();
            if (tree.NextWatering < now)
            {
                // It's time to water - good!
                tree.Health += 8;
                if (tree.Health > 100)
                    tree.Health = 100;
                await ctx.Reply($"{Emojis.Success} You water the {tree.Name}. It looks refreshed and healthy.");
            } else
            {
                // Uh oh, we're overwatering the tree...
                tree.Health -= 1;
                if (tree.Health < 0)
                    tree.Health = 0;
                await ctx.Reply($"{Emojis.Success} You water the {tree.Name}. It looks a little soggy...");
            }
            await _repo.UpdateTree(tree);
        }

        public async Task CollectFruit(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);

            // TODO
        }

        private async Task RemoveTree(Context ctx, Core.Tree tree)
        {
            ctx.CheckGarden();
            ctx.CheckOwnTree(tree);
            await ConfirmRemoveTree(ctx, tree);

            // TODO: Collect their fruit for them at least...

            // Make a stump out of that sucker
            await _repo.DeleteTree(tree);
            await ctx.Reply($"{Emojis.Success} {tree.Name} was removed from your orchard.");
        }

        public async Task ConfirmRemoveTree(Context ctx, Core.Tree tree)
        {
            var prompt = $"{Emojis.Warn} Are you sure you want to remove {tree.Name}?";
            if (!(await ctx.PromptYesNo(prompt, "Confirm")))
                throw Errors.GenericCancelled();
        }
    }
}