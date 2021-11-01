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
            //await TryHandleProxyMessageReactions(evt);
        }

        private async Task TryRemoveOriginalReaction(MessageReactionAddEvent evt)
        {
            if (_bot.PermissionsIn(evt.ChannelId).HasFlag(PermissionSet.ManageMessages))
                await _rest.DeleteUserReaction(evt.ChannelId, evt.MessageId, evt.Emoji, evt.UserId);
        }
    }
}