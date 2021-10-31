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
            var existingChao = await _repo.GetChaoByName(ctx.Garden.Id, newName);
            if (existingChao != null && existingChao.Id != target.Id)
            {
                var msg = $"{Emojis.Warn} You already have a chao in your system with the name \"{existingChao.Name}\" (`{existingChao.Id}`). Do you want to rename this chao to that name too?";
                if (!await ctx.PromptYesNo(msg, "Rename")) throw new CWError("Chao renaming cancelled.");
            }

            // Rename the chao
            var patch = new ChaoPatch { Name = Partial<string>.Present(newName) };
            await _repo.UpdateChao(target.Id, patch);

            await ctx.Reply($"{Emojis.Success} Chao renamed.");
        }

        public async Task Delete(Context ctx, Core.Chao target)
        {
            ctx.CheckGarden().CheckOwnChao(target);

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to send \"{target.Name}\" away? If so, reply to this message with the chao's ID (`{target.Id}`). __***This cannot be undone!***__");
            if (!await ctx.ConfirmWithReply(target.Id.ToString())) throw Errors.ChaoDeleteCancelled;

            await _repo.DeleteChao(target.Id);

            await ctx.Reply($"{Emojis.Success} {target.Name} was sent away. {target.Name} will live a happy life in the forest.");
        }
    }
}