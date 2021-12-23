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
using System;

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
                await ctx.Reply($"{Emojis.Success} You found {ringsFound:n0} rings! Your current balance is {ctx.Garden.RingBalance:n0}.");
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

        public async Task SeeJackpot(Context ctx)
        {
            var jackpot = await _repo.GetJackpot();
            await ctx.Reply($"{Emojis.Note} The jackpot is currently {jackpot:n0} rings. Maybe today's your lucky day!");
        }

        public async Task SimulateSlots(Context ctx)
        {
            var betInput = ctx.RemainderOrNull();
            var bet = 100; // Default this to 100, but let them go higher if they want... (this should reduce slots spam somewhat)
            if (int.TryParse(betInput, out int newBet))
                bet = newBet;
            var payoutMultiplier = bet / 100.0;

            var balance = 10000000;
            var startingBalance = balance;
            var simulationCount = 1000000;

            for (int i = 0; i < simulationCount; i++)
            {
                balance -= bet;
                var tiles = new string[]
                {
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                };

                var payout = (int)Math.Floor((await GetSlotPayout(ctx, tiles)) * payoutMultiplier);
                balance += payout;
            }
            await ctx.Reply($"{Emojis.Note} After {simulationCount} plays (betting {bet} rings), balance is {balance:n0} ({((double)balance)/startingBalance*100.0:N0}% return)");
        }

        public async Task PlaySlots(Context ctx)
        {
            ctx.CheckGarden();
            var betInput = ctx.RemainderOrNull();
            var bet = 100; // Default this to 100, but let them go higher if they want... (this should reduce slots spam somewhat)
            if (int.TryParse(betInput, out int newBet))
                bet = newBet;

            if (ctx.Garden.RingBalance < bet)
            {
                await ctx.Reply($"{Emojis.Error} Sorry, you don't have enough rings to play Chao Slots! You need at least 100 rings.");
            }
            else if (bet < 100)
            {
                await ctx.Reply($"{Emojis.Error} Sorry, the minimum bet for Chao Slots is 100 rings.");
            }
            else
            {
                ctx.Garden.RingBalance -= bet; // Pay 100 rings to play
                await _repo.UpdateJackpot(bet / 20); // A portion of the bet goes into the jackpot pool (intentionally less so they never just win back everything)
                var payoutMultiplier = bet / 100.0;

                var tiles = new string[]
                {
                    PickSlotResult(new Random().Next(30, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(150, 1001)),
                    PickSlotResult(new Random().Next(100, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(200, 1001)), PickSlotResult(new Random().Next(100, 1001)),
                };
                var msg = await ctx.Reply($"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}");

                // Shuffle the tiles so we can give the "slots" effect (some of these are deliberately more likely to show "exciting" rewards to keep them playing)
                tiles = new string[]
                {
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(200, 1001)), PickSlotResult(new Random().Next(50, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(50, 1001)), PickSlotResult(new Random().Next(100, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                };
                await Task.Delay(300);
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest
                    {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });
                tiles = new string[]
                {
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                    PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)), PickSlotResult(new Random().Next(0, 1001)),
                };
                await Task.Delay(300);
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest
                    {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });

                var payout = (int)Math.Floor((await GetSlotPayout(ctx, tiles)) * payoutMultiplier);
                ctx.Garden.RingBalance += payout;
                await _repo.UpdateGarden(ctx.Garden);
                
                await Task.Delay(300);
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });

                if (payout > bet)
                    await ctx.Reply($"{Emojis.Success} {ctx.Author.Username} won {payout:n0} rings playing Chao Slots! (+{payout-bet:n0})");
                else if (payout == bet)
                    await ctx.Reply($"{Emojis.Success} {ctx.Author.Username} broke even on Chao Slots. (+0)");
                else if (payout > 0)
                    await ctx.Reply($"{Emojis.Eggman} {ctx.Author.Username} won back some of the rings they put in. Better luck next time. (+{bet-payout:n0})");
                else
                    await ctx.Reply($"{Emojis.Eggman} {ctx.Author.Username} didn't win anything... (-{bet:n0})");
            }
        }

        private string PickSlotResult(int roll)
        {
            if (roll > 995)
                return Emojis.Rings;
            if (roll > 930)
                return Emojis.GoldEgg;
            if (roll > 870)
                return Emojis.BlueFruit;
            if (roll > 810)
                return Emojis.GreenFruit;
            if (roll > 750)
                return Emojis.PinkFruit;
            if (roll > 555)
                return Emojis.OrangeFruit;
            if (roll > 350)
                return Emojis.PurpleFruit;
            if (roll > 127)
                return Emojis.RedFruit;
            if (roll > 40)
                return Emojis.YellowFruit;
            return Emojis.Eggman;
        }

        private async Task<int> GetSlotPayout(Context ctx, string[] tiles)
        {
            var payout = 0;

            // Something went wrong if we don't have the number of tiles we expected
            if (tiles.Length < 9)
                return payout;

            var ringCount = tiles.Count(x => x == Emojis.Rings); // Rings are rare and guarantee a base payout per ring
            payout += 1111 * ringCount;

            var eggmanCount = tiles.Count(x => x == Emojis.Eggman); // On the other hand, the eggman logo guarantees a base loss per eggman
            payout -= 300 * eggmanCount;

            var rowPayout = 0;
            if (tiles[0] == tiles[1] && tiles[1] == tiles[2]) // Top row match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[0]));
            if (tiles[3] == tiles[4] && tiles[4] == tiles[5]) // Middle row match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[3]));
            if (tiles[6] == tiles[7] && tiles[7] == tiles[8]) // Bottom row match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[6]));
            if (tiles[0] == tiles[4] && tiles[4] == tiles[8]) // Diagonal \
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[0]));
            if (tiles[2] == tiles[4] && tiles[4] == tiles[6]) // Diagonal /
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[2]));
            if (tiles[0] == tiles[3] && tiles[3] == tiles[6]) // First column match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[0]));
            if (tiles[1] == tiles[4] && tiles[4] == tiles[7]) // Second column match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[1]));
            if (tiles[2] == tiles[5] && tiles[5] == tiles[8]) // Third column match
                rowPayout += Math.Max(rowPayout, await GetSlotRateForRow(ctx, tiles[2]));

            payout += rowPayout;
            if (payout < 0)
                payout = 0;
            return payout;
        }

        private async Task<int> GetSlotRateForRow(Context ctx, string tile)
        {
            switch (tile)
            {
                case Emojis.Rings:
                    var jackpot = await _repo.GetJackpot();
                    await _repo.ResetJackpot();
                    await ctx.Reply($"{Emojis.Rings} {Emojis.Rings} {Emojis.Rings} {ctx.Author.Username} hit the jackpot! Rings pour out of the slot machine like a waterfall. {Emojis.Rings} {Emojis.Rings} {Emojis.Rings}");
                    return jackpot;
                case Emojis.GoldEgg:
                    return 3333;
                case Emojis.YellowFruit:
                    return 888;
                case Emojis.PinkFruit:
                case Emojis.BlueFruit:
                case Emojis.GreenFruit:
                    return 666;
                case Emojis.OrangeFruit:
                case Emojis.PurpleFruit:
                case Emojis.RedFruit:
                    return 222;
                case Emojis.Eggman:
                    return -9000;
                default:
                    return 0;
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