using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;
using NodaTime.Extensions;

using Serilog;

using ChaoWorld.Core;
using System.Collections.Generic;

namespace ChaoWorld.ScheduledTasks
{
    public class TaskHandler
    {
        private Timer _periodicTask;
        private Timer _hourlyTask;

        private readonly ILogger _logger;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;

        public TaskHandler(ILogger logger, IDatabase db, ModelRepository repo)
        {
            _logger = logger;
            _db = db;
            _repo = repo;
        }

        public void Run()
        {
            _logger.Information("Starting scheduled task runner...");
            var timeNow = SystemClock.Instance.GetCurrentInstant();

            var timeTillNextWholeMinute = TimeSpan.FromMilliseconds(60000 - timeNow.ToUnixTimeMilliseconds() % 60000 + 250);
            _periodicTask = new Timer(_ =>
            {
                var __ = UpdatePerMinute();
            }, null, timeTillNextWholeMinute, TimeSpan.FromMinutes(1));

            var timeTillNextWholeHour = TimeSpan.FromSeconds(3600 - timeNow.ToUnixTimeSeconds() % 3600);
            _hourlyTask = new Timer(_ =>
            {
                var __ = UpdateHourly();
            }, null, timeTillNextWholeHour, TimeSpan.FromHours(1));
        }

        private async Task UpdatePerMinute()
        {
            _logger.Information("Running per-minute scheduled tasks.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _logger.Information("Updating database stats...");
            await _repo.UpdateStats();

            _logger.Information("Updating available race instances...");
            await InstantiateRaces();

            _logger.Information("Updating available tournament instances...");
            await InstantiateTournaments();

            stopwatch.Stop();
            _logger.Information("Ran per-minute scheduled tasks in {Time}", stopwatch.ElapsedDuration());
        }

        private async Task RunFirstEvolutions()
        {
            var chao = await _repo.GetChaoReadyForFirstEvolution();
            foreach (var c in chao)
            {
                var abilityType = c.GetEffectiveAbilityType();
                c.Alignment = c.GetEffectiveAlignment();
                c.EvolutionState = Chao.EvolutionStates.First;
                c.FirstEvolutionType = abilityType;
                c.RaiseStatGrade(abilityType);
                c.FlySwimAffinity = 0;
                c.RunPowerAffinity = 0;
                await _repo.UpdateChao(c);
            }
        }

        private async Task InstantiateRaces()
        {
            var races = await _repo.GetAvailableRaces();
            foreach (var r in races)
            {
                await _repo.ResetRaceAvailableOn(r);
                var instance = await _repo.CreateRaceInstance(r);
            }
        }

        private async Task InstantiateTournaments()
        {
            var tournaments = await _repo.GetAvailableTournaments();
            foreach (var t in tournaments)
            {
                await _repo.ResetTournamentAvailableOn(t);
                var instance = await _repo.CreateTournamentInstance(t);
            }
        }

        private async Task UpdateHourly()
        {
            _logger.Information("Running hourly scheduled tasks.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

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

            _logger.Information("Checking for chao to evolve...");
            await RunFirstEvolutions();

            _logger.Information("Clearing expired instance bans...");
            await _repo.ClearExpiredRaceInstanceBans();

            _logger.Information("Reincarnating eligible NPCs...");
            await _repo.ReincarnateEligibleNpcChao();

            _logger.Information("Recalculating instance prize amounts...");
            await _repo.RecalculateRaceRewards();
            await _repo.RecalculateTournamentRewards();

            stopwatch.Stop();
            _logger.Information("Ran hourly scheduled tasks in {Time}", stopwatch.ElapsedDuration());
        }

        private async Task<List<MarketItem>> MakeMarketListings()
        {
            var items = new List<MarketItem>();

            // Common eggs (non-shiny monotone)
            var commonEggLimit = new Random().Next(1, 5); // Always have at least one egg in the market
            var commonEggs = await _repo.GetMarketEnabledEggs(commonEggLimit, false);
            foreach (var egg in commonEggs)
            {
                egg.Quantity = 1;
                items.Add(egg);
            }

            // Rare eggs (shiny monotone)
            var uncommonEggLimit = new Random().Next(1, 4) == 1 ? 1 : 0; // Only have shiny eggs available every few hours
            var rareEggs = await _repo.GetMarketEnabledEggs(uncommonEggLimit, true);
            foreach (var egg in rareEggs)
            {
                egg.Quantity = 1;
                items.Add(egg);
            }

            // Common fruit (e.g. tasty, round, 
            var commonFruitLimit = new Random().Next(3, 5); // Always have some fruit on the market
            var commonFruit = await _repo.GetMarketEnabledFruit(commonFruitLimit, false);
            foreach (var fruit in commonFruit)
            {
                fruit.Quantity = new Random().Next(3, 5);
                items.Add(fruit);
            }

            var rareFruitLimit = new Random().Next(1, 24) == 1 ? 1 : 0; // Only have hyper fruit available roughly once per day
            var rareFruit = await _repo.GetMarketEnabledFruit(rareFruitLimit, true);
            foreach (var fruit in rareFruit)
            {
                fruit.Quantity = 1;
                items.Add(fruit);
            }

            var specialLimit = new Random().Next(1, 8) == 1 ? 1 : 0; // Have special items available a few times per day
            var specials = await _repo.GetMarketEnabledSpecials(specialLimit);
            foreach (var special in specials)
            {
                special.Quantity = 1;
                items.Add(special);
            }

            return items;
        }

        // we don't have access to ChaoWorld.Bot here, so this needs to be vendored
        public static ulong InstantToSnowflake(Instant time) =>
            (ulong)(time - Instant.FromUtc(2015, 1, 1, 0, 0, 0)).TotalMilliseconds << 22;

    }
}