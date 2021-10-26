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
            if (ctx.Chao == null) throw Errors.NoGardenError;
            var chaoName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a chao name.");

            // Hard name length cap
            if (chaoName.Length > Limits.MaxChaoNameLength)
                throw Errors.StringTooLongError("Chao name", chaoName.Length, Limits.MaxChaoNameLength);

            // Warn if there's already a chao by this name
            var existingChao = await _repo.GetChaoByName(ctx.Garden.Id, chaoName);
            if (existingChao != null)
            {
                var msg = $"{Emojis.Warn} You already have a chao in your garden with the name \"{existingChao.Name}\" (with ID `{existingChao.Id}`). Do you want to create another chao with the same name?";
                if (!await ctx.PromptYesNo(msg, "Create")) throw new CWError("Chao creation cancelled.");
            }

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

            // Send confirmation and space hint
            await ctx.Reply($"{Emojis.Success} Chao \"{chaoName}\" (`{chao.Id}`) registered!");

            if (chaoCount >= chaoLimit)
                await ctx.Reply($"{Emojis.Warn} You have reached the per-garden chao limit ({chaoLimit}). You will be unable to obtain additional chao until existing chao are deleted.");
            else if (chaoCount >= Limits.WarnThreshold(chaoLimit))
                await ctx.Reply($"{Emojis.Warn} You are approaching the per-garden chao limit ({chaoCount} / {chaoLimit} chao). Please review your chao list for unused or duplicate chao.");
        }

        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var system = await _repo.GetGarden(target.GardenId);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(system, target, ctx.Guild));
        }
    }
}