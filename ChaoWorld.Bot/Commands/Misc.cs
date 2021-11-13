using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using App.Metrics;

using Humanizer;

using NodaTime;

using ChaoWorld.Core;

using Myriad.Builders;
using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Gateway;
using Myriad.Rest;
using Myriad.Rest.Exceptions;
using Myriad.Rest.Types.Requests;
using Myriad.Types;

namespace ChaoWorld.Bot
{
    public class Misc
    {
        private readonly BotConfig _botConfig;
        private readonly IMetrics _metrics;
        private readonly CpuStatService _cpu;
        private readonly ShardInfoService _shards;
        private readonly EmbedService _embeds;
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IDiscordCache _cache;
        private readonly DiscordApiClient _rest;
        private readonly Cluster _cluster;
        private readonly Bot _bot;

        public Misc(BotConfig botConfig, IMetrics metrics, CpuStatService cpu, ShardInfoService shards, EmbedService embeds, ModelRepository repo,
                                IDatabase db, IDiscordCache cache, DiscordApiClient rest, Bot bot, Cluster cluster)
        {
            _botConfig = botConfig;
            _metrics = metrics;
            _cpu = cpu;
            _shards = shards;
            _embeds = embeds;
            _repo = repo;
            _db = db;
            _cache = cache;
            _rest = rest;
            _bot = bot;
            _cluster = cluster;
        }

        public async Task Invite(Context ctx)
        {
            var clientId = _botConfig.ClientId ?? _cluster.Application?.Id;

            var permissions =
                PermissionSet.AddReactions |
                PermissionSet.AttachFiles |
                PermissionSet.EmbedLinks |
                PermissionSet.ManageMessages |
                PermissionSet.ManageWebhooks |
                PermissionSet.ReadMessageHistory |
                PermissionSet.SendMessages;

            var invite = $"https://discord.com/oauth2/authorize?client_id={clientId}&scope=bot%20applications.commands&permissions={(ulong)permissions}";
            await ctx.Reply($"{Emojis.Success} Use this link to add Chao World to your server:\n<{invite}>");
        }

        public async Task Collect(Context ctx)
        {
            ctx.CheckGarden();

            var now = SystemClock.Instance.GetCurrentInstant();
            if (ctx.Garden.NextCollectOn < now)
            {
                var maxLuck = await _repo.GetHighestLuckInGarden(ctx.Garden.Id);
                var ringsFound = new System.Random().Next(100, 1000 + maxLuck/10);
                ctx.Garden.RingBalance += ringsFound;
                var duration = Duration.FromDays(1);
                ctx.Garden.NextCollectOn = now.Plus(duration);
                await _repo.UpdateGarden(ctx.Garden);
                await ctx.Reply($"{Emojis.Success} You found {ringsFound} rings! Your current balance is {ctx.Garden.RingBalance}.");
            } else {
                var duration = ctx.Garden.NextCollectOn - now;
                var timeRemaining = "24 hours";
                if (duration.Hours >= 2)
                    timeRemaining = $"{duration.Hours} hours";
                else if (duration.Hours >= 1)
                    timeRemaining = $"hour";
                else if (duration.Minutes >= 2)
                    timeRemaining = $"{duration.Minutes} minutes";
                else if (duration.Minutes >= 1)
                    timeRemaining = $"minute";
                else
                    timeRemaining = $"{duration.Seconds} seconds";

                await ctx.Reply($"{Emojis.Error} You couldn't find anything. Please wait another {timeRemaining} to collect rings.");
            }
        }

        public async Task Stats(Context ctx)
        {
            var timeBefore = SystemClock.Instance.GetCurrentInstant();
            var msg = await ctx.Reply($"...");
            var timeAfter = SystemClock.Instance.GetCurrentInstant();
            var apiLatency = timeAfter - timeBefore;

            var messagesReceived = _metrics.Snapshot.GetForContext("Bot").Meters.FirstOrDefault(m => m.MultidimensionalName == BotMetrics.MessagesReceived.Name)?.Value;
            var messagesProxied = _metrics.Snapshot.GetForContext("Bot").Meters.FirstOrDefault(m => m.MultidimensionalName == BotMetrics.MessagesProxied.Name)?.Value;
            var commandsRun = _metrics.Snapshot.GetForContext("Bot").Meters.FirstOrDefault(m => m.MultidimensionalName == BotMetrics.CommandsRun.Name)?.Value;

            var counts = await _repo.GetStats();

            var shardId = ctx.Shard.ShardId;
            var shardTotal = ctx.Cluster.Shards.Count;
            var shardUpTotal = _shards.Shards.Where(x => x.Connected).Count();
            var shardInfo = _shards.GetShardInfo(ctx.Shard);

            var process = Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64;

            var now = SystemClock.Instance.GetCurrentInstant();
            var shardUptime = now - shardInfo.LastConnectionTime;

            var embed = new EmbedBuilder();
            if (messagesReceived != null) embed.Field(new("Messages processed", $"{messagesReceived.OneMinuteRate * 60:F1}/m ({messagesReceived.FifteenMinuteRate * 60:F1}/m over 15m)", true));
            if (commandsRun != null) embed.Field(new("Commands executed", $"{commandsRun.OneMinuteRate * 60:F1}/m ({commandsRun.FifteenMinuteRate * 60:F1}/m over 15m)", true));

            embed
                .Field(new("Current shard", $"Shard #{shardId} (of {shardTotal} total, {shardUpTotal} are up)", true))
                .Field(new("Shard uptime", $"{shardUptime.FormatDuration()} ({shardInfo.DisconnectionCount} disconnections)", true))
                .Field(new("CPU usage", $"{_cpu.LastCpuMeasure:P1}", true))
                .Field(new("Memory usage", $"{memoryUsage / 1024 / 1024} MiB", true))
                .Field(new("Latency", $"API: {apiLatency.TotalMilliseconds:F0} ms, shard: {shardInfo.ShardLatency.Milliseconds} ms", true))
                .Field(new("Total numbers", $"{counts.GardenCount:N0} gardens"
                    + $"\n{counts.ChaoCount:N0} chao"))
                .Timestamp(process.StartTime.ToString("O"))
                .Footer(new($"Chao World {BuildInfoService.Version} • Last restarted: ")); ;
            await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                new MessageEditRequest { Content = "", Embed = embed.Build() });
        }

    }
}