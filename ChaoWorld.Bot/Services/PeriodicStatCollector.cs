using System.Diagnostics;
using System.Threading.Tasks;
using App.Metrics;

using Dapper;

using Myriad.Cache;
using Myriad.Types;

using NodaTime.Extensions;
using ChaoWorld.Core;

using Serilog;
using System.Linq;
using System.Collections.Generic;
using Myriad.Rest.Types;
using Myriad.Rest;
using Myriad.Rest.Types.Requests;
using System;

namespace ChaoWorld.Bot
{
    public class PeriodicStatCollector
    {
        private readonly IMetrics _metrics;
        private readonly IDiscordCache _cache;
        private readonly CpuStatService _cpu;

        private readonly DiscordApiClient _rest;

        private readonly ModelRepository _repo;

        private readonly WebhookCacheService _webhookCache;

        private readonly DbConnectionCountHolder _countHolder;

        private readonly ILogger _logger;

        public PeriodicStatCollector(IMetrics metrics, ILogger logger, WebhookCacheService webhookCache, DbConnectionCountHolder countHolder, CpuStatService cpu, ModelRepository repo, IDiscordCache cache, DiscordApiClient rest)
        {
            _metrics = metrics;
            _webhookCache = webhookCache;
            _countHolder = countHolder;
            _cpu = cpu;
            _repo = repo;
            _cache = cache;
            _rest = rest;
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

        public async Task RunPeriodicWithBroadcast()
        {
            var broadcastChannels = await _repo.ReadBroadcastChannels();

            _logger.Information("Updating available races...");
            await InstantiateRaces(broadcastChannels.Races);
            await CancelRaces(broadcastChannels.Races);

            _logger.Information("Updating available tournaments...");
            await InstantiateTournaments(broadcastChannels.Tournaments);
            await CancelTournaments(broadcastChannels.Races);
        }

        public async Task RunHourlyWithBroadcast()
        {
            var broadcastChannels = await _repo.ReadBroadcastChannels();

            await RunFirstEvolutions(broadcastChannels.General);

            _logger.Information("Updating Black Market...");
            await _repo.ClearMarketListings();
            try
            {
                var listings = await MakeMarketListings();
                foreach (var item in listings)
                {
                    await _repo.AddMarketItem(item);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to list items on the market: {e.Message} {e.StackTrace}");
            }

            _logger.Information("Reincarnating eligible NPCs...");
            await _repo.ReincarnateEligibleNpcChao();

            _logger.Information("Recalculating instance prize amounts...");
            await _repo.RecalculateRaceRewards();
            await _repo.RecalculateTournamentRewards();
        }

        private async Task InstantiateRaces(ulong channel)
        {
            var races = await _repo.GetAvailableRaces();
            foreach (var r in races)
            {
                await _repo.ResetRaceAvailableOn(r);
                var instance = await _repo.CreateRaceInstance(r);
                _logger.Information($"Created instance {instance.Id} of race {r.Id} ({r.Name})");
                await SendMessage(channel, $"{Emojis.Megaphone} The {r.Name} Race will begin in {r.ReadyDelayMinutes} minutes.");
            }
        }

        private async Task CancelRaces(ulong channel)
        {
            var instances = await _repo.GetExpiredRaceInstances();
            foreach (var i in instances)
            {
                var race = await _repo.GetRaceByInstanceId(i.Id);
                await _repo.DeleteRaceInstance(i);
                await SendMessage(channel, $"{Emojis.Stop} The {race.Name} Race has been canceled as it did not reach the minimum number of participants.");
            }
        }

        private async Task InstantiateTournaments(ulong channel)
        {
            var tournaments = await _repo.GetAvailableTournaments();
            foreach (var t in tournaments)
            {
                await _repo.ResetTournamentAvailableOn(t);
                var instance = await _repo.CreateTournamentInstance(t);
                _logger.Information($"Created instance {instance.Id} of tournament {t.Id} ({t.Name})");
                await SendMessage(channel, $"{Emojis.Megaphone} The {t.Name} Tournament will begin in {t.ReadyDelayMinutes}.");
            }
        }

        private async Task CancelTournaments(ulong channel)
        {
            var instances = await _repo.GetExpiredTournamentInstances();
            foreach (var i in instances)
            {
                var tourney = await _repo.GetTournamentByInstanceId(i.Id);
                await _repo.DeleteTournamentInstance(i);
                await SendMessage(channel, $"{Emojis.Stop} The {tourney.Name} Tournament has been canceled as it did not reach the minimum number of participants.");
            }
        }

        private async Task RunFirstEvolutions(ulong channel)
        {
            var chao = await _repo.GetChaoReadyForFirstEvolution();
            foreach (var c in chao)
            {
                var abilityType = c.GetEffectiveAbilityType();
                c.Alignment = c.GetEffectiveAlignment();
                c.EvolutionState = Core.Chao.EvolutionStates.First;
                c.FirstEvolutionType = abilityType;
                c.RaiseStatGrade(abilityType);
                c.FlySwimAffinity = 0;
                c.RunPowerAffinity = 0;
                await _repo.UpdateChao(c);
                await SendMessage(channel, $"{Emojis.Megaphone} {c.Name} has reached their first evolution! Congratulations!");
            }
        }

        private async Task<List<MarketItem>> MakeMarketListings()
        {
            var items = new List<MarketItem>();

            // Common eggs (non-shiny monotone)
            var commonEggLimit = new Random().Next(1, 6); // Always have at least one egg in the market
            var commonEggs = await _repo.GetMarketEnabledEggs(commonEggLimit, false);
            foreach (var egg in commonEggs)
            {
                egg.Quantity = 1;
                items.Add(egg);
            }

            // Rare eggs (shiny monotone)
            var uncommonEggLimit = new Random().Next(1, 5) == 1 ? 1 : 0; // Only have shiny eggs available every few hours
            var rareEggs = await _repo.GetMarketEnabledEggs(uncommonEggLimit, true);
            foreach (var egg in rareEggs)
            {
                egg.Quantity = 1;
                items.Add(egg);
            }

            // Common fruit (e.g. tasty, round, 
            var commonFruitLimit = new Random().Next(3, 6); // Always have some fruit on the market
            var commonFruit = await _repo.GetMarketEnabledFruit(commonFruitLimit, false);
            foreach (var fruit in commonFruit)
            {
                fruit.Quantity = new Random().Next(3, 6);
                items.Add(fruit);
            }

            var rareFruitLimit = new Random().Next(1, 25) == 1 ? 1 : 0; // Only have hyper fruit available roughly once per day
            var rareFruit = await _repo.GetMarketEnabledFruit(rareFruitLimit, true);
            foreach (var fruit in rareFruit)
            {
                fruit.Quantity = 1;
                items.Add(fruit);
            }

            var specialLimit = new Random().Next(1, 9) == 1 ? 1 : 0; // Have special items available a few times per day
            var specials = await _repo.GetMarketEnabledSpecials(specialLimit);
            foreach (var special in specials)
            {
                special.Quantity = 1;
                items.Add(special);
            }

            return items;
        }

        public async Task<Message> SendMessage(ulong channel, string text = null, Embed embed = null, AllowedMentions? mentions = null)
        {
            var msg = await _rest.CreateMessage(channel, new MessageRequest
            {
                Content = text,
                Embed = embed,
                // Default to an empty allowed mentions object instead of null (which means no mentions allowed)
                AllowedMentions = mentions ?? new AllowedMentions()
            });

            return msg;
        }
    }
}