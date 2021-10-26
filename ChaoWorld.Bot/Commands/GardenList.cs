using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class GardenList
    {
        private readonly IDatabase _db;

        public GardenList(IDatabase db)
        {
            _db = db;
        }

        public async Task ChaoList(Context ctx, Core.Garden target)
        {
            if (target == null) throw Errors.NoGardenError;

            var opts = ctx.ParseChaoListOptions();
            await ctx.RenderChaoList(_db, target.Id, GetEmbedTitle(ctx, target, opts), null, opts);
        }

        private string GetEmbedTitle(Context ctx, Core.Garden target, ChaoListOptions opts)
        {
            var title = new StringBuilder($"{ctx.Author.Username}'s Chao");

            if (opts.Search != null)
                title.Append($" matching **{opts.Search}**");

            return title.ToString();
        }
    }
}