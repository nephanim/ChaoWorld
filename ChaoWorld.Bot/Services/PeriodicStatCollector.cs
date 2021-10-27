using System.Diagnostics;
using System.Threading.Tasks;
using App.Metrics;

using Dapper;

using Myriad.Cache;
using Myriad.Types;

using NodaTime.Extensions;
using ChaoWorld.Core;

using Serilog;

namespace ChaoWorld.Bot
{
    public class PeriodicStatCollector
    {
        private readonly IMetrics _metrics;
        private readonly IDiscordCache _cache;
        private readonly CpuStatService _cpu;

        private readonly ModelRepository _repo;

        private readonly WebhookCacheService _webhookCache;

        private readonly DbConnectionCountHolder _countHolder;

        private readonly ILogger _logger;

        public PeriodicStatCollector(IMetrics metrics, ILogger logger, WebhookCacheService webhookCache, DbConnectionCountHolder countHolder, CpuStatService cpu, ModelRepository repo, IDiscordCache cache)
        {
            _metrics = metrics;
            _webhookCache = webhookCache;
            _countHolder = countHolder;
            _cpu = cpu;
            _repo = repo;
            _cache = cache;
            _logger = logger.ForContext<PeriodicStatCollector>();
        }

        public async Task<int> CollectStats()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Aggregate guild/channel stats
            var guildCount = 0;
            var channelCount = 0;

            // No LINQ today, sorry
            await foreach (var guild in _cache.GetAllGuilds())
            {
                guildCount++;
                foreach (var channel in _cache.GetGuildChannels(guild.Id))
                {
                    if (DiscordUtils.IsValidGuildChannel(channel))
                        channelCount++;
                }
            }

            _metrics.Measure.Gauge.SetValue(BotMetrics.Guilds, guildCount);
            _metrics.Measure.Gauge.SetValue(BotMetrics.Channels, channelCount);

            // Aggregate DB stats
            // just fetching from database here - actual updating of the data is done in ChaoWorld.ScheduledTasks
            // if you're not running ScheduledTasks and want up-to-date counts, uncomment the following line:
            //await _repo.UpdateStats(); //TODO: Reevaluate whether we need this
            var counts = await _repo.GetStats();
            _metrics.Measure.Gauge.SetValue(CoreMetrics.GardenCount, counts.GardenCount);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ChaoCount, counts.ChaoCount);
            _cache.SetTotalChao(counts.ChaoCount);

            // Process info
            var process = Process.GetCurrentProcess();
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ProcessPhysicalMemory, process.WorkingSet64);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ProcessVirtualMemory, process.VirtualMemorySize64);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ProcessPrivateMemory, process.PrivateMemorySize64);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ProcessThreads, process.Threads.Count);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.ProcessHandles, process.HandleCount);
            _metrics.Measure.Gauge.SetValue(CoreMetrics.CpuUsage, await _cpu.EstimateCpuUsage());

            // Database info
            _metrics.Measure.Gauge.SetValue(CoreMetrics.DatabaseConnections, _countHolder.ConnectionCount);

            // Other shiz
            _metrics.Measure.Gauge.SetValue(BotMetrics.WebhookCacheSize, _webhookCache.CacheSize);

            stopwatch.Stop();
            _logger.Debug("Updated metrics in {Time}", stopwatch.ElapsedDuration());

            return counts.ChaoCount;
        }
    }
}