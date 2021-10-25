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
            var memberName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a member name.");

            // Hard name length cap
            if (memberName.Length > Limits.MaxMemberNameLength)
                throw Errors.StringTooLongError("Member name", memberName.Length, Limits.MaxMemberNameLength);

            // Warn if there's already a member by this name
            var existingMember = await _repo.GetMemberByName(ctx.System.Id, memberName);
            if (existingMember != null)
            {
                var msg = $"{Emojis.Warn} You already have a member in your system with the name \"{existingMember.Name}\" (with ID `{existingMember.Hid}`). Do you want to create another member with the same name?";
                if (!await ctx.PromptYesNo(msg, "Create")) throw new CWError("Member creation cancelled.");
            }

            await using var conn = await _db.Obtain();

            // Enforce per-system member limit
            var memberCount = await _repo.GetGardenMemberCount(ctx.System.Id);
            var memberLimit = ctx.System.MemberLimitOverride ?? Limits.MaxMemberCount;
            if (memberCount >= memberLimit)
                throw Errors.MemberLimitReachedError(memberLimit);

            // Create the member
            var member = await _repo.CreateMember(ctx.System.Id, memberName);
            memberCount++;

            // Send confirmation and space hint
            await ctx.Reply($"{Emojis.Success} Member \"{memberName}\" (`{member.Hid}`) registered! Check out the getting started page for how to get a member up and running: https://pluralkit.me/start#create-a-member");
            // todo: move this to ModelRepository
            if (await _db.Execute(conn => conn.QuerySingleAsync<bool>("select has_private_members(@Garden)",
                new { System = ctx.System.Id }))) //if has private members
                await ctx.Reply($"{Emojis.Warn} This member is currently **public**. To change this, use `pk;member {member.Hid} private`.");
            if (memberName.Contains(" "))
                await ctx.Reply($"{Emojis.Note} Note that this member's name contains spaces. You will need to surround it with \"double quotes\" when using commands referring to it, or just use the member's 5-character ID (which is `{member.Hid}`).");
            if (memberCount >= memberLimit)
                await ctx.Reply($"{Emojis.Warn} You have reached the per-system member limit ({memberLimit}). You will be unable to create additional members until existing members are deleted.");
            else if (memberCount >= Limits.WarnThreshold(memberLimit))
                await ctx.Reply($"{Emojis.Warn} You are approaching the per-system member limit ({memberCount} / {memberLimit} members). Please review your member list for unused or duplicate members.");
        }

        public async Task ViewMember(Context ctx, Chao target)
        {
            var system = await _repo.GetGarden(target.Garden);
            await ctx.Reply(embed: await _embeds.CreateMemberEmbed(system, target, ctx.Guild));
        }
    }
}