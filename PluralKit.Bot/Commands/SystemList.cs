using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class SystemList
    {
        private readonly IDatabase _db;

        public SystemList(IDatabase db)
        {
            _db = db;
        }

        public async Task MemberList(Context ctx, Core.Garden target)
        {
            if (target == null) throw Errors.NoGardenError;

            var opts = ctx.ParseMemberListOptions();
            await ctx.RenderMemberList(_db, target.Id, GetEmbedTitle(target, opts), target.Color, opts);
        }

        private string GetEmbedTitle(Core.Garden target, MemberListOptions opts)
        {
            var title = new StringBuilder("Chao of ");

            if (target.Name != null)
                title.Append($"{target.Name} (`{target.Hid}`)");
            else
                title.Append($"`{target.Hid}`");

            if (opts.Search != null)
                title.Append($" matching **{opts.Search}**");

            return title.ToString();
        }
    }
}