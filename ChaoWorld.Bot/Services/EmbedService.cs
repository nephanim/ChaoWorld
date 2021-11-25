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
using System.Text;

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

        public async Task<Embed> CreateGardenEmbed(Context ctx, Core.Garden garden)
        {
            var gardenOwner = await ctx.GetCachedGardenOwner(garden.Id);
            var chaoCount = await _repo.GetGardenChaoCount(garden.Id);
            var activeChao = await _repo.GetActiveChaoForGarden(garden.Id.Value);

            var eb = new EmbedBuilder()
                .Title($"{gardenOwner}'s Garden")
                .Footer(new($"Garden ID: {garden.Id} | Created on {garden.CreatedOn}"));

            eb.Field(new("Rings", string.Format("{0:n0}", garden.RingBalance), true));
            eb.Field(new("Active Chao", activeChao != null ? activeChao.DisplayName : "(not set)"));
            eb.Field(new($"Chao ({chaoCount})", $"(see `!garden {garden.Id} list`)", true));

            return eb.Build();
        }

        public async Task<Embed> CreateChaoEmbed(Context ctx, Core.Garden garden, Core.Chao chao)
        {
            var name = chao.DisplayName;
            var gardenOwner = await ctx.GetCachedGardenOwner(garden.Id);
            var raceStats = await _repo.GetRaceStats(chao.Id.Value);
            var totalRaces = raceStats != null ? string.Format("{0:n0}", raceStats.TotalRaces) : "0";
            var totalWins = raceStats != null ? string.Format("{0:n0}", raceStats.TotalWins) : "0";
            var totalRetires = raceStats != null ? string.Format("{0:n0}", raceStats.TotalRetires) : "0";
            var winRate = raceStats != null ? raceStats.WinRate.ToString("N2") : "0";
            var retireRate = raceStats != null ? raceStats.RetireRate.ToString("N2") : "0";
            var isFertile = chao.IsFertile ? "Yes" : "No";
            var imageUrl = MiscUtils.GenerateThumbnailForChao(chao);

            var eb = new EmbedBuilder()
                .Title(new(name))
                //.Thumbnail(new(imageUrl))
                .Image(new(imageUrl))
                .Description(chao.Appearance)
                .Footer(new(
                    $"Garden ID: {garden.Id} | Chao ID: {chao.Id} {$"| Created on {chao.CreatedOn.FormatZoned(DateTimeZone.Utc)}"}"));

            //eb.Field(new("URL", imageUrl));
            eb.Field(new("__General Info:__",
                $"Owner: {gardenOwner}\r\n" +
                $"Current Age: {chao.CurrentAge}\r\n" +
                $"Total Age: {chao.TotalAge}\r\n" +
                $"Reincarnations: {chao.Reincarnations}\r\n" +
                $"Chaos Factor: {Math.Floor(chao.ReincarnationStatFactor*100)}%" +
                (chao.EvolutionState != Core.Chao.EvolutionStates.Child ? $"\r\nFertile: {isFertile}" : string.Empty)
            ));
            eb.Field(new($"__Development:__",
                $"Affinity: {chao.GetEffectiveAbilityType().GetDescription()}\r\n" +
                $"Alignment: {chao.GetEffectiveAlignment()}"
            ));

            if (await _repo.GetChaoGenes(chao.Id.Value) is { } genes)
            {
                var papaString = string.Empty;
                var mamaString = string.Empty;
                if (genes.FirstParentId != null && genes.SecondParentId != null)
                {
                    if (await _repo.GetChao(genes.FirstParentId.Value) is { } papaChao)
                    {
                        var papaOwner = await ctx.GetCachedGardenOwner(papaChao.GardenId);
                        papaString = $"{papaChao.Name} ({papaOwner}) ({papaChao.SwimGrade}{papaChao.FlyGrade}{papaChao.RunGrade}{papaChao.PowerGrade}{papaChao.StaminaGrade}{papaChao.IntelligenceGrade}{papaChao.LuckGrade})";
                    }
                    if (await _repo.GetChao(genes.SecondParentId.Value) is { } mamaChao)
                    {
                        var mamaOwner = await ctx.GetCachedGardenOwner(mamaChao.GardenId);
                        mamaString = $"{mamaChao.Name} ({mamaOwner}) ({mamaChao.SwimGrade}{mamaChao.FlyGrade}{mamaChao.RunGrade}{mamaChao.PowerGrade}{mamaChao.StaminaGrade}{mamaChao.IntelligenceGrade}{mamaChao.LuckGrade})";
                    }
                    eb.Field(new($"__Lineage:__", $"{papaString}\r\n{mamaString}"));
                }
            }

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
            //eb.Field(new("Image URL", imageUrl));
            eb.Color(await GetDiscordColor(chao));

            return eb.Build();
        }

        public async Task<Embed> CreateItemEmbed(ItemBase item)
        {
            // TODO: Item thumbnail images for embeds!
            //var imageUrl = MiscUtils.GenerateThumbnailForItem(item);

            var eb = new EmbedBuilder()
                .Title(new(item.Name))
                //.Thumbnail(new(imageUrl))
                .Description(item.Description)
                .Footer(new(
                    $"Item Type ID: {item.TypeId} | Item Category: {item.Category.GetDescription()}"));

            var priceInfo = item.IsMarketEnabled && item.MarketPrice.HasValue
                ? string.Format("{0:n0}", item.MarketPrice.Value)
                : "N/A";
            eb.Field(new("Market Price", priceInfo));

            return eb.Build();
        }

        public async Task<Embed> CreateRaceEmbed(Context ctx, Core.Race race, RaceInstance raceInstance)
        {
            var name = race.Name;
            var difficulty = Core.Race.GetDifficultyString(race.Difficulty);
            var participants = await _repo.GetRaceInstanceChaoCount(raceInstance.Id);
            var imageUrl = MiscUtils.GenerateThumbnailForRace(race);

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
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
                    var chaoImageUrl = MiscUtils.GenerateThumbnailForChao(winner);
                    var winnerOwner = await ctx.GetCachedGardenOwner(winner.GardenId);
                    eb.Field(new("Winner", $"{winner.DisplayName} ({winnerOwner})"));
                    eb.Field(new("Time", timeElapsed));
                    eb.Image(new(chaoImageUrl));
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
            var imageUrl = MiscUtils.GenerateThumbnailForRace(race);

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
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

        public async Task<Embed> CreateTournamentEmbed(Context ctx, Core.Tournament tourney, TournamentInstance instance)
        {
            var name = $"{tourney.Name} Tournament";
            var participants = await _repo.GetTournamentInstanceChaoCount(instance.Id);
            var imageUrl = MiscUtils.GenerateThumbnailForTournament();

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
                .Description(tourney.Description)
                .Footer(new(
                    $"Instance ID: {instance.Id} | Created on {instance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            eb.Field(new("Status", instance.State.GetDescription()));
            eb.Field(new("Participants", $"{participants}"));

            if (instance.WinnerChaoId.HasValue)
            {
                var timeElapsed = TimeSpan.FromSeconds(instance.TotalTimeElapsedSeconds).ToString("c");
                var winner = await _repo.GetChao(instance.WinnerChaoId.GetValueOrDefault(0));
                if (winner != null)
                {
                    var chaoImageUrl = MiscUtils.GenerateThumbnailForChao(winner);
                    var winnerOwner = await ctx.GetCachedGardenOwner(winner.GardenId);
                    eb.Field(new("Winner", $"{winner.DisplayName} ({winnerOwner})"));
                    eb.Field(new("Time", timeElapsed));
                    eb.Image(new(chaoImageUrl));
                }
            }

            return eb.Build();
        }

        public async Task<Embed> CreateTreeEmbed(Context ctx, Core.Tree tree)
        {
            var name = $"{tree.Name}";
            var fruitType = await _repo.GetItemBaseByTypeId(tree.FruitTypeId);
            var desc = string.Empty;
            var now = SystemClock.Instance.GetCurrentInstant();
            if (tree.Health == 0)
                desc = "It doesn't appear to be producing anything. Maybe it needs water.";
            else if (tree.Health < 25)
                desc = "It's producing at low capacity. It could use more attention.";
            else if (tree.Health < 75)
                desc = "It's producing at good capacity. Keep taking care of it, and it could be a steady source of food.";
            else
                desc = "It's in excellent shape. You've been watering it regularly.";

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Description(desc)
                .Footer(new(
                    $"Tree ID: {tree.Id} | Created on {tree.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            eb.Field(new("Type", fruitType.Name));
            eb.Field(new("Age", $"{tree.Age}"));
            var yieldType = tree.Name.Contains("Mushroom")
                ? "Mushrooms"
                : "Fruits";
            var hasFruits = tree.FruitQuantity > 0
                ? $" {Emojis.OrangeFruit}"
                : string.Empty;
            eb.Field(new(yieldType, $"{tree.FruitQuantity}{hasFruits}"));
            eb.Field(new("Health", $"{tree.Health:D2}/100"));
            var waterMe = tree.NextWatering < now
                ? $" {Emojis.Droplet}"
                : string.Empty;
            eb.Field(new("Next Watering", $"{tree.TimeUntilWatering}{waterMe}"));

            return eb.Build();
        }

        public async Task<Embed> CreateTournamentRoundResultsEmbed(Context ctx, Core.Tournament tourney, TournamentInstance instance, int round)
        {
            var roundText = round == instance.Rounds ? "Final Round" : $"Round #{round}";
            var name = $"{tourney.Name} Tournament Progress: Round {round} Results";
            var imageUrl = MiscUtils.GenerateThumbnailForTournament();

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
                .Description(tourney.Description)
                .Footer(new(
                    $"Instance ID: {instance.Id} | Round: {round} of {instance.Rounds} | Created on {instance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            var elapsed = TimeSpan.FromSeconds(instance.RoundElapsedTimeSeconds).ToString("c");
            eb.Field(new("Time Elapsed", elapsed));

            var combatants = (await _repo.GetChaoInTournament(instance)).OrderBy(x => x.Name);
            var tourneyInstanceChao = (await _repo.GetTournamentInstanceChao(instance));
            var advancingList = new StringBuilder();
            var retiredList = new StringBuilder();
            foreach (var combatant in combatants)
            {
                var chao = tourneyInstanceChao.FirstOrDefault(x => x.ChaoId == combatant.Id.Value);
                if (chao == null)
                    continue;

                if (chao.State == TournamentInstance.TournamentStates.Canceled)
                    retiredList.Append($"{combatant.DisplayName} :x:\r\n");
                else
                    advancingList.Append($"{combatant.DisplayName}\r\n");
            }

            eb.Field(new("Advancing", advancingList.ToString()));
            eb.Field(new("Retired", retiredList.ToString()));

            return eb.Build();
        }

        public async Task<Embed> CreateTournamentMatchResultsEmbed(Context ctx, Core.Tournament tourney, TournamentInstance instance, TournamentInstanceMatch match, Core.Chao winner, Core.Chao loser)
        {
            var matchText = match.RoundNumber == instance.Rounds ? "FINALS" : $"{match.RoundNumber}-{match.RoundOrder}";
            var name = $"{tourney.Name} Tournament Progress: Match Results ({matchText})";
            var imageUrl = MiscUtils.GenerateThumbnailForTournament();

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Thumbnail(new(imageUrl))
                .Description(tourney.Description)
                .Footer(new(
                    $"Instance ID: {instance.Id} | Round: {match.RoundNumber} of {instance.Rounds} | Match: {match.RoundOrder} of {instance.Matches} | Created on {instance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            var elapsed = TimeSpan.FromSeconds(match.ElapsedTimeSeconds).ToString("c");
            eb.Field(new("Match Time", elapsed));

            if (winner != null)
            {
                var winnerOwner = await ctx.GetCachedGardenOwner(winner.GardenId);
                eb.Field(new("Winner", $"{winner.DisplayName} ({winnerOwner})"));
            }

            if (loser != null)
            {
                var loserOwner = await ctx.GetCachedGardenOwner(loser.GardenId);
                eb.Field(new("Loser", $"{loser.DisplayName} ({loserOwner})"));
            }

            return eb.Build();
        }

        public async Task<Embed> CreateExpeditionEmbed(Context ctx, Expedition expedition, ExpeditionInstance instance)
        {
            var name = $"Expedition: {expedition.Name}";

            var eb = new EmbedBuilder()
                .Title(new(name))
                .Description(new(expedition.Description))
                .Footer(new (
                    $"Instance ID: {instance.Id} | Created on {instance.CreatedOn.FormatZoned(DateTimeZone.Utc)}"));

            eb.Field(new("Status", $"{instance.State.GetDescription()}"));
            eb.Field(new("Time Remaining", $"{instance.TimeRemaining}"));
            eb.Field(new("Progress", $"{Math.Round(instance.TotalContribution.GetValueOrDefault(0) / (double)expedition.ProgressRequired * 100)}%"));

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