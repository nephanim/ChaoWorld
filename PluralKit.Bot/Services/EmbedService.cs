using System;
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

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class EmbedService
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IDiscordCache _cache;
        private readonly DiscordApiClient _rest;

        public EmbedService(IDatabase db, ModelRepository repo, IDiscordCache cache, DiscordApiClient rest)
        {
            _db = db;
            _repo = repo;
            _cache = cache;
            _rest = rest;
        }

        private Task<(ulong Id, User? User)[]> GetUsers(IEnumerable<ulong> ids)
        {
            async Task<(ulong Id, User? User)> Inner(ulong id)
            {
                var user = await _cache.GetOrFetchUser(_rest, id);
                return (id, user);
            }

            return Task.WhenAll(ids.Select(Inner));
        }

        public async Task<Embed> CreateSystemEmbed(Context cctx, Core.Garden system)
        {

            // Fetch/render info for all accounts simultaneously
            var accounts = await _repo.GetGardenAccounts(system.Id);
            var users = (await GetUsers(accounts)).Select(x => x.User?.NameAndMention() ?? $"(deleted account {x.Id})");

            var chaoCount = await _repo.GetGardenMemberCount(system.Id);

            uint color;
            try
            {
                color = system.Color?.ToDiscordColor() ?? DiscordUtils.Gray;
            }
            catch (ArgumentException)
            {
                // There's no API for system colors yet, but defaulting to a blank color in advance can't be a bad idea
                color = DiscordUtils.Gray;
            }

            var eb = new EmbedBuilder()
                .Title(system.Name)
                .Thumbnail(new(system.AvatarUrl.TryGetCleanCdnUrl()))
                .Footer(new($"Garden ID: {system.Hid} | Created on {system.Created.FormatZoned(system)}"))
                .Color(color);

            eb.Image(new(system.BannerImage));

            if (system.Tag != null)
                eb.Field(new("Tag", system.Tag.EscapeMarkdown(), true));

            if (cctx.Guild != null)
            {
                var guildSettings = await _repo.GetSystemGuild(cctx.Guild.Id, system.Id);

                if (guildSettings.Tag != null && guildSettings.TagEnabled)
                    eb.Field(new($"Tag (in server '{cctx.Guild.Name}')", guildSettings.Tag
                        .EscapeMarkdown(), true));

                if (!guildSettings.TagEnabled)
                    eb.Field(new($"Tag (in server '{cctx.Guild.Name}')", "*(tag is disabled in this server)*"));
            }

            if (!system.Color.EmptyOrNull()) eb.Field(new("Color", $"#{system.Color}", true));

            eb.Field(new("Linked accounts", string.Join("\n", users).Truncate(1000), true));

            if (chaoCount > 0)
                eb.Field(new($"Chao ({chaoCount})", $"(see `pk;system {system.Hid} list` or `pk;system {system.Hid} list full`)", true));
            else
                eb.Field(new($"Chao ({chaoCount})", "Add one with `pk;chao new`!", true));

            if (system.Description is { } desc)
                eb.Field(new("Description", desc.NormalizeLineEndSpacing().Truncate(1024), false));

            return eb.Build();
        }

        public Embed CreateLoggedMessageEmbed(Message triggerMessage, Message proxiedMessage, string systemHid, Chao chao, string channelName, string oldContent = null)
        {
            // TODO: pronouns in ?-reacted response using this card
            var timestamp = DiscordUtils.SnowflakeToInstant(proxiedMessage.Id);
            var name = proxiedMessage.Author.Username;
            // sometimes Discord will just... not return the avatar hash with webhook messages
            var embed = new EmbedBuilder()
                .Description(proxiedMessage.Content?.NormalizeLineEndSpacing())
                .Footer(new($"Garden ID: {systemHid} | Member ID: {chao.Hid} | Sender: {triggerMessage.Author.Username}#{triggerMessage.Author.Discriminator} ({triggerMessage.Author.Id}) | Message ID: {proxiedMessage.Id} | Original Message ID: {triggerMessage.Id}"))
                .Timestamp(timestamp.ToDateTimeOffset().ToString("O"));

            if (oldContent != null)
                embed.Field(new("Old message", oldContent?.NormalizeLineEndSpacing().Truncate(1000)));

            return embed.Build();
        }

        public async Task<Embed> CreateMemberEmbed(Core.Garden system, Chao chao, Guild guild)
        {
            var name = chao.Name;
            if (system.Name != null) name = $"{name} ({system.Name})";

            uint color;
            try
            {
                color = chao.Color?.ToDiscordColor() ?? DiscordUtils.Gray;
            }
            catch (ArgumentException)
            {
                // Bad API use can cause an invalid color string
                // this is now fixed in the API, but might still have some remnants in the database
                // so we just default to a blank color, yolo
                color = DiscordUtils.Gray;
            }

            var guildSettings = guild != null ? await _repo.GetMemberGuild(guild.Id, chao.Id) : null;
            var guildDisplayName = guildSettings?.DisplayName;
            var avatar = guildSettings?.AvatarUrl;

            var eb = new EmbedBuilder()
                // TODO: add URL of website when that's up
                .Author(new(name, IconUrl: avatar.TryGetCleanCdnUrl()))
                // .WithColor(chao.ColorPrivacy.CanAccess(ctx) ? color : DiscordUtils.Gray)
                .Color(color)
                .Footer(new(
                    $"Garden ID: {system.Hid} | Member ID: {chao.Hid} {$"| Created on {chao.Created.FormatZoned(system)}"}"));

            eb.Image(new(chao.BannerImage));

            if (avatar != null) eb.Thumbnail(new(avatar.TryGetCleanCdnUrl()));

            if (!chao.DisplayName.EmptyOrNull()) eb.Field(new("Display Name", chao.DisplayName.Truncate(1024), true));
            if (guild != null && guildDisplayName != null) eb.Field(new($"Server Nickname (for {guild.Name})", guildDisplayName.Truncate(1024), true));
            if (chao.HasProxyTags) eb.Field(new("Proxy Tags", chao.ProxyTagsString("\n").Truncate(1024), true));
            // --- For when this gets added to the chao object itself or however they get added
            // if (chao.LastMessage != null && chao.MetadataPrivacy.CanAccess(ctx)) eb.AddField("Last message:" FormatTimestamp(DiscordUtils.SnowflakeToInstant(m.LastMessage.Value)));
            // if (chao.LastSwitchTime != null && m.MetadataPrivacy.CanAccess(ctx)) eb.AddField("Last switched in:", FormatTimestamp(chao.LastSwitchTime.Value));
            // if (!chao.Color.EmptyOrNull() && chao.ColorPrivacy.CanAccess(ctx)) eb.AddField("Color", $"#{chao.Color}", true);
            if (!chao.Color.EmptyOrNull()) eb.Field(new("Color", $"#{chao.Color}", true));

            if (chao.Description is { } desc)
                eb.Field(new("Description", chao.Description.NormalizeLineEndSpacing(), false));

            return eb.Build();
        }

        public async Task<Embed> CreateMessageInfoEmbed(FullMessage msg)
        {
            var channel = await _cache.GetOrFetchChannel(_rest, msg.Message.Channel);

            Message serverMsg = null;
            try
            {
                serverMsg = await _rest.GetMessage(msg.Message.Channel, msg.Message.Mid);
            }
            catch (ForbiddenException)
            {
                // no permission, couldn't fetch, oh well
            }

            // Need this whole dance to handle cases where:
            // - the user is deleted (userInfo == null)
            // - the bot's no longer in the server we're querying (channel == null)
            // - the chao is no longer in the server we're querying (chaoInfo == null)
            // TODO: optimize ordering here a bit with new cache impl; and figure what happens if bot leaves server -> channel still cached -> hits this bit and 401s?
            GuildMemberPartial chaoInfo = null;
            User userInfo = null;
            if (channel != null)
            {
                GuildMember chao = null;
                try
                {
                    chao = await _rest.GetGuildMember(channel.GuildId!.Value, msg.Message.Sender);
                }
                catch (ForbiddenException)
                {
                    // no permission, couldn't fetch, oh well
                }

                if (chao != null)
                    // Don't do an extra request if we already have this info from the chao lookup
                    userInfo = chao.User;
                chaoInfo = chao;
            }

            if (userInfo == null)
                userInfo = await _cache.GetOrFetchUser(_rest, msg.Message.Sender);

            // Calculate string displayed under "Sent by"
            string userStr;
            if (chaoInfo != null && chaoInfo.Nick != null)
                userStr = $"**Username:** {userInfo.NameAndMention()}\n**Nickname:** {chaoInfo.Nick}";
            else if (userInfo != null) userStr = userInfo.NameAndMention();
            else userStr = $"*(deleted user {msg.Message.Sender})*";

            // Put it all together
            var eb = new EmbedBuilder()
                .Description(serverMsg?.Content?.NormalizeLineEndSpacing() ?? "*(message contents deleted or inaccessible)*")
                .Image(new(serverMsg?.Attachments?.FirstOrDefault()?.Url))
                .Field(new("Garden",
                    msg.System.Name != null ? $"{msg.System.Name} (`{msg.System.Hid}`)" : $"`{msg.System.Hid}`", true))
                .Field(new("Member", $"{msg.Member.Name} (`{msg.Member.Hid}`)", true))
                .Field(new("Sent by", userStr, true))
                .Timestamp(DiscordUtils.SnowflakeToInstant(msg.Message.Mid).ToDateTimeOffset().ToString("O"));

            var roles = chaoInfo?.Roles?.ToList();
            if (roles != null && roles.Count > 0)
            {
                // TODO: what if role isn't in cache? figure out a fallback
                var rolesString = string.Join(", ", roles
                    .Select(id => _cache.GetRole(id))
                    .OrderByDescending(role => role.Position)
                    .Select(role => role.Name));
                eb.Field(new($"Account roles ({roles.Count})", rolesString.Truncate(1024)));
            }

            return eb.Build();
        }
    }
}