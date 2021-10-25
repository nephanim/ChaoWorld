using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Net.Http;

using Myriad.Builders;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class ChaoEdit
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public ChaoEdit(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task Name(Context ctx, Core.Chao target)
        {
            ctx.CheckGarden().CheckOwnChao(target);

            var newName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a new name for the chao.");

            // Hard name length cap
            if (newName.Length > Limits.MaxChaoNameLength)
                throw Errors.StringTooLongError("Chao name", newName.Length, Limits.MaxChaoNameLength);

            // Warn if there's already a chao by this name
            var existingChao = await _repo.GetChaoByName(ctx.System.Id, newName);
            if (existingChao != null && existingChao.Id != target.Id)
            {
                var msg = $"{Emojis.Warn} You already have a chao in your system with the name \"{existingChao.Name}\" (`{existingChao.Hid}`). Do you want to rename this chao to that name too?";
                if (!await ctx.PromptYesNo(msg, "Rename")) throw new CWError("Chao renaming cancelled.");
            }

            // Rename the chao
            var patch = new GardenPatch { Name = Partial<string>.Present(newName) };
            await _repo.UpdateChao(target.Id, patch);

            await ctx.Reply($"{Emojis.Success} Chao renamed.");
            if (newName.Contains(" ")) await ctx.Reply($"{Emojis.Note} Note that this chao's name now contains spaces. You will need to surround it with \"double quotes\" when using commands referring to it.");
            if (target.DisplayName != null) await ctx.Reply($"{Emojis.Note} Note that this chao has a display name set ({target.DisplayName}), and will be proxied using that name instead.");

            if (ctx.Guild != null)
            {
                var chaoGuildConfig = await _repo.GetChaoGuild(ctx.Guild.Id, target.Id);
                if (chaoGuildConfig.DisplayName != null)
                    await ctx.Reply($"{Emojis.Note} Note that this chao has a server name set ({chaoGuildConfig.DisplayName}) in this server ({ctx.Guild.Name}), and will be proxied using that name here.");
            }
        }

        public async Task Delete(Context ctx, Core.Chao target)
        {
            ctx.CheckGarden().CheckOwnChao(target);

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete \"{target.Name}\"? If so, reply to this message with the chao's ID (`{target.Id}`). __***This cannot be undone!***__");
            if (!await ctx.ConfirmWithReply(target.Hid)) throw Errors.ChaoDeleteCancelled;

            await _repo.DeleteChao(target.Id);

            await ctx.Reply($"{Emojis.Success} Chao deleted.");
        }
    }
}