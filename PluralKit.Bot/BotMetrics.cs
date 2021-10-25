using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Timer;

namespace ChaoWorld.Bot
{
    public static class BotMetrics
    {
        public static MeterOptions MessagesReceived => new MeterOptions { Name = "Messages processed", MeasurementUnit = Unit.Events, RateUnit = TimeUnit.Seconds, Context = "Bot" };
        public static MeterOptions MessagesProxied => new MeterOptions { Name = "Messages proxied", MeasurementUnit = Unit.Events, RateUnit = TimeUnit.Seconds, Context = "Bot" };
        public static MeterOptions CommandsRun => new MeterOptions { Name = "Commands run", MeasurementUnit = Unit.Commands, RateUnit = TimeUnit.Seconds, Context = "Bot" };
        public static TimerOptions CommandTime => new TimerOptions { Name = "Command run time", MeasurementUnit = Unit.Commands, RateUnit = TimeUnit.Seconds, DurationUnit = TimeUnit.Seconds, Context = "Bot" };
        public static GaugeOptions ChaoTotal => new GaugeOptions { Name = "Chao total", MeasurementUnit = Unit.None, Context = "Bot" };
        public static GaugeOptions ChaoOnline => new GaugeOptions { Name = "Chao online", MeasurementUnit = Unit.None, Context = "Bot" };
        public static GaugeOptions Guilds => new GaugeOptions { Name = "Guilds", MeasurementUnit = Unit.None, Context = "Bot" };
        public static GaugeOptions Channels => new GaugeOptions { Name = "Channels", MeasurementUnit = Unit.None, Context = "Bot" };
        public static GaugeOptions ShardLatency => new GaugeOptions { Name = "Shard Latency", Context = "Bot" };
        public static GaugeOptions ShardsConnected => new GaugeOptions { Name = "Shards Connected", Context = "Bot", MeasurementUnit = Unit.Connections };
        public static MeterOptions WebhookCacheMisses => new MeterOptions { Name = "Webhook cache misses", Context = "Bot", MeasurementUnit = Unit.Calls };
        public static GaugeOptions WebhookCacheSize => new GaugeOptions { Name = "Webhook Cache Size", Context = "Bot", MeasurementUnit = Unit.Items };
        public static TimerOptions WebhookResponseTime => new TimerOptions { Name = "Webhook Response Time", Context = "Bot", RateUnit = TimeUnit.Seconds, MeasurementUnit = Unit.Requests, DurationUnit = TimeUnit.Seconds };
        public static TimerOptions MessageContextQueryTime => new TimerOptions { Name = "Message context query duration", Context = "Bot", RateUnit = TimeUnit.Seconds, DurationUnit = TimeUnit.Seconds, MeasurementUnit = Unit.Calls };
        public static TimerOptions ProxyChaoQueryTime => new TimerOptions { Name = "Proxy chao query duration", Context = "Bot", RateUnit = TimeUnit.Seconds, DurationUnit = TimeUnit.Seconds, MeasurementUnit = Unit.Calls };
        public static TimerOptions DiscordApiRequests => new TimerOptions { Name = "Discord API requests", MeasurementUnit = Unit.Requests, DurationUnit = TimeUnit.Milliseconds, Context = "Bot" };
        public static MeterOptions BotErrors => new MeterOptions { Name = "Bot errors", MeasurementUnit = Unit.Errors, RateUnit = TimeUnit.Seconds, Context = "Bot" };
        public static MeterOptions ErrorMessagesSent => new MeterOptions { Name = "Error messages sent", MeasurementUnit = Unit.Errors, RateUnit = TimeUnit.Seconds, Context = "Bot" };
        public static TimerOptions EventsHandled => new TimerOptions { Name = "Events handled", MeasurementUnit = Unit.Errors, RateUnit = TimeUnit.Seconds, DurationUnit = TimeUnit.Seconds, Context = "Bot" };
    }
}