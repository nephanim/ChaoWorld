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
                var ringsFound = new Random().Next(100 + maxLuck/10, 1000 + maxLuck);
                ctx.Garden.RingBalance += ringsFound;
                var duration = Duration.FromHours(23);
                ctx.Garden.NextCollectOn = now.Plus(duration);
                await _repo.UpdateGarden(ctx.Garden);
                await ctx.Reply($"{Emojis.Success} You found {ringsFound:n0} rings! Your current balance is {ctx.Garden.RingBalance:n0}.");
            } else {
                var duration = ctx.Garden.NextCollectOn - now;
                var timeRemaining = "24 hours";
                if (duration.TotalHours >= 2)
                    timeRemaining = $"{duration.TotalHours:N0} hours";
                else if (duration.TotalHours >= 1)
                    timeRemaining = $"1 hour {(duration.TotalMinutes - 60):N0} minutes";
                else if (duration.TotalMinutes >= 2)
                    timeRemaining = $"{duration.TotalMinutes:N0} minutes";
                else if (duration.TotalMinutes >= 1)
                    timeRemaining = $"1 minute {duration.TotalSeconds:N0} seconds";
                else
                    timeRemaining = $"{duration.TotalSeconds:N0} seconds";

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
            var bet = 100;
            if (int.TryParse(betInput, out int newBet))
                bet = newBet;

            var machine = new SlotMachine(_repo, _embeds, bet)
            {
                IsSimulated = true
            };
            await machine.Simulate(ctx);
        }

        public async Task PlaySlots(Context ctx)
        {
            ctx.CheckGarden();
            var betInput = ctx.RemainderOrNull();
            var bet = 100; // Default this to 100, but let them go higher if they want... (this should reduce slots spam somewhat)
            if (int.TryParse(betInput, out int newBet))
                bet = newBet;

            var machine = new SlotMachine(_repo, _embeds, bet);
            await machine.Play(ctx);
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

    public class SlotMachine
    {
        public bool IsJackpot { get; set; }
        public bool IsSimulated { get; set; }
        public double PayoutMultiplier
        {
            get
            {
                return _betAmount / 100.0;
            }
        }
        public double PayoutAmount { get; set; }

        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly int _betAmount;

        public SlotMachine(ModelRepository repo, EmbedService embeds, int betAmount)
        {
            _repo = repo;
            _embeds = embeds;
            _betAmount = betAmount;
        }

        public async Task Play(Context ctx)
        {
            if (ctx.Garden.RingBalance < _betAmount)
            {
                await ctx.Reply($"{Emojis.Error} Sorry, you don't have enough rings to play Chao Slots! You need at least {_betAmount:n0} rings.");
            }
            else if (_betAmount < 100)
            {
                await ctx.Reply($"{Emojis.Error} Sorry, the minimum bet for Chao Slots is 100 rings.");
            }
            else
            {
                ctx.Garden.RingBalance -= _betAmount; // Pay up first...
                await _repo.UpdateJackpot(_betAmount / 20); // A portion of the bet goes into the jackpot pool (intentionally less so they never just win back everything)

                var tiles = new string[]
                {
                    Emojis.Slots1, Emojis.Slots8, Emojis.Slots4,
                    Emojis.Slots2, Emojis.Slots9, Emojis.Slots5,
                    Emojis.Slots3, Emojis.Slots0, Emojis.Slots6
                };
                var msg = await ctx.Reply($"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}");

                // Stop first wheel
                await Task.Delay(500);
                tiles[0] = PickSlotResult();
                tiles[3] = PickSlotResult();
                tiles[6] = PickSlotResult();
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest
                    {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });

                // Stop second wheel
                await Task.Delay(300);
                tiles[1] = PickSlotResult();
                tiles[4] = PickSlotResult();
                tiles[7] = PickSlotResult();
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest
                    {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });

                // Stop last wheel
                await Task.Delay(300);
                tiles[2] = PickSlotResult();
                tiles[5] = PickSlotResult();
                tiles[8] = PickSlotResult();
                await ctx.Rest.EditMessage(msg.ChannelId, msg.Id,
                    new MessageEditRequest
                    {
                        Content = $"{tiles[0]} {tiles[1]} {tiles[2]}\r\n{tiles[3]} {tiles[4]} {tiles[5]}\r\n{tiles[6]} {tiles[7]} {tiles[8]}",
                        Embed = null
                    });

                // Now determine payout
                var payout = await GetSlotPayout(ctx, tiles);
                ctx.Garden.RingBalance += payout;
                await _repo.UpdateGarden(ctx.Garden);

                if (payout > _betAmount)
                    await ctx.Reply($"{Emojis.Success} {ctx.Author.Username} won {payout:n0} rings playing Chao Slots! (+{payout - _betAmount:n0})");
                else if (payout == _betAmount)
                    await ctx.Reply($"{Emojis.Success} {ctx.Author.Username} broke even on Chao Slots. (+0)");
                else if (payout > 0)
                    await ctx.Reply($"{Emojis.Eggman} {ctx.Author.Username} won back some of the rings they put in. Better luck next time. (-{_betAmount - payout:n0})");
                else
                    await ctx.Reply($"{Emojis.Eggman} {ctx.Author.Username} didn't win anything... (-{_betAmount:n0})");
            }
        }

        public async Task Simulate(Context ctx)
        {
            IsSimulated = true;

            var balance = 100000000; // Start with a huge amount of money
            var startingBalance = balance;
            var simulationCount = 1000000; // Run a huge number of trials

            for (int i = 0; i < simulationCount; i++)
            {
                balance -= _betAmount;
                var tiles = new string[]
                {
                    PickSlotResult(), PickSlotResult(), PickSlotResult(),
                    PickSlotResult(), PickSlotResult(), PickSlotResult(),
                    PickSlotResult(), PickSlotResult(), PickSlotResult(),
                };

                var payout = await GetSlotPayout(ctx, tiles);
                balance += payout;
            }
            await ctx.Reply($"{Emojis.Note} After {simulationCount:n0} plays (betting {_betAmount:n0} rings), balance is {balance:n0} ({((double)balance) / startingBalance * 100.0:N0}% return)");
        }

        private static string PickSlotResult()
        {
            var roll = new Random().Next(0, 1001);
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
            // Something went wrong if we don't have the number of tiles we expected
            if (tiles.Length < 9)
                return 0;

            // Adjust payout based on rings/eggmen
            AddPayoutForRings(tiles);
            SubtractPayoutForEggmen(tiles);

            if (tiles[0] == tiles[1] && tiles[1] == tiles[2]) // Top row match
                await AddPayoutForRow(tiles[0]);
            if (tiles[3] == tiles[4] && tiles[4] == tiles[5]) // Middle row match
                await AddPayoutForRow(tiles[3]);
            if (tiles[6] == tiles[7] && tiles[7] == tiles[8]) // Bottom row match
                await AddPayoutForRow(tiles[6]);
            if (tiles[0] == tiles[4] && tiles[4] == tiles[8]) // Diagonal \
                await AddPayoutForRow(tiles[0]);
            if (tiles[2] == tiles[4] && tiles[4] == tiles[6]) // Diagonal /
                await AddPayoutForRow(tiles[2]);
            if (tiles[0] == tiles[3] && tiles[3] == tiles[6]) // First column match
                await AddPayoutForRow(tiles[0]);
            if (tiles[1] == tiles[4] && tiles[4] == tiles[7]) // Second column match
                await AddPayoutForRow(tiles[1]);
            if (tiles[2] == tiles[5] && tiles[5] == tiles[8]) // Third column match
                await AddPayoutForRow(tiles[2]);

            if (IsJackpot)
            {
                // Note that this won't be triggered during simulations - someone actually hit the jackpot
                await _repo.ResetJackpot();
                await ctx.Reply(embed: await _embeds.CreateJackpotEmbed(ctx));
            }

            var payout = (int)Math.Floor(PayoutAmount);
            if (payout < 0)
                payout = 0;
            return payout;
        }

        private void AddPayoutForRings(string[] tiles)
        {
            var ringCount = tiles.Count(x => x == Emojis.Rings); // Rings are rare and guarantee a base payout per ring
            PayoutAmount += (1111.111 * ringCount * PayoutMultiplier);
        }

        private void SubtractPayoutForEggmen(string[] tiles)
        {
            var eggmanCount = tiles.Count(x => x == Emojis.Eggman); // On the other hand, the eggman logo guarantees a base loss per eggman
            PayoutAmount -= (300 * eggmanCount * PayoutMultiplier);
        }

        private async Task AddPayoutForRow(string tile)
        {
            switch (tile)
            {
                case Emojis.Rings:
                    if (IsSimulated) // We don't want simulations to include the jackpot because it's such an extreme edge case
                        break;
                    if (IsJackpot) // The jackpot was triggered by more than one row... Incredible, but just ignore it because we already added the jackpot payout
                        break;
                    IsJackpot = true;
                    var jackpot = await _repo.GetJackpot();
                    PayoutAmount += jackpot; // This doesn't use the payout multiplier, otherwise we'd see some stupid numbers and it would disincentivize low bets
                    break;
                case Emojis.GoldEgg:
                    PayoutAmount += 3333.333 * PayoutMultiplier;
                    break;
                case Emojis.YellowFruit:
                    PayoutAmount += 888.888 * PayoutMultiplier;
                    break;
                case Emojis.PinkFruit:
                case Emojis.BlueFruit:
                case Emojis.GreenFruit:
                    PayoutAmount += 666.666 * PayoutMultiplier;
                    break;
                case Emojis.OrangeFruit:
                case Emojis.PurpleFruit:
                case Emojis.RedFruit:
                    PayoutAmount += 222.222 * PayoutMultiplier;
                    break;
                case Emojis.Eggman:
                    PayoutAmount += -9000 * PayoutMultiplier;
                    break;
                default:
                    break;
            }
        }
    }
}