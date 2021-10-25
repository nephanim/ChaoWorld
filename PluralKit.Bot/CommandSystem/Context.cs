using System;
using System.Threading.Tasks;

using App.Metrics;

using Autofac;

using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Gateway;
using Myriad.Rest;
using Myriad.Rest.Types;
using Myriad.Rest.Types.Requests;
using Myriad.Types;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class Context
    {
        private readonly ILifetimeScope _provider;

        private readonly DiscordApiClient _rest;
        private readonly Cluster _cluster;
        private readonly Shard _shard;
        private readonly Guild? _guild;
        private readonly Channel _channel;
        private readonly MessageCreateEvent _message;
        private readonly Parameters _parameters;
        private readonly MessageContext _messageContext;

        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly Core.Garden _senderSystem;
        private readonly IMetrics _metrics;
        private readonly IDiscordCache _cache;

        private Command? _currentCommand;

        public Context(ILifetimeScope provider, Shard shard, Guild? guild, Channel channel, MessageCreateEvent message, int commandParseOffset,
                       Core.Garden senderSystem, MessageContext messageContext)
        {
            _message = message;
            _shard = shard;
            _guild = guild;
            _channel = channel;
            _senderSystem = senderSystem;
            _messageContext = messageContext;
            _cache = provider.Resolve<IDiscordCache>();
            _db = provider.Resolve<IDatabase>();
            _repo = provider.Resolve<ModelRepository>();
            _metrics = provider.Resolve<IMetrics>();
            _provider = provider;
            _parameters = new Parameters(message.Content?.Substring(commandParseOffset));
            _rest = provider.Resolve<DiscordApiClient>();
            _cluster = provider.Resolve<Cluster>();
        }

        public IDiscordCache Cache => _cache;

        public Channel Channel => _channel;
        public User Author => _message.Author;
        public GuildMemberPartial Member => _message.Member;

        public Message Message => _message;
        public Guild Guild => _guild;
        public Shard Shard => _shard;
        public Cluster Cluster => _cluster;
        public MessageContext MessageContext => _messageContext;

        public PermissionSet BotPermissions => _provider.Resolve<Bot>().PermissionsIn(_channel.Id);
        public PermissionSet UserPermissions => _cache.PermissionsFor(_message);

        public DiscordApiClient Rest => _rest;

        public Core.Garden System => _senderSystem;

        public Parameters Parameters => _parameters;

        internal IDatabase Database => _db;
        internal ModelRepository Repository => _repo;

        public async Task<Message> Reply(string text = null, Embed embed = null, AllowedMentions? mentions = null)
        {
            if (!BotPermissions.HasFlag(PermissionSet.SendMessages))
                // Will be "swallowed" during the error handler anyway, this message is never shown.
                throw new CWError("ChaoWorld does not have permission to send messages in this channel.");

            if (embed != null && !BotPermissions.HasFlag(PermissionSet.EmbedLinks))
                throw new CWError("ChaoWorld does not have permission to send embeds in this channel. Please ensure I have the **Embed Links** permission enabled.");

            var msg = await _rest.CreateMessage(_channel.Id, new MessageRequest
            {
                Content = text,
                Embed = embed,
                // Default to an empty allowed mentions object instead of null (which means no mentions allowed)
                AllowedMentions = mentions ?? new AllowedMentions()
            });

            return msg;
        }

        public async Task Execute<T>(Command? commandDef, Func<T, Task> handler)
        {
            _currentCommand = commandDef;

            try
            {
                using (_metrics.Measure.Timer.Time(BotMetrics.CommandTime, new MetricTags("Command", commandDef?.Key ?? "null")))
                    await handler(_provider.Resolve<T>());

                _metrics.Measure.Meter.Mark(BotMetrics.CommandsRun);
            }
            catch (CWSyntaxError e)
            {
                await Reply($"{Emojis.Error} {e.Message}\n**Command usage:**\n> pk;{commandDef?.Usage}");
            }
            catch (CWError e)
            {
                await Reply($"{Emojis.Error} {e.Message}");
            }
            catch (TimeoutException)
            {
                // Got a complaint the old error was a bit too patronizing. Hopefully this is better?
                await Reply($"{Emojis.Error} Operation timed out, sorry. Try again, perhaps?");
            }
        }

        public IComponentContext Services => _provider;
    }
}