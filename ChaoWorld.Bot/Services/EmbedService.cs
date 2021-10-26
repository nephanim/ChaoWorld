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

        public async Task<Embed> CreateGardenEmbed(Context cctx, Core.Garden garden)
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
                eb.Field(new($"Chao ({chaoCount})", $"(see `!garden {garden.Id} list` or `!garden {garden.Id} list full`)", true));
            else
                eb.Field(new($"Chao ({chaoCount})", "Add one with `!chao new`!", true));

            return eb.Build();
        }

        public Embed CreateLoggedMessageEmbed(Message triggerMessage, Message proxiedMessage, string gardenId, Core.Chao chao, string channelName, string oldContent = null)
        {
            // TODO: pronouns in ?-reacted response using this card
            var timestamp = DiscordUtils.SnowflakeToInstant(proxiedMessage.Id);
            var name = proxiedMessage.Author.Username;
            // sometimes Discord will just... not return the avatar hash with webhook messages
            var embed = new EmbedBuilder()
                .Description(proxiedMessage.Content?.NormalizeLineEndSpacing())
                .Footer(new($"Garden ID: {gardenId} | Chao ID: {chao.Id} | Sender: {triggerMessage.Author.Username}#{triggerMessage.Author.Discriminator} ({triggerMessage.Author.Id}) | Message ID: {proxiedMessage.Id} | Original Message ID: {triggerMessage.Id}"))
                .Timestamp(timestamp.ToDateTimeOffset().ToString("O"));

            if (oldContent != null)
                embed.Field(new("Old message", oldContent?.NormalizeLineEndSpacing().Truncate(1000)));

            return embed.Build();
        }

        public async Task<Embed> CreateChaoEmbed(Core.Garden system, Core.Chao chao, Guild guild)
        {
            var name = chao.Name;

            var eb = new EmbedBuilder()
                .Author(new(name))
                .Footer(new(
                    $"Garden ID: {system.Hid} | Chao ID: {chao.Id} {$"| Created on {chao.CreatedOn.FormatZoned(DateTimeZone.Utc)}"}"));
            eb.Field(new($"Swim (Lv.{chao.SwimLevel:D2})", $"Grade {chao.SwimGrade} - {chao.SwimProgress:D4}/1000 ({chao.SwimValue:D4})"));
            eb.Field(new($"Fly (Lv.{chao.FlyLevel:D2})", $"Grade {chao.FlyGrade} - {chao.FlyProgress:D4}/1000 ({chao.FlyValue:D4})"));
            eb.Field(new($"Run (Lv.{chao.RunLevel:D2})", $"Grade {chao.RunGrade} - {chao.RunProgress:D4}/1000 ({chao.RunValue:D4})"));
            eb.Field(new($"Power (Lv.{chao.PowerLevel:D2})", $"Grade {chao.PowerGrade} - {chao.PowerProgress:D4}/1000 ({chao.PowerValue:D4})"));
            eb.Field(new($"Stamina (Lv.{chao.StaminaLevel:D2})", $"Grade {chao.StaminaGrade} - {chao.StaminaProgress:D4}/1000 ({chao.StaminaValue:D4})"));
            eb.Field(new($"Intelligence (Lv.{chao.IntelligenceLevel:D2})", $"Grade {chao.IntelligenceGrade} - {chao.IntelligenceProgress:D4}/1000 ({chao.IntelligenceValue:D4})"));
            eb.Field(new($"Luck (Lv.{chao.LuckLevel:D2})", $"Grade {chao.LuckGrade} - {chao.LuckProgress:D4}/1000 ({chao.LuckValue:D4})"));

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
                .Field(new("Chao", $"{msg.Chao.Name} (`{msg.Chao.Id}`)", true))
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