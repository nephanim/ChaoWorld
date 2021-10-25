using System;
using System.Threading.Tasks;

using App.Metrics;

using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Gateway;
using Myriad.Rest;
using Myriad.Types;

using ChaoWorld.Core;

using Serilog;


namespace ChaoWorld.Bot
{
    public class MessageEdited: IEventHandler<MessageUpdateEvent>
    {
        private readonly LastMessageCacheService _lastMessageCache;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IMetrics _metrics;
        private readonly Cluster _client;
        private readonly IDiscordCache _cache;
        private readonly Bot _bot;
        private readonly DiscordApiClient _rest;
        private readonly ILogger _logger;

        public MessageEdited(LastMessageCacheService lastMessageCache, IDatabase db, IMetrics metrics, ModelRepository repo, Cluster client, IDiscordCache cache, Bot bot, DiscordApiClient rest, ILogger logger)
        {
            _lastMessageCache = lastMessageCache;
            _db = db;
            _metrics = metrics;
            _repo = repo;
            _client = client;
            _cache = cache;
            _bot = bot;
            _rest = rest;
            _logger = logger.ForContext<MessageEdited>();
        }

        public async Task Handle(Shard shard, MessageUpdateEvent evt)
        {
            if (evt.Author.Value?.Id == _client.User?.Id) return;

            // Edit message events sometimes arrive with missing data; double-check it's all there
            if (!evt.Content.HasValue || !evt.Author.HasValue || !evt.Member.HasValue)
                return;

            var channel = _cache.GetChannel(evt.ChannelId);
            if (!DiscordUtils.IsValidGuildChannel(channel))
                return;
            var guild = _cache.GetGuild(channel.GuildId!.Value);
            var lastMessage = _lastMessageCache.GetLastMessage(evt.ChannelId)?.Current;

            // Only react to the last message in the channel
            if (lastMessage?.Id != evt.Id)
                return;

            // Just run the normal message handling code, with a flag to disable autoproxying
            MessageContext ctx;
            using (_metrics.Measure.Timer.Time(BotMetrics.MessageContextQueryTime))
                ctx = await _repo.GetMessageContext(evt.Author.Value!.Id, channel.GuildId!.Value, evt.ChannelId);
        }
    }
}