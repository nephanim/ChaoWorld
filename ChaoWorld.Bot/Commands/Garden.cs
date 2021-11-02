using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Garden
    {
        private readonly EmbedService _embeds;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public Garden(EmbedService embeds, IDatabase db, ModelRepository repo)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
        }

        public async Task Query(Context ctx, Core.Garden garden)
        {
            if (garden == null) throw Errors.NoGardenError;

            await ctx.Reply(embed: await _embeds.CreateGardenEmbed(ctx, garden));
        }

        public async Task New(Context ctx)
        {
            ctx.CheckNoGarden();

            var garden = await _repo.CreateGarden();
            await _repo.AddAccount(garden.Id, ctx.Author.Id);

            var chao = new Core.Chao();
            chao.Initialize();
            await _repo.CreateChao(garden.Id, chao);

            // TODO: better message, perhaps embed like in groups?
            await ctx.Reply($"{Emojis.Success} Your garden has been created. Type `!garden` to view it, and type `!garden help` for more information about commands you can use now.");
        }

        public async Task ChangeActiveChao(Context ctx, Core.Garden garden)
        {
            ctx.CheckGarden();
            var chao = await ctx.PeekChao();

            if (chao != null)
            {
                ctx.CheckOwnChao(chao);
                garden.ActiveChao = chao.Id.Value;
                await _repo.UpdateGarden(garden);
                await ctx.Reply($"{Emojis.Success} {chao.Name} is now selected for your garden. Some commands can now be used without specifying a chao (e.g. `!race {{id}} join` instead of `!race {{id}} join {{chao id}}`.");
            }
            else
            {
                await ctx.Reply($"{Emojis.Error} Chao not found.");
            }
        }
    }
}