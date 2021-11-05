using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Myriad.Builders;
using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Rest;
using Myriad.Rest.Exceptions;
using Myriad.Types;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class EmbedService
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IDiscordCache _cache;
        private readonly DiscordApiClient _rest;

        public EmbedService(IDatabase db, ModelRepository repo, IDiscordCache cache, DiscordApiClient rest)
        {
            _db = db;
            _repo = repo;
            _cache = cache;
            _rest = rest;
        }

        public async Task<(ulong Id, User? User)[]> GetUsers(IEnumerable<ulong> ids)
        {
            async Task<(ulong Id, User? User)> Inner(ulong id)
            {
                var user = await _cache.GetOrFetchUser(_rest, id);
                return (id, user);
            }

            return await Task.WhenAll(ids.Select(Inner));
        }

        public async Task<Embed> CreateGardenEmbed(Context cctx, Core.Garden garden)
        {

            // Fetch/render info for all accounts simultaneously
            var accounts = await _repo.GetGardenAccounts(garden.Id);
            var users = (await GetUsers(accounts)).Select(x => x.User?.Username ?? $"({x.Id})");
            var firstUser = garden.Id.Value == 0 ? "Professor Chao" : users.FirstOrDefault();

            var chaoCount = await _repo.GetGardenChaoCount(garden.Id);
            var activeChao = await _repo.GetActiveChaoForGarden(garden.Id.Value);

            var eb = new EmbedBuilder()
                .Title($"{firstUser}'s Garden")
                .Footer(new($"Garden ID: {garden.Id} | Created on {garden.CreatedOn}"));

            eb.Field(new("Rings", string.Format("{0:n0}", garden.RingBalance), true));
            eb.Field(new("Active Chao", activeChao != null ? activeChao.Name : "(not set)"));

            if (chaoCount > 0)
                eb.Field(new($"Chao ({chaoCount})", $"(see `!garden {garden.Id} list` or `!garden {garden.Id} list full`)", true));
            else
                eb.Field(new($"Chao ({chaoCount})", "Add one with `!chao new`!", true));

            return eb.Build();
        }

        public async Task<Embed> CreateChaoEmbed(Core.Garden garden, Core.Chao chao)
        {
            var name = chao.Name;
            var raceStats = await _repo.GetRaceStats(chao.Id.Value);
            var totalRaces = raceStats != null ? string.Format("{0:n0}", raceStats.TotalRaces) : "0";
            var totalWins = raceStats != null ? string.Format("{0:n0}", raceStats.TotalWins) : "0";
            var totalRetires = raceStats != null ? string.Format("{0:n0}", raceStats.TotalRetires) : "0";
            var winRate = raceStats != null ? raceStats.WinRate.ToString("N2") : "0";
            var retireRate = raceStats != null ? raceStats.RetireRate.ToString("N2") : "0";
            var imageUrl = MiscUtils.GenerateThumbnailForChao(chao);

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
                //.Image(new(imageUrl))
                .Description(chao.Appearance)
                .Footer(new(
                    $"Garden ID: {garden.Id} | Chao ID: {chao.Id} {$"| Created on {chao.CreatedOn.FormatZoned(DateTimeZone.Utc)}"}"));

            eb.Field(new("__General Info:__",
                $"Age: {chao.Age}\r\n" +
                $"Reincarnations: {chao.Reincarnations}"
            ));
            eb.Field(new($"__Abilities:__",
                $"**Swim** (Lv.{chao.SwimLevel:D2})\r\n{chao.SwimGrade} • {chao.SwimProgress:D2}/100 ({chao.SwimValue:D4})\r\n"
                + $"**Fly** (Lv.{chao.FlyLevel:D2})\r\n{chao.FlyGrade} • {chao.FlyProgress:D2}/100 ({chao.FlyValue:D4})\r\n"
                + $"**Run** (Lv.{chao.RunLevel:D2})\r\n{chao.RunGrade} • {chao.RunProgress:D2}/100 ({chao.RunValue:D4})\r\n"
                + $"**Power** (Lv.{chao.PowerLevel:D2})\r\n{chao.PowerGrade} • {chao.PowerProgress:D2}/100 ({chao.PowerValue:D4})\r\n"
                + $"**Stamina** (Lv.{chao.StaminaLevel:D2})\r\n{chao.StaminaGrade} • {chao.StaminaProgress:D2}/100 ({chao.StaminaValue:D4})\r\n"
                + $"**Intelligence** (Lv.{chao.IntelligenceLevel:D2})\r\n{chao.IntelligenceGrade} • {chao.IntelligenceProgress:D2}/100 ({chao.IntelligenceValue:D4})\r\n"
                + $"**Luck** (Lv.{chao.LuckLevel:D2})\r\n{chao.LuckGrade} • {chao.LuckProgress:D2}/100 ({chao.LuckValue:D4})"
            ));
            eb.Field(new($"__Race Stats:__",
                $"Races: {totalRaces}\r\n" +
                $"Wins: {totalWins} ({winRate}%)\r\n" +
                $"Retires: {totalRetires} ({retireRate}%)"
            ));
            eb.Color(await GetDiscordColor(chao));

            return eb.Build();
        }

        public async Task<Embed> CreateRaceEmbed(Core.Race race, RaceInstance raceInstance)
        {
            var name = race.Name;
            var difficulty = Core.Race.GetDifficultyString(race.Difficulty);
            var participants = await _repo.GetRaceInstanceChaoCount(raceInstance.Id);

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Description(race.Description)
                .Footer(new(
                    $"Instance ID: {raceInstance.Id} | Created on {raceInstance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            eb.Field(new("Status", raceInstance.State.GetDescription()));
            eb.Field(new("Difficulty", difficulty));
            eb.Field(new("Participants", $"{participants} / {race.MaximumChao}"));

            if (raceInstance.WinnerChaoId.HasValue && raceInstance.TimeElapsedSeconds.HasValue)
            {
                var timeElapsed = TimeSpan.FromSeconds(raceInstance.TimeElapsedSeconds.GetValueOrDefault(0)).ToString("c");
                var winner = await _repo.GetChao(raceInstance.WinnerChaoId.GetValueOrDefault(0));
                if (winner != null)
                {
                    eb.Field(new("Winner", $"{winner.Name}"));
                    eb.Field(new("Time", timeElapsed));
                }
            }

            var composition = string.Empty;
            if (race.SwimPercentage > 0)
                composition += $"Swimming: {Math.Round(race.SwimPercentage * 100, 0)}%\r\n";
            if (race.FlyPercentage > 0)
                composition += $"Flying: {Math.Round(race.FlyPercentage * 100, 0)}%\r\n";
            if (race.RunPercentage > 0)
                composition += $"Running: {Math.Round(race.RunPercentage * 100, 0)}%\r\n";
            if (race.PowerPercentage > 0)
                composition += $"Climbing: {Math.Round(race.PowerPercentage * 100, 0)}%\r\n";
            if (race.IntelligencePercentage > 0)
                composition += $"Puzzles: {Math.Round(race.IntelligencePercentage * 100, 0)}%\r\n";
            if (race.LuckPercentage > 0)
                composition += $"Traps: {Math.Round(race.LuckPercentage * 100, 0)}%\r\n";
            eb.Field(new("Race Composition", composition));

            return eb.Build();
        }

        public async Task<Embed> CreateRaceProgressEmbed(Core.Race race, RaceInstance raceInstance, RaceSegment segment, TimeSpan timeElapsed, IEnumerable<RaceProgressListItem> chao)
        {
            var name = $"Race Progress: {race.Name}";
            var orderedChao = chao.OrderBy(x => x.Position);
            var desc = $"{segment.Description}";

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Description(desc)
                .Footer(new(
                    $"Instance ID: {raceInstance.Id} | Segment: {segment.RaceIndex} | Created on {raceInstance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            var elapsed = timeElapsed.ToString("c");
            eb.Field(new("Time Elapsed", elapsed));
            var elapsedSeconds = timeElapsed.TotalSeconds;

            var roster = string.Empty;
            foreach (var c in chao)
            {
                var status = string.Empty;
                var lagTime = string.Empty;
                if (c.Status == RaceInstanceChaoSegment.SegmentStates.Retired)
                    status = " :x: (Retired)";
                else if (c.TotalTimeSeconds > elapsedSeconds)
                    lagTime = $" *+{c.TotalTimeSeconds - elapsedSeconds}s*";

                roster += $"#{c.Position}. {c.ChaoName}{lagTime}{status}\r\n";
            }
            eb.Field(new("Chao", roster));

            return eb.Build();
        }

        private async Task<uint> GetDiscordColor(Core.Chao chao)
        {
            switch (chao.PrimaryColor)
            {
                case Core.Chao.Colors.Normal:
                case Core.Chao.Colors.Blue:
                case Core.Chao.Colors.SkyBlue:
                case Core.Chao.Colors.Aquamarine:
                case Core.Chao.Colors.Moon:
                case Core.Chao.Colors.Sapphire:
                case Core.Chao.Colors.Topaz:
                    return 3447003; // BLUE
                case Core.Chao.Colors.Black:
                    return 2303786; // BLACK
                case Core.Chao.Colors.Brown:
                case Core.Chao.Colors.Bronze:
                    return 11027200; // DARK ORANGE
                case Core.Chao.Colors.Amethyst:
                case Core.Chao.Colors.Purple:
                    return 10181046; // PURPLE
                case Core.Chao.Colors.Emerald:
                case Core.Chao.Colors.Green:
                case Core.Chao.Colors.LimeGreen:
                case Core.Chao.Colors.Peridot:
                    return 3066993; // GREEN
                case Core.Chao.Colors.Glass:
                case Core.Chao.Colors.Grey:
                case Core.Chao.Colors.Metal:
                case Core.Chao.Colors.Silver:
                    return 9807270; // GREY
                case Core.Chao.Colors.Garnet:
                case Core.Chao.Colors.Yellow:
                    return 16776960; // YELLOW
                case Core.Chao.Colors.Gold:
                    return 15844367; // GOLD
                case Core.Chao.Colors.Orange:
                    return 15105570; // ORANGE
                case Core.Chao.Colors.Pearl:
                case Core.Chao.Colors.White:
                    return 16777215; // WHITE
                case Core.Chao.Colors.Pink:
                    return 15277667; // LUMINOUS VIVID PINK
                case Core.Chao.Colors.Red:
                case Core.Chao.Colors.Ruby:
                    return 15158332; // RED
                default:
                    return 0; // DEFAULT
            }
        }
    }
}