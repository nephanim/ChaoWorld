using System.Threading.Tasks;

using Myriad.Builders;
using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Gateway;
using Myriad.Rest;
using Myriad.Rest.Exceptions;
using Myriad.Rest.Types;
using Myriad.Rest.Types.Requests;
using Myriad.Types;

using ChaoWorld.Core;

using Serilog;

namespace ChaoWorld.Bot
{
    public class ReactionAdded: IEventHandler<MessageReactionAddEvent>
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly ILogger _logger;
        private readonly IDiscordCache _cache;
        private readonly EmbedService _embeds;
        private readonly Bot _bot;
        private readonly Cluster _cluster;
        private readonly DiscordApiClient _rest;

        public ReactionAdded(ILogger logger, IDatabase db, ModelRepository repo, IDiscordCache cache, Bot bot, Cluster cluster, DiscordApiClient rest, EmbedService embeds)
        {
            _db = db;
            _repo = repo;
            _cache = cache;
            _bot = bot;
            _cluster = cluster;
            _rest = rest;
            _embeds = embeds;
            _logger = logger.ForContext<ReactionAdded>();
        }

        public async Task Handle(Shard shard, MessageReactionAddEvent evt)
        {
            await TryHandleProxyMessageReactions(evt);
        }

        private async ValueTask TryHandleProxyMessageReactions(MessageReactionAddEvent evt)
        {
            // Sometimes we get events from users that aren't in the user cache
            // We just ignore all of those for now, should be quite rare...
            if (!_cache.TryGetUser(evt.UserId, out var user))
                return;

            // ignore any reactions added by *us*
            if (evt.UserId == _cluster.User?.Id)
                return;

            // Ignore reactions from bots (we can't DM them anyway)
            if (user.Bot) return;

            var channel = _cache.GetChannel(evt.ChannelId);

            // Proxied messages only exist in guild text channels, so skip checking if we're elsewhere
            if (!DiscordUtils.IsValidGuildChannel(channel)) return;

            switch (evt.Emoji.Name)
            {
                // Message deletion
                case "\u274C": // Red X
                    {
                        var msg = await _db.Execute(c => _repo.GetMessage(c, evt.MessageId));
                        if (msg != null)
                            await HandleProxyDeleteReaction(evt, msg);

                        break;
                    }
                case "\u2753": // Red question mark
                case "\u2754": // White question mark
                    {
                        var msg = await _db.Execute(c => _repo.GetMessage(c, evt.MessageId));
                        if (msg != null)
                            await HandleQueryReaction(evt, msg);

                        break;
                    }

                case "\U0001F514": // Bell
                case "\U0001F6CE": // Bellhop bell
                case "\U0001F3D3": // Ping pong paddle (lol)
                case "\u23F0": // Alarm clock
                case "\u2757": // Exclamation mark
                    {
                        var msg = await _db.Execute(c => _repo.GetMessage(c, evt.MessageId));
                        if (msg != null)
                            await HandlePingReaction(evt, msg);
                        break;
                    }
            }
        }

        private async ValueTask HandleProxyDeleteReaction(MessageReactionAddEvent evt, FullMessage msg)
        {
            if (!_bot.PermissionsIn(evt.ChannelId).HasFlag(PermissionSet.ManageMessages))
                return;

            var system = await _repo.GetSystemByAccount(evt.UserId);

            // Can only delete your own message
            if (msg.System.Id != system?.Id) return;

            try
            {
                await _rest.DeleteMessage(evt.ChannelId, evt.MessageId);
            }
            catch (NotFoundException)
            {
                // Message was deleted by something/someone else before we got to it
            }

            await _repo.DeleteMessage(evt.MessageId);
        }

        private async ValueTask HandleQueryReaction(MessageReactionAddEvent evt, FullMessage msg)
        {
            var guild = _cache.GetGuild(evt.GuildId!.Value);

            // Try to DM the user info about the message
            try
            {
                var dm = await _cache.GetOrCreateDmChannel(_rest, evt.UserId);
                await _rest.CreateMessage(dm.Id, new MessageRequest
                {
                    Embed = await _embeds.CreateMemberEmbed(msg.System, msg.Member, guild)
                });

                await _rest.CreateMessage(dm.Id, new MessageRequest
                {
                    Embed = await _embeds.CreateMessageInfoEmbed(msg)
                });
            }
            catch (ForbiddenException) { } // No permissions to DM, can't check for this :(

            await TryRemoveOriginalReaction(evt);
        }

        private async ValueTask HandlePingReaction(MessageReactionAddEvent evt, FullMessage msg)
        {
            if (!_bot.PermissionsIn(evt.ChannelId).HasFlag(PermissionSet.ManageMessages))
                return;

            // Check if the "pinger" has permission to send messages in this channel
            // (if not, PK shouldn't send messages on their behalf)
            var member = await _rest.GetGuildMember(evt.GuildId!.Value, evt.UserId);
            var requiredPerms = PermissionSet.ViewChannel | PermissionSet.SendMessages;
            if (member == null || !_cache.PermissionsFor(evt.ChannelId, member).HasFlag(requiredPerms)) return;

            if (msg.System.PingsEnabled)
            {
                // If the system has pings enabled, go ahead
                await _rest.CreateMessage(evt.ChannelId, new()
                {
                    Content = $"Psst, **{msg.Member.DisplayName()}** (<@{msg.Message.Sender}>), you have been pinged by <@{evt.UserId}>.",
                    Components = new[]
                    {
                        new MessageComponent
                        {
                            Type = ComponentType.ActionRow,
                            Components = new[]
                            {
                                new MessageComponent
                                {
                                    Style = ButtonStyle.Link,
                                    Type = ComponentType.Button,
                                    Label = "Jump",
                                    Url = evt.JumpLink()
                                }
                            }
                        }
                    },
                    AllowedMentions = new AllowedMentions { Users = new[] { msg.Message.Sender } }
                });
            }
            else
            {
                // If not, tell them in DMs (if we can)
                try
                {
                    var dm = await _cache.GetOrCreateDmChannel(_rest, evt.UserId);
                    await _rest.CreateMessage(dm.Id, new MessageRequest
                    {
                        Content = $"{Emojis.Error} {msg.Member.DisplayName()}'s system has disabled reaction pings. If you want to mention them anyway, you can copy/paste the following message:"
                    });
                    await _rest.CreateMessage(dm.Id, new MessageRequest { Content = $"<@{msg.Message.Sender}>".AsCode() });
                }
                catch (ForbiddenException) { }
            }

            await TryRemoveOriginalReaction(evt);
        }

        private async Task TryRemoveOriginalReaction(MessageReactionAddEvent evt)
        {
            if (_bot.PermissionsIn(evt.ChannelId).HasFlag(PermissionSet.ManageMessages))
                await _rest.DeleteUserReaction(evt.ChannelId, evt.MessageId, evt.Emoji, evt.UserId);
        }
    }
}