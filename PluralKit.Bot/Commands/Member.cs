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
    public class Member
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Member(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task NewMember(Context ctx)
        {
            if (ctx.System == null) throw Errors.NoGardenError;
            var chaoName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a chao name.");

            // Hard name length cap
            if (chaoName.Length > Limits.MaxMemberNameLength)
                throw Errors.StringTooLongError("Member name", chaoName.Length, Limits.MaxMemberNameLength);

            // Warn if there's already a chao by this name
            var existingMember = await _repo.GetMemberByName(ctx.System.Id, chaoName);
            if (existingMember != null)
            {
                var msg = $"{Emojis.Warn} You already have a chao in your system with the name \"{existingMember.Name}\" (with ID `{existingMember.Hid}`). Do you want to create another chao with the same name?";
                if (!await ctx.PromptYesNo(msg, "Create")) throw new CWError("Member creation cancelled.");
            }

            await using var conn = await _db.Obtain();

            // Enforce per-system chao limit
            var chaoCount = await _repo.GetGardenMemberCount(ctx.System.Id);
            var chaoLimit = ctx.System.MemberLimitOverride ?? Limits.MaxMemberCount;
            if (chaoCount >= chaoLimit)
                throw Errors.MemberLimitReachedError(chaoLimit);

            // Create the chao
            var chao = await _repo.CreateMember(ctx.System.Id, chaoName);
            chaoCount++;

            // Send confirmation and space hint
            await ctx.Reply($"{Emojis.Success} Member \"{chaoName}\" (`{chao.Hid}`) registered! Check out the getting started page for how to get a chao up and running: https://pluralkit.me/start#create-a-chao");
            // todo: move this to ModelRepository
            if (await _db.Execute(conn => conn.QuerySingleAsync<bool>("select has_private_chao(@Garden)",
                new { System = ctx.System.Id }))) //if has private chao
                await ctx.Reply($"{Emojis.Warn} This chao is currently **public**. To change this, use `pk;chao {chao.Hid} private`.");
            if (chaoName.Contains(" "))
                await ctx.Reply($"{Emojis.Note} Note that this chao's name contains spaces. You will need to surround it with \"double quotes\" when using commands referring to it, or just use the chao's 5-character ID (which is `{chao.Hid}`).");
            if (chaoCount >= chaoLimit)
                await ctx.Reply($"{Emojis.Warn} You have reached the per-system chao limit ({chaoLimit}). You will be unable to create additional chao until existing chao are deleted.");
            else if (chaoCount >= Limits.WarnThreshold(chaoLimit))
                await ctx.Reply($"{Emojis.Warn} You are approaching the per-system chao limit ({chaoCount} / {chaoLimit} chao). Please review your chao list for unused or duplicate chao.");
        }

        public async Task ViewMember(Context ctx, Chao target)
        {
            var system = await _repo.GetGarden(target.Garden);
            await ctx.Reply(embed: await _embeds.CreateMemberEmbed(system, target, ctx.Guild));
        }
    }
}