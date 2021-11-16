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

        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var garden = await _repo.GetGarden(target.GardenId.Value);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(ctx, garden, target));
        }

        public async Task PetChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} is soothed and smiling.");
        }

        public async Task RockChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} relaxes and coos in your arms.");
        }

        public async Task CuddleChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} falls asleep in your arms.");
        }

        public async Task ChaoRankings(Context ctx)
        {
            await ctx.RenderChaoRankedList(_db);
        }
    }
}