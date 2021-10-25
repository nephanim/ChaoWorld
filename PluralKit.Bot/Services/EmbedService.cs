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

        public async Task<Embed> CreateSystemEmbed(Context cctx, Core.Garden garden)
        {

            // Fetch/render info for all accounts simultaneously
            var accounts = await _repo.GetGardenAccounts(garden.Id);
            var users = (await GetUsers(accounts)).Select(x => x.User?.NameAndMention() ?? $"(deleted account {x.Id})");

            var chaoCount = await _repo.GetGardenChaoCount(garden.Id);

            var eb = new EmbedBuilder()
                .Title($"{cctx.Author.Username}'s Garden")
                .Footer(new($"Garden ID: {garden.Id} | Created on {garden.CreatedOn}"));

            eb.Field(new("Rings", garden.RingBalance.ToString(), true));
            eb.Field(new("Linked Accounts", string.Join("\n", users).Truncate(1000), true));

            if (chaoCount > 0)
                eb.Field(new($"Chao ({chaoCount})", $"(see `!system {garden.Id} list` or `!system {garden.Id} list full`)", true));
            else
                eb.Field(new($"Chao ({chaoCount})", "Add one with `!chao new`!", true));

            return eb.Build();
        }

        public Embed CreateLoggedMessageEmbed(Message triggerMessage, Message proxiedMessage, string systemHid, Core.Chao chao, string channelName, string oldContent = null)
        {
            // TODO: pronouns in ?-reacted response using this card
            var timestamp = DiscordUtils.SnowflakeToInstant(proxiedMessage.Id);
            var name = proxiedMessage.Author.Username;
            // sometimes Discord will just... not return the avatar hash with webhook messages
            var embed = new EmbedBuilder()
                .Description(proxiedMessage.Content?.NormalizeLineEndSpacing())
                .Footer(new($"Garden ID: {systemHid} | Chao ID: {chao.Hid} | Sender: {triggerMessage.Author.Username}#{triggerMessage.Author.Discriminator} ({triggerMessage.Author.Id}) | Message ID: {proxiedMessage.Id} | Original Message ID: {triggerMessage.Id}"))
                .Timestamp(timestamp.ToDateTimeOffset().ToString("O"));

            if (oldContent != null)
                embed.Field(new("Old message", oldContent?.NormalizeLineEndSpacing().Truncate(1000)));

            return embed.Build();
        }

        public async Task<Embed> CreateChaoEmbed(Core.Garden system, Core.Chao chao, Guild guild)
        {
            var name = chao.Name;
            if (system.Name != null) name = $"{name} ({system.Name})";

            var guildSettings = guild != null ? await _repo.GetChaoGuild(guild.Id, chao.Id) : null;
            var guildDisplayName = guildSettings?.DisplayName;
            var avatar = guildSettings?.AvatarUrl;

            var eb = new EmbedBuilder()
                // TODO: add URL of website when that's up
                .Author(new(name, IconUrl: avatar.TryGetCleanCdnUrl()))
                .Footer(new(
                    $"Garden ID: {system.Hid} | Chao ID: {chao.Id} {$"| Created on {chao.Created.FormatZoned(DateTimeZone.Utc)}"}"));

            if (avatar != null) eb.Thumbnail(new(avatar.TryGetCleanCdnUrl()));
            if (guild != null && guildDisplayName != null) eb.Field(new($"Server Nickname (for {guild.Name})", guildDisplayName.Truncate(1024), true));

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
                .Field(new("Chao", $"{msg.Chao.Name} (`{msg.Chao.Hid}`)", true))
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