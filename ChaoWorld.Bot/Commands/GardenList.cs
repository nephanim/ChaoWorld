using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class GardenList
    {
        private readonly IDatabase _db;
        private readonly EmbedService _embeds;

        public GardenList(EmbedService embeds, IDatabase db)
        {
            _db = db;
            _embeds = embeds;
        }

        public async Task ChaoList(Context ctx, Core.Garden target)
        {
            if (target == null) throw Errors.NoGardenError;

            var accounts = await ctx.Repository.GetGardenAccounts(target.Id);
            var users = (await _embeds.GetUsers(accounts)).Select(x => x.User?.Username ?? $"({x.Id})");
            var firstUser = target.Id.Value == 0 ? "Professor Chao" : users.FirstOrDefault();

            var opts = ctx.ParseChaoListOptions();
            await ctx.RenderChaoList(_db, target.Id, GetEmbedTitle(firstUser, opts), null, opts);
        }

        private string GetEmbedTitle(string user, ChaoListOptions opts)
        {
            var title = new StringBuilder($"{user}'s Chao");

            if (opts.Search != null)
                title.Append($" matching **{opts.Search}**");

            return title.ToString();
        }
    }
}