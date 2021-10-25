using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Myriad.Builders;
using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Rest;
using Myriad.Rest.Exceptions;
using Myriad.Types;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Checks
    {
        private readonly DiscordApiClient _rest;
        private readonly Bot _bot;
        private readonly IDiscordCache _cache;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly BotConfig _botConfig;
        private readonly ProxyMatcher _matcher;

        public Checks(DiscordApiClient rest, Bot bot, IDiscordCache cache, IDatabase db, ModelRepository repo,
                      BotConfig botConfig, ProxyMatcher matcher)
        {
            _rest = rest;
            _bot = bot;
            _cache = cache;
            _db = db;
            _repo = repo;
            _botConfig = botConfig;
            _matcher = matcher;
        }

        private readonly PermissionSet[] requiredPermissions = new[]
            {
                PermissionSet.ViewChannel,
                PermissionSet.SendMessages,
                PermissionSet.AddReactions,
                PermissionSet.AttachFiles,
                PermissionSet.EmbedLinks,
                PermissionSet.ManageMessages,
                PermissionSet.ManageWebhooks
            };

        public async Task PermCheckGuild(Context ctx)
        {
            Guild guild;
            GuildMemberPartial senderGuildUser = null;

            if (ctx.Guild != null && !ctx.HasNext())
            {
                guild = ctx.Guild;
                senderGuildUser = ctx.Member;
            }
            else
            {
                var guildIdStr = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a server ID or run this command in a server.");
                if (!ulong.TryParse(guildIdStr, out var guildId))
                    throw new CWSyntaxError($"Could not parse {guildIdStr.AsCode()} as an ID.");

                try
                {
                    guild = await _rest.GetGuild(guildId);
                }
                catch (Myriad.Rest.Exceptions.ForbiddenException)
                {
                    throw Errors.GuildNotFound(guildId);
                }

                if (guild != null)
                    senderGuildUser = await _rest.GetGuildMember(guildId, ctx.Author.Id);
                if (guild == null || senderGuildUser == null)
                    throw Errors.GuildNotFound(guildId);
            }

            // Loop through every channel and group them by sets of permissions missing
            var permissionsMissing = new Dictionary<ulong, List<Channel>>();
            var hiddenChannels = false;
            var missingEmojiPermissions = false;
            foreach (var channel in await _rest.GetGuildChannels(guild.Id))
            {
                var botPermissions = _bot.PermissionsIn(channel.Id);
                var webhookPermissions = _cache.EveryonePermissions(channel);
                var userPermissions = PermissionExtensions.PermissionsFor(guild, channel, ctx.Author.Id, senderGuildUser);

                if ((userPermissions & PermissionSet.ViewChannel) == 0)
                {
                    // If the user can't see this channel, don't calculate permissions for it
                    // (to prevent info-leaking, mostly)
                    // Instead, show the user that some channels got ignored (so they don't get confused)
                    hiddenChannels = true;
                    continue;
                }

                // We use a bitfield so we can set individual permission bits in the loop
                // TODO: Rewrite with proper bitfield math
                ulong missingPermissionField = 0;

                foreach (var requiredPermission in requiredPermissions)
                    if ((botPermissions & requiredPermission) == 0)
                        missingPermissionField |= (ulong)requiredPermission;

                if ((webhookPermissions & PermissionSet.UseExternalEmojis) == 0)
                {
                    missingPermissionField |= (ulong)PermissionSet.UseExternalEmojis;
                    missingEmojiPermissions = true;
                }

                // If we're not missing any permissions, don't bother adding it to the dict
                // This means we can check if the dict is empty to see if all channels are proxyable
                if (missingPermissionField != 0)
                {
                    permissionsMissing.TryAdd(missingPermissionField, new List<Channel>());
                    permissionsMissing[missingPermissionField].Add(channel);
                }
            }

            // Generate the output embed
            var eb = new EmbedBuilder()
                .Title($"Permission check for **{guild.Name}**");

            if (permissionsMissing.Count == 0)
            {
                eb.Description($"No errors found, all channels proxyable :)").Color(DiscordUtils.Green);
            }
            else
            {
                foreach (var (missingPermissionField, channels) in permissionsMissing)
                {
                    // Each missing permission field can have multiple missing channels
                    // so we extract them all and generate a comma-separated list
                    var missingPermissionNames = ((PermissionSet)missingPermissionField).ToPermissionString();

                    var channelsList = string.Join("\n", channels
                        .OrderBy(c => c.Position)
                        .Select(c => $"#{c.Name}"));
                    eb.Field(new($"Missing *{missingPermissionNames}*", channelsList.Truncate(1000)));
                    eb.Color(DiscordUtils.Red);
                }
            }

            var footer = "";
            if (hiddenChannels)
                footer += "Some channels were ignored as you do not have view access to them.";
            if (missingEmojiPermissions)
            {
                if (hiddenChannels) footer += " | ";
                footer += "Use External Emojis permissions must be granted to the @everyone role / Default Permissions.";
            }

            if (footer.Length > 0)
                eb.Footer(new(footer));

            // Send! :)
            await ctx.Reply(embed: eb.Build());
        }

        public async Task PermCheckChannel(Context ctx)
        {
            if (!ctx.HasNext())
                throw new CWSyntaxError("You need to specify a channel.");

            var error = "Channel not found or you do not have permissions to access it.";

            var channel = await ctx.MatchChannel();
            if (channel == null || channel.GuildId == null)
                throw new CWError(error);

            var guild = _cache.GetGuild(channel.GuildId.Value);
            if (guild == null)
                throw new CWError(error);

            var guildMember = await _rest.GetGuildMember(channel.GuildId.Value, ctx.Author.Id);
            if (guildMember == null)
                throw new CWError(error);



            var botPermissions = _bot.PermissionsIn(channel.Id);
            var webhookPermissions = _cache.EveryonePermissions(channel);
            var userPermissions = PermissionExtensions.PermissionsFor(guild, channel, ctx.Author.Id, guildMember);

            if ((userPermissions & PermissionSet.ViewChannel) == 0)
                throw new CWError(error);



            // We use a bitfield so we can set individual permission bits
            ulong missingPermissions = 0;

            foreach (var requiredPermission in requiredPermissions)
                if ((botPermissions & requiredPermission) == 0)
                    missingPermissions |= (ulong)requiredPermission;

            if ((webhookPermissions & PermissionSet.UseExternalEmojis) == 0)
                missingPermissions |= (ulong)PermissionSet.UseExternalEmojis;

            // Generate the output embed
            var eb = new EmbedBuilder()
                .Title($"Permission check for **{channel.Name}**");

            if (missingPermissions == 0)
                eb.Description("No issues found, channel is proxyable :)");
            else
            {
                var missing = "";

                foreach (var permission in requiredPermissions)
                    if (((ulong)permission & missingPermissions) == (ulong)permission)
                        missing += $"\n- **{permission.ToPermissionString()}**";

                if (((ulong)PermissionSet.UseExternalEmojis & missingPermissions) == (ulong)PermissionSet.UseExternalEmojis)
                    missing += $"\n- **{PermissionSet.UseExternalEmojis.ToPermissionString()}**";

                eb.Description($"Missing permissions:\n{missing}");
            }

            await ctx.Reply(embed: eb.Build());
        }
    }
}