using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;
using NodaTime.Extensions;

using Serilog;

using ChaoWorld.Core;

namespace ChaoWorld.ScheduledTasks
{
    public class TaskHandler
    {
        private Timer _periodicTask;

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
                var __ = UpdatePeriodic();
            }, null, timeTillNextWholeMinute, TimeSpan.FromMinutes(1));
        }

        private async Task UpdatePeriodic()
        {
            _logger.Information("Running per-minute scheduled tasks.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _logger.Information("Updating database stats...");
            await _repo.UpdateStats();

            _logger.Information("Updating race information...");
            await _repo.InstantiateRaces();

            stopwatch.Stop();
            _logger.Information("Ran scheduled tasks in {Time}", stopwatch.ElapsedDuration());
        }

        // we don't have access to ChaoWorld.Bot here, so this needs to be vendored
        public static ulong InstantToSnowflake(Instant time) =>
            (ulong)(time - Instant.FromUtc(2015, 1, 1, 0, 0, 0)).TotalMilliseconds << 22;

    }
}