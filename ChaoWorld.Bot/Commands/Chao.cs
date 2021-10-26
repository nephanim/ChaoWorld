using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Linq;

using Dapper;

using Myriad.Builders;

using Newtonsoft.Json.Linq;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Chao
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Chao(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task NewChao(Context ctx)
        {
            if (ctx.Member == null) throw Errors.NoGardenError;
            await using var conn = await _db.Obtain();

            // Enforce per-system chao limit
            var chaoCount = await _repo.GetGardenChaoCount(ctx.Garden.Id);
            var chaoLimit = ctx.Garden.ChaoLimitOverride ?? Limits.MaxChaoCount;
            if (chaoCount >= chaoLimit)
                throw Errors.ChaoLimitReachedError(chaoLimit);

            // Create the chao
            var chaoTemplate = new Core.Chao();
            chaoTemplate.Initialize(isStarterChao: false);
            var chao = await _repo.CreateChao(ctx.Garden.Id, chaoTemplate);
            chaoCount++;

            // Send confirmation
            await ctx.Reply($"{Emojis.Success} Chao {chao.Id} registered!");
        }

        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var system = await _repo.GetGarden(target.GardenId);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(system, target, ctx.Guild));
        }
    }
}