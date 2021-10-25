using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Random
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;

        private readonly global::System.Random randGen = new global::System.Random();

        public Random(EmbedService embeds, IDatabase db, ModelRepository repo)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
        }

        // todo: get postgresql to return one random member/group instead of querying all members/groups

        public async Task Member(Context ctx)
        {
            ctx.CheckGarden();

            var members = await _repo.GetGardenMembers(ctx.System.Id).ToListAsync();

            if (members == null || !members.Any())
                throw new CWError("Your system has no members! Please create at least one member before using this command.");

            var randInt = randGen.Next(members.Count);
            await ctx.Reply(embed: await _embeds.CreateMemberEmbed(ctx.System, members[randInt], ctx.Guild));
        }
    }
}