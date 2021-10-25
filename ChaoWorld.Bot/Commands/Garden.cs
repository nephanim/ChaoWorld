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

        public async Task Query(Context ctx, Core.Garden system)
        {
            if (system == null) throw Errors.NoGardenError;

            await ctx.Reply(embed: await _embeds.CreateSystemEmbed(ctx, system));
        }

        public async Task New(Context ctx)
        {
            ctx.CheckNoGarden();

            var garden = await _repo.CreateGarden();
            await _repo.AddAccount(garden.Id, ctx.Author.Id);

            var chao = new Core.Chao();
            chao.Initialize(isStarterChao: true);
            await _repo.CreateChao(garden.Id, chao);

            // TODO: better message, perhaps embed like in groups?
            await ctx.Reply($"{Emojis.Success} Your garden has been created. Type `!garden` to view it, and type `!garden help` for more information about commands you can use now.");
        }
    }
}