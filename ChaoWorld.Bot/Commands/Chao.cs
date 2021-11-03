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
            if (ctx.Garden == null) throw Errors.NoGardenError;
            await using var conn = await _db.Obtain();

            // Create the chao
            var chaoTemplate = new Core.Chao();
            chaoTemplate.Initialize();
            var chao = await _repo.CreateChao(ctx.Garden.Id, chaoTemplate);

            // Send confirmation
            await ctx.Reply($"{Emojis.Success} Chao {chao.Id} registered!");
        }

        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var system = await _repo.GetGarden(target.GardenId.Value);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(system, target));
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

        /*private async Task<Affection> GiveChaoAffection(Context ctx, Core.Chao target)
        {

        }*/
    }
}