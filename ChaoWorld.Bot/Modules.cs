using System;
using System.Net.Http;

using Autofac;

using Myriad.Cache;
using Myriad.Gateway;
using Myriad.Rest;

using NodaTime;

using ChaoWorld.Core;

using Sentry;

using Serilog;

namespace ChaoWorld.Bot
{
    public class BotModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Clients
            builder.Register(c =>
            {
                var botConfig = c.Resolve<BotConfig>();
                return new GatewaySettings
                {
                    Token = botConfig.Token,
                    MaxShardConcurrency = botConfig.MaxShardConcurrency,
                    GatewayQueueUrl = botConfig.GatewayQueueUrl,
                    Intents = GatewayIntent.Guilds |
                              GatewayIntent.DirectMessages |
                              GatewayIntent.DirectMessageReactions |
                              GatewayIntent.GuildEmojis |
                              GatewayIntent.GuildMessages |
                              GatewayIntent.GuildWebhooks |
                              GatewayIntent.GuildMessageReactions
                };
            }).AsSelf().SingleInstance();
            builder.RegisterType<Cluster>().AsSelf().SingleInstance();
            builder.Register(c => new Myriad.Rest.DiscordApiClient(c.Resolve<BotConfig>().Token, c.Resolve<ILogger>()))
                .AsSelf().SingleInstance();
            builder.RegisterType<MemoryDiscordCache>().AsSelf().As<IDiscordCache>().SingleInstance();

            // Commands
            builder.RegisterType<CommandTree>().AsSelf();
            builder.RegisterType<Help>().AsSelf();
            builder.RegisterType<Chao>().AsSelf();
            builder.RegisterType<ChaoEdit>().AsSelf();
            builder.RegisterType<Misc>().AsSelf();
            builder.RegisterType<Random>().AsSelf();
            builder.RegisterType<Garden>().AsSelf();
            builder.RegisterType<GardenEdit>().AsSelf();
            builder.RegisterType<GardenList>().AsSelf();
            builder.RegisterType<Race>().AsSelf();
            builder.RegisterType<RaceList>().AsSelf();
            builder.RegisterType<Tournament>().AsSelf();
            builder.RegisterType<TournamentList>().AsSelf();
            builder.RegisterType<Item>().AsSelf();
            builder.RegisterType<ItemList>().AsSelf();

            // Bot core
            builder.RegisterType<Bot>().AsSelf().SingleInstance();
            builder.RegisterType<MessageCreated>().As<IEventHandler<MessageCreateEvent>>();
            builder.RegisterType<MessageDeleted>().As<IEventHandler<MessageDeleteEvent>>().As<IEventHandler<MessageDeleteBulkEvent>>();
            builder.RegisterType<MessageEdited>().As<IEventHandler<MessageUpdateEvent>>();
            builder.RegisterType<ReactionAdded>().As<IEventHandler<MessageReactionAddEvent>>();
            builder.RegisterType<InteractionCreated>().As<IEventHandler<InteractionCreateEvent>>();

            // Event handler queue
            builder.RegisterType<HandlerQueue<MessageCreateEvent>>().AsSelf().SingleInstance();
            builder.RegisterType<HandlerQueue<MessageReactionAddEvent>>().AsSelf().SingleInstance();

            // Bot services
            builder.RegisterType<EmbedService>().AsSelf().SingleInstance();
            builder.RegisterType<WebhookExecutorService>().AsSelf().SingleInstance();
            builder.RegisterType<WebhookCacheService>().AsSelf().SingleInstance();
            builder.RegisterType<ShardInfoService>().AsSelf().SingleInstance();
            builder.RegisterType<CpuStatService>().AsSelf().SingleInstance();
            builder.RegisterType<PeriodicStatCollector>().AsSelf().SingleInstance();
            builder.RegisterType<LastMessageCacheService>().AsSelf().SingleInstance();
            builder.RegisterType<LoggerCleanService>().AsSelf().SingleInstance();
            builder.RegisterType<ErrorMessageService>().AsSelf().SingleInstance();
            builder.RegisterType<InteractionDispatchService>().AsSelf().SingleInstance();

            // Sentry stuff
            builder.Register(_ => new Scope(null)).AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<SentryEnricher>()
                .As<ISentryEnricher<MessageCreateEvent>>()
                .As<ISentryEnricher<MessageDeleteEvent>>()
                .As<ISentryEnricher<MessageUpdateEvent>>()
                .As<ISentryEnricher<MessageDeleteBulkEvent>>()
                .As<ISentryEnricher<MessageReactionAddEvent>>()
                .SingleInstance();

            // Utils
            builder.Register(c => new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
                DefaultRequestHeaders = { { "User-Agent", DiscordApiClient.UserAgent } }
            }).AsSelf().SingleInstance();
            builder.RegisterInstance(SystemClock.Instance).As<IClock>();
            builder.RegisterType<SerilogGatewayEnricherFactory>().AsSelf().SingleInstance();

            builder.RegisterType<DiscordRequestObserver>().AsSelf().SingleInstance();
        }
    }
}