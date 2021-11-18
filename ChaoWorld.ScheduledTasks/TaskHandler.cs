using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;
using NodaTime.Extensions;

using Serilog;

using ChaoWorld.Core;
using System.Collections.Generic;
using Myriad.Rest;
using Myriad.Types;
using Myriad.Rest.Types;
using Myriad.Rest.Types.Requests;

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

            stopwatch.Stop();
            _logger.Information("Ran per-minute scheduled tasks in {Time}", stopwatch.ElapsedDuration());
        }

        private async Task UpdateHourly()
        {
            _logger.Information("Running hourly scheduled tasks.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            stopwatch.Stop();
            _logger.Information("Ran hourly scheduled tasks in {Time}", stopwatch.ElapsedDuration());
        }

        // we don't have access to ChaoWorld.Bot here, so this needs to be vendored
        public static ulong InstantToSnowflake(Instant time) =>
            (ulong)(time - Instant.FromUtc(2015, 1, 1, 0, 0, 0)).TotalMilliseconds << 22;
    }
}