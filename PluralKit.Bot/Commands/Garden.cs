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

            var systemName = ctx.RemainderOrNull();
            if (systemName != null && systemName.Length > Limits.MaxSystemNameLength)
                throw Errors.StringTooLongError("Garden name", systemName.Length, Limits.MaxSystemNameLength);

            var system = await _repo.CreateSystem(systemName);
            await _repo.AddAccount(system.Id, ctx.Author.Id);

            // TODO: better message, perhaps embed like in groups?
            await ctx.Reply($"{Emojis.Success} Your system has been created. Type `pk;system` to view it, and type `pk;system help` for more information about commands you can use now. Now that you have that set up, check out the getting started guide on setting up members and proxies: <https://pluralkit.me/start>");
        }
    }
}