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

            eb.Field(new("Rings", garden.RingBalance.ToString(), true));
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

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new("https://chao-island.com/assets/images/gallery/twotone/child/childtwotoneregular.jpg"))
                .Description(chao.Appearance)
                .Footer(new(
                    $"Garden ID: {garden.Id} | Chao ID: {chao.Id} {$"| Created on {chao.CreatedOn.FormatZoned(DateTimeZone.Utc)}"}"));

            eb.Field(new("Age", chao.Age.ToString()));
            eb.Field(new("Reincarnations", chao.Reincarnations.ToString()));
            eb.Field(new($"Swim (Lv.{chao.SwimLevel:D2})", $"{chao.GetEmojiGrade(chao.SwimGrade)} : {chao.SwimProgress:D2}/100 ({chao.SwimValue:D4})"));
            eb.Field(new($"Fly (Lv.{chao.FlyLevel:D2})", $"{chao.GetEmojiGrade(chao.FlyGrade)} : {chao.FlyProgress:D2}/100 ({chao.FlyValue:D4})"));
            eb.Field(new($"Run (Lv.{chao.RunLevel:D2})", $"{chao.GetEmojiGrade(chao.RunGrade)} : {chao.RunProgress:D2}/100 ({chao.RunValue:D4})"));
            eb.Field(new($"Power (Lv.{chao.PowerLevel:D2})", $"{chao.GetEmojiGrade(chao.PowerGrade)} : {chao.PowerProgress:D2}/100 ({chao.PowerValue:D4})"));
            eb.Field(new($"Stamina (Lv.{chao.StaminaLevel:D2})", $"{chao.GetEmojiGrade(chao.StaminaGrade)} : {chao.StaminaProgress:D2}/100 ({chao.StaminaValue:D4})"));
            eb.Field(new($"Intelligence (Lv.{chao.IntelligenceLevel:D2})", $"{chao.GetEmojiGrade(chao.IntelligenceGrade)} : {chao.IntelligenceProgress:D2}/100 ({chao.IntelligenceValue:D4})"));
            eb.Field(new($"Luck (Lv.{chao.LuckLevel:D2})", $"{chao.GetEmojiGrade(chao.LuckGrade)} : {chao.LuckProgress:D2}/100 ({chao.LuckValue:D4})"));

            return eb.Build();
        }

        public async Task<Embed> CreateRaceEmbed(Core.Race race, RaceInstance raceInstance)
        {
            var name = race.Name;
            var difficulty = GetDifficultyString(race.Difficulty);
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
                composition += $"**Swimming**: {race.SwimPercentage * 100}%\r\n";
            if (race.FlyPercentage > 0)
                composition += $"**Flying**: {race.FlyPercentage * 100}%\r\n";
            if (race.RunPercentage > 0)
                composition += $"**Running**: {race.RunPercentage * 100}%\r\n";
            if (race.PowerPercentage > 0)
                composition += $"**Climbing**: {race.PowerPercentage * 100}%\r\n";
            if (race.IntelligencePercentage > 0)
                composition += $"**Puzzles**: {race.IntelligencePercentage * 100}%\r\n";
            if (race.LuckPercentage > 0)
                composition += $"**Traps**: {race.LuckPercentage * 100}%\r\n";
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

            var roster = string.Empty;
            foreach (var c in chao)
            {
                var status = string.Empty;
                if (c.Status == RaceInstanceChaoSegment.SegmentStates.Retired)
                    status = " :x: (Retired)";

                roster += $"#{c.Position}. {c.ChaoName}{status}\r\n";
                //eb.Field(new($"#{c.Position}", $"{c.ChaoName}{status}"));
            }
            eb.Field(new("Chao", roster));

            return eb.Build();
        }

        private string GetDifficultyString(int difficulty)
        {
            switch (difficulty)
            {
                default:
                case 1:
                    return "★";
                case 2:
                    return "★★";
                case 3:
                    return "★★★";
                case 4:
                    return "★★★★";
                case 5:
                    return "★★★★★";
                case 6:
                    return "☠";
                case 7:
                    return "☠☠";
                case 8:
                    return "☠☠☠";
                case 9:
                    return "☠☠☠☠";
                case 10:
                    return "☠☠☠☠☠";
                case 11:
                    return "🔥";
                case 12:
                    return "🔥🔥";
                case 13:
                    return "🔥🔥🔥";
                case 14:
                    return "🔥🔥🔥🔥";
                case 15:
                    return "🔥🔥🔥🔥🔥";
            }
        }
    }
}