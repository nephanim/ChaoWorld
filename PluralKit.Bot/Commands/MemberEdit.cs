using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Net.Http;

using Myriad.Builders;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class MemberEdit
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public MemberEdit(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task Name(Context ctx, Chao target)
        {
            ctx.CheckGarden().CheckOwnMember(target);

            var newName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a new name for the chao.");

            // Hard name length cap
            if (newName.Length > Limits.MaxMemberNameLength)
                throw Errors.StringTooLongError("Member name", newName.Length, Limits.MaxMemberNameLength);

            // Warn if there's already a chao by this name
            var existingMember = await _repo.GetMemberByName(ctx.System.Id, newName);
            if (existingMember != null && existingMember.Id != target.Id)
            {
                var msg = $"{Emojis.Warn} You already have a chao in your system with the name \"{existingMember.Name}\" (`{existingMember.Hid}`). Do you want to rename this chao to that name too?";
                if (!await ctx.PromptYesNo(msg, "Rename")) throw new CWError("Member renaming cancelled.");
            }

            // Rename the chao
            var patch = new GardenPatch { Name = Partial<string>.Present(newName) };
            await _repo.UpdateMember(target.Id, patch);

            await ctx.Reply($"{Emojis.Success} Member renamed.");
            if (newName.Contains(" ")) await ctx.Reply($"{Emojis.Note} Note that this chao's name now contains spaces. You will need to surround it with \"double quotes\" when using commands referring to it.");
            if (target.DisplayName != null) await ctx.Reply($"{Emojis.Note} Note that this chao has a display name set ({target.DisplayName}), and will be proxied using that name instead.");

            if (ctx.Guild != null)
            {
                var chaoGuildConfig = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);
                if (chaoGuildConfig.DisplayName != null)
                    await ctx.Reply($"{Emojis.Note} Note that this chao has a server name set ({chaoGuildConfig.DisplayName}) in this server ({ctx.Guild.Name}), and will be proxied using that name here.");
            }
        }

        public async Task Delete(Context ctx, Chao target)
        {
            ctx.CheckGarden().CheckOwnMember(target);

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete \"{target.Name}\"? If so, reply to this message with the chao's ID (`{target.Hid}`). __***This cannot be undone!***__");
            if (!await ctx.ConfirmWithReply(target.Hid)) throw Errors.MemberDeleteCancelled;

            await _repo.DeleteMember(target.Id);

            await ctx.Reply($"{Emojis.Success} Member deleted.");
        }
    }
}