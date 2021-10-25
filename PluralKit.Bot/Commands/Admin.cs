using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Admin
    {
        private readonly BotConfig _botConfig;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public Admin(BotConfig botConfig, IDatabase db, ModelRepository repo)
        {
            _botConfig = botConfig;
            _db = db;
            _repo = repo;
        }

        public async Task UpdateSystemId(Context ctx)
        {
            ctx.AssertBotAdmin();

            var target = await ctx.MatchSystem();
            if (target == null)
                throw new PKError("Unknown system.");

            var newHid = ctx.PopArgument();
            if (!Regex.IsMatch(newHid, "^[a-z]{5}$"))
                throw new PKError($"Invalid new system ID `{newHid}`.");

            var existingSystem = await _repo.GetSystemByHid(newHid);
            if (existingSystem != null)
                throw new PKError($"Another system already exists with ID `{newHid}`.");

            if (!await ctx.PromptYesNo($"Change system ID of `{target.Hid}` to `{newHid}`?", "Change"))
                throw new PKError("ID change cancelled.");

            await _repo.UpdateSystem(target.Id, new() { Hid = newHid });
            await ctx.Reply($"{Emojis.Success} Garden ID updated (`{target.Hid}` -> `{newHid}`).");
        }

        public async Task UpdateMemberId(Context ctx)
        {
            ctx.AssertBotAdmin();

            var target = await ctx.MatchMember();
            if (target == null)
                throw new PKError("Unknown member.");

            var newHid = ctx.PopArgument();
            if (!Regex.IsMatch(newHid, "^[a-z]{5}$"))
                throw new PKError($"Invalid new member ID `{newHid}`.");

            var existingMember = await _repo.GetMemberByHid(newHid);
            if (existingMember != null)
                throw new PKError($"Another member already exists with ID `{newHid}`.");

            if (!await ctx.PromptYesNo($"Change member ID of **{target.NameFor(LookupContext.ByNonOwner)}** (`{target.Hid}`) to `{newHid}`?", "Change"))
                throw new PKError("ID change cancelled.");

            await _repo.UpdateMember(target.Id, new() { Hid = newHid });
            await ctx.Reply($"{Emojis.Success} Member ID updated (`{target.Hid}` -> `{newHid}`).");
        }

        public async Task UpdateGroupId(Context ctx)
        {
            ctx.AssertBotAdmin();

            var target = await ctx.MatchGroup();
            if (target == null)
                throw new PKError("Unknown group.");

            var newHid = ctx.PopArgument();
            if (!Regex.IsMatch(newHid, "^[a-z]{5}$"))
                throw new PKError($"Invalid new group ID `{newHid}`.");

            var existingGroup = await _repo.GetGroupByHid(newHid);
            if (existingGroup != null)
                throw new PKError($"Another group already exists with ID `{newHid}`.");

            if (!await ctx.PromptYesNo($"Change group ID of **{target.Name}** (`{target.Hid}`) to `{newHid}`?", "Change"))
                throw new PKError("ID change cancelled.");

            await _repo.UpdateGroup(target.Id, new() { Hid = newHid });
            await ctx.Reply($"{Emojis.Success} Group ID updated (`{target.Hid}` -> `{newHid}`).");
        }

        public async Task SystemMemberLimit(Context ctx)
        {
            ctx.AssertBotAdmin();

            var target = await ctx.MatchSystem();
            if (target == null)
                throw new PKError("Unknown system.");

            var currentLimit = target.MemberLimitOverride ?? Limits.MaxMemberCount;
            if (!ctx.HasNext())
            {
                await ctx.Reply($"Current member limit is **{currentLimit}** members.");
                return;
            }

            var newLimitStr = ctx.PopArgument();
            if (!int.TryParse(newLimitStr, out var newLimit))
                throw new PKError($"Couldn't parse `{newLimitStr}` as number.");

            if (!await ctx.PromptYesNo($"Update member limit from **{currentLimit}** to **{newLimit}**?", "Update"))
                throw new PKError("Member limit change cancelled.");

            await _repo.UpdateSystem(target.Id, new() { MemberLimitOverride = newLimit });
            await ctx.Reply($"{Emojis.Success} Member limit updated.");
        }

        public async Task SystemGroupLimit(Context ctx)
        {
            ctx.AssertBotAdmin();

            var target = await ctx.MatchSystem();
            if (target == null)
                throw new PKError("Unknown system.");

            var currentLimit = target.GroupLimitOverride ?? Limits.MaxGroupCount;
            if (!ctx.HasNext())
            {
                await ctx.Reply($"Current group limit is **{currentLimit}** groups.");
                return;
            }

            var newLimitStr = ctx.PopArgument();
            if (!int.TryParse(newLimitStr, out var newLimit))
                throw new PKError($"Couldn't parse `{newLimitStr}` as number.");

            if (!await ctx.PromptYesNo($"Update group limit from **{currentLimit}** to **{newLimit}**?", "Update"))
                throw new PKError("Group limit change cancelled.");

            await _repo.UpdateSystem(target.Id, new() { GroupLimitOverride = newLimit });
            await ctx.Reply($"{Emojis.Success} Group limit updated.");
        }
    }
}