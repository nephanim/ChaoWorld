using System.Linq;
using System.Threading.Tasks;

using Myriad.Extensions;
using Myriad.Rest.Types;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class SystemLink
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public SystemLink(IDatabase db, ModelRepository repo)
        {
            _db = db;
            _repo = repo;
        }

        public async Task LinkSystem(Context ctx)
        {
            ctx.CheckGarden();

            var account = await ctx.MatchUser() ?? throw new CWSyntaxError("You must pass an account to link with (either ID or @mention).");
            var accountIds = await _repo.GetGardenAccounts(ctx.Garden.Id);
            if (accountIds.Contains(account.Id))
                throw Errors.AccountAlreadyLinked;

            var existingAccount = await _repo.GetGardenByAccount(account.Id);
            if (existingAccount != null)
                throw Errors.AccountInOtherSystem(existingAccount);

            var msg = $"{account.Mention()}, please confirm the link.";
            if (!await ctx.PromptYesNo(msg, "Confirm", user: account, matchFlag: false)) throw Errors.ChaoLinkCancelled;
            await _repo.AddAccount(ctx.Garden.Id, account.Id);
            await ctx.Reply($"{Emojis.Success} Account linked to system.");
        }

        public async Task UnlinkAccount(Context ctx)
        {
            ctx.CheckGarden();

            ulong id;
            if (!ctx.MatchUserRaw(out id))
                throw new CWSyntaxError("You must pass an account to link with (either ID or @mention).");

            var accountIds = (await _repo.GetGardenAccounts(ctx.Garden.Id)).ToList();
            if (!accountIds.Contains(id)) throw Errors.AccountNotLinked;
            if (accountIds.Count == 1) throw Errors.UnlinkingLastAccount;

            var msg = $"Are you sure you want to unlink <@{id}> from your system?";
            if (!await ctx.PromptYesNo(msg, "Unlink")) throw Errors.ChaoUnlinkCancelled;

            await _repo.RemoveAccount(ctx.Garden.Id, id);
            await ctx.Reply($"{Emojis.Success} Account unlinked.");
        }
    }
}