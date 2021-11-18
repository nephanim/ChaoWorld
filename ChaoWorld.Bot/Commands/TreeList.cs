using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class TreeList
    {
        private readonly IDatabase _db;

        public TreeList(IDatabase db)
        {
            _db = db;
        }

        public async Task OrchardList(Context ctx)
        {
            var gardenOwner = await ctx.GetCachedGardenOwner(ctx.Garden.Id);
            var title = $"{gardenOwner}'s Orchard";

            var filter = ctx.RemainderOrNull();
            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.Replace("\"", string.Empty).Replace(" ", string.Empty);
                title = $"{gardenOwner}'s Orchard (Type: {filter})";
            }

            await ctx.RenderTreeList(_db, title, filter);
        }
    }
}