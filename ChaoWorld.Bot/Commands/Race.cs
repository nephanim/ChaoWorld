using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Linq;

using Dapper;

using Myriad.Builders;

using Newtonsoft.Json.Linq;

using ChaoWorld.Core;
using System.Threading;
using System.Collections.Generic;
using Myriad.Rest.Types;
using NodaTime;

namespace ChaoWorld.Bot
{
    public class Race
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Race(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task ViewRaceInstance(Context ctx, Core.RaceInstance target)
        {
            var race = await _repo.GetRaceById(target.RaceId);
            await ctx.Reply(embed: await _embeds.CreateRaceEmbed(ctx, race, target));
        }

        public async Task EnterChaoInRace(Context ctx, Core.Chao chao, RaceInstance raceInstance)
        {
            ctx.CheckOwnChao(chao); //You can only enter your own chao in a race...

            raceInstance = await _repo.GetRaceInstanceById(raceInstance.Id);
            var activeInRace = await _repo.GetActiveRaceByGarden(chao.GardenId.Value);
            var activeInTourney = await _repo.GetActiveTournamentByGarden(chao.GardenId.Value);
            var race = await _repo.GetRaceByInstanceId(raceInstance.Id);
            var allowedChannels = await _repo.ReadBroadcastChannels();

            if (ctx.Channel.Id != allowedChannels.Races)
            {
                await ctx.Reply($"{Emojis.Error} Please use <#{allowedChannels.Races}> to join races.");
            }
            else if (raceInstance.State == RaceInstance.RaceStates.InProgress
                || raceInstance.State == RaceInstance.RaceStates.Completed
                || raceInstance.State == RaceInstance.RaceStates.Canceled)
            {
                // Race isn't joinable - sorry!
                await ctx.Reply($"{Emojis.Error} The {race.Name} race is {Core.MiscUtils.GetDescription(raceInstance.State).ToLower()} and can no longer be joined. Please wait for the next race.");
            }
            else if (ctx.Garden.InstanceLimit <= 0)
            {
                await ctx.Reply($"{Emojis.Error} You have reached the daily instance limit. You can join races again tomorrow.");
            }
            else if (activeInRace != null)
            {
                // There's a chao in this garden that's already participating in a race.
                var activeRace = await _repo.GetRaceByInstanceId(activeInRace.Id);
                await ctx.Reply($"{Emojis.Error} You already have a chao participating in a {activeRace.Name} race. Please support your chao in that race first!");
            }
            else if (activeInTourney != null)
            {
                // There's a chao in this garden that's already participating in a tournament.
                var tourney = await _repo.GetTournamentById(activeInTourney.TournamentId);
                await ctx.Reply($"{Emojis.Error} You already have a chao participating in a {tourney.Name} tournament. Please support your chao's tournament first!");
            }
            else
            {
                // Race is in a joinable state
                // Check whether we've reached the minimum number of chao for the race
                var currentChaoCount = await _repo.GetRaceInstanceChaoCount(raceInstance.Id);
                if (race == null)
                {
                    await ctx.Reply($"{Emojis.Error} Unable to read the requested race. (Race Instance ID: {raceInstance.Id})");
                }
                else if (currentChaoCount >= race.MaximumChao)
                {
                    // The race is full - don't join it
                    await ctx.Reply($"{Emojis.Error} This race has reached the participant limit ({currentChaoCount}/{race.MaximumChao}). Please wait for the next race.");
                }
                else
                {
                    // The race is not full - join it
                    await _repo.JoinChaoToRaceInstance(raceInstance, chao);
                    currentChaoCount++;
                    await ctx.Reply($"{Emojis.Success} {chao.Name} has joined the {race.Name} race. Do your best!");

                    // See whether this chao joining puts us at the required threshold to start
                    if (currentChaoCount >= race.MinimumChao && raceInstance.State == RaceInstance.RaceStates.New)
                    {
                        // We've reached the minimum threshold, and haven't begun preparing the race
                        raceInstance.State = RaceInstance.RaceStates.Preparing;
                        await _repo.UpdateRaceInstance(raceInstance);

                        // Start polling to make sure the tournament is still ready when it's time to start
                        var now = SystemClock.Instance.GetCurrentInstant();
                        while (now < raceInstance.ReadyOn)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1));
                            now = SystemClock.Instance.GetCurrentInstant();
                        }
                        // We've waited long enough... if we're still in a preparing state, go ahead and start
                        if (raceInstance.State == RaceInstance.RaceStates.Preparing)
                            await StartRace(ctx, race, raceInstance);
                    }
                }
            }
        }

        public async Task LeaveRace(Context ctx)
        {
            ctx.CheckGarden();

            var activeRace = await _repo.GetActiveRaceByGarden(ctx.Garden.Id.Value);
            if (activeRace != null)
            {
                if (activeRace.State == RaceInstance.RaceStates.New || activeRace.State == RaceInstance.RaceStates.Preparing)
                {
                    var raceChao = await _repo.GetRaceInstanceChao(activeRace);
                    var chao = raceChao.FirstOrDefault(x => x.GardenId == ctx.Garden.Id);
                    if (chao != null)
                    {
                        await _repo.RemoveChaoFromRaceInstance(activeRace, chao);
                    }
                    await ctx.Reply($"{Emojis.Success} You are no longer waiting for the race to start.");
                } else
                    await ctx.Reply($"{Emojis.Error} You can no longer withdraw from the race. Please wait for it to finish.");
            }
            else
                await ctx.Reply($"{Emojis.Error} None of your chao are currently participating in a race.");
        }

        public async Task UpdatePingSettings(Context ctx)
        {
            var allowPings = false;
            if (ctx.Match("enable", "on", "yes", "1", "true", "accept", "allow"))
                allowPings = true;
            await _repo.UpdateRacePingSetting(ctx.Author.Id, allowPings);
            if (allowPings)
                await ctx.Reply($"{Emojis.Success} Race pings are now enabled.");
            else
                await ctx.Reply($"{Emojis.Success} Race pings are now disabled.");
        }

        public async Task StartRace(Context ctx, Core.Race race, RaceInstance raceInstance)
        {
            // The ready delay period is over -- time to race!
            if (raceInstance.State == RaceInstance.RaceStates.Preparing || raceInstance.State == RaceInstance.RaceStates.New)
            {
                // Start the race!
                raceInstance.State = RaceInstance.RaceStates.InProgress;
                await _repo.UpdateRaceInstance(raceInstance);
                await ctx.Reply($"{Emojis.Megaphone} The {race.Name} race is starting! Good luck to all participants!");

                try
                {
                    // Determine how many slots to fill with NPC chao and select random chao to fill those
                    var currentChaoCount = await _repo.GetRaceInstanceChaoCount(raceInstance.Id);
                    var joiningNPCs = new List<Core.Chao>();
                    while (currentChaoCount < race.MaximumChao)
                    {
                        var npc = await _repo.GetRandomChao(0); // Garden 0 is a special holding place reserved for NPCs
                        if (joiningNPCs.All(x => x.Id != npc.Id))
                        {
                            joiningNPCs.Add(npc);
                            await _repo.JoinChaoToRaceInstance(raceInstance, npc);
                            currentChaoCount++;
                        } // else we loop again without incrementing, so we get another one
                    }
                }
                catch (Exception e)
                {
                    await _repo.LogMessage($"Failed to fill race instance {raceInstance.Id} of race {race.Id} with NPC chao: {e.Message}");
                }

                // Now we have a full roster - initialize all the race segments
                await _repo.AddSegmentsToRaceInstance(raceInstance);

                // Queue updates for the first set of race segments
                var complete = false;
                var index = 0;
                while (!complete)
                {
                    var result = await UpdateRaceSegments(ctx, race, raceInstance, index);
                    complete = result.Complete;
                    index++;
                }
            }
            else
            {
                await _repo.LogMessage($"Tried to start race instance {raceInstance.Id} of race {raceInstance.RaceId}, but its state was {raceInstance.State}");
                // Something weird happened and the race is already running / finished... do nothing
            }
        }

        public async Task<RaceSegmentResult> UpdateRaceSegments(Context ctx, Core.Race race, Core.RaceInstance raceInstance, int index)
        {
            var result = new RaceSegmentResult
            {
                Complete = false,
                SegmentTimeSeconds = 0
            };
            var template = await _repo.GetRaceSegment(raceInstance.RaceId, index);
            var segments = await _repo.GetRaceInstanceSegments(raceInstance, index);
            if (segments.Count() == 0)
            {
                // Race is done - finish it!
                await _repo.FinalizeRaceInstanceChao(raceInstance); // Set final results for each chao
                await _repo.CompleteRaceInstance(raceInstance); // Set final results for the race
                var prizeRings = GetPrizeAmount(race); 
                await _repo.GiveRaceRewards(raceInstance, prizeRings); // Award the prize to the winner

                raceInstance = await _repo.GetRaceInstanceById(raceInstance.Id); // Refresh our instance information (because some of it is stale)
                await ctx.Reply(embed: await _embeds.CreateRaceEmbed(ctx, race, raceInstance));

                var notifyList = await _repo.GetAccountsToPingForRace(raceInstance.Id);
                var notifyString = notifyList.Any() ? " " + string.Join(" ", notifyList.Select(x => $"<@{x}>").ToArray()) : "";
                var mentions = new AllowedMentions
                {
                    Users = notifyList.ToArray()
                };
                
                await ctx.Reply($"{Emojis.Megaphone} The {race.Name} race has finished. Thanks for playing!{notifyString}", mentions: mentions);
                result.Complete = true;
            }
            else if (segments.All(x => x.State == RaceInstanceChaoSegment.SegmentStates.Retired))
            {
                // Race isn't done, but all chao already retired...
                await _repo.FinalizeRaceInstanceChao(raceInstance); // Set final results for each chao
                await _repo.CompleteRaceInstance(raceInstance); // Set final results for the race

                var notifyList = await _repo.GetAccountsToPingForRace(raceInstance.Id);
                var notifyString = notifyList.Any() ? " " + string.Join(" ", notifyList.Select(x => $"<@{x}>").ToArray()) : "";
                var mentions = new AllowedMentions
                {
                    Users = notifyList.ToArray()
                };

                await ctx.Reply($"{Emojis.Megaphone} The {race.Name} race has been canceled because the chao can no longer continue.{notifyString}", mentions: mentions);
                result.Complete = true;
            }
            else
            {
                // Process this leg of the race for all participants
                var allChao = await _repo.GetRaceInstanceChao(raceInstance);
                var lastSegments = await _repo.GetRaceInstanceSegments(raceInstance, index - 1);

                foreach (var segment in segments)
                {
                    var chao = allChao.FirstOrDefault(x => x.Id.Value == segment.ChaoId);

                    // Make sure we carry over the end elevation from the previous segment if the chao flew through it
                    var lastSegment = lastSegments.Where(x => x.ChaoId == chao.Id.Value).FirstOrDefault();
                    if (lastSegment != null)
                        segment.StartElevation = lastSegment.EndElevation > template.StartElevation
                            ? lastSegment.EndElevation
                            : template.StartElevation;
                    var updatedSegment = await ProcessSegmentForChao(template, segment, chao);
                    await _repo.UpdateRaceInstanceSegment(updatedSegment); // Persist the results of running this ChaoSegment
                    await _repo.UpdateChao(chao); // Persist any changes to the chao (stat progress)
                }

                // Figure out positions based on total time in the race
                // Note that the total time for retired chao is somewhat misleading; we want to make sure those are at the bottom
                var orderedSegments = segments.OrderBy(x =>
                    x.State == RaceInstanceChaoSegment.SegmentStates.Completed
                        ? x.TotalTimeSeconds.GetValueOrDefault(0)
                        : Int32.MaxValue
                );
                var orderedChao = new List<RaceProgressListItem>();
                var i = 1;
                foreach (var s in orderedSegments)
                {
                    orderedChao.Add(new RaceProgressListItem
                    {
                        ChaoId = s.ChaoId,
                        ChaoName = allChao.FirstOrDefault(x => x.Id.Value == s.ChaoId).DisplayName,
                        Status = s.State,
                        Position = i,
                        TotalTimeSeconds = s.TotalTimeSeconds.GetValueOrDefault(0)
                    });
                    i++;
                }

                // Determine how long we need to wait to simulate "running" this segment of the race
                var fastestSegment = orderedSegments.FirstOrDefault();
                var fastestChao = allChao.FirstOrDefault(x => x.Id.Value == fastestSegment.ChaoId);
                var timeElapsed = TimeSpan.FromSeconds(fastestSegment.TotalTimeSeconds.GetValueOrDefault(0));

                // Now we wait...
                await Task.Delay(fastestSegment.SegmentTimeSeconds.GetValueOrDefault(0) * 1000);

                // Report race status - current time / positions of all chao
                await ctx.Reply(embed: await _embeds.CreateRaceProgressEmbed(race, raceInstance, template, timeElapsed, orderedChao));

                //await ctx.Reply($"{Emojis.Megaphone} {fastestChao.Name} is in the lead in the {race.Name} race!");
                result.Complete = false; // There may be more segments after this one
                result.SegmentTimeSeconds = fastestSegment.SegmentTimeSeconds.GetValueOrDefault(0);
            }
            return result;
        }

        private async Task<RaceInstanceChaoSegment> ProcessSegmentForChao(RaceSegment template, RaceInstanceChaoSegment segment, Core.Chao chao)
        {
            // If the chao retired in an earlier segment, all following segments will already have been processed
            if (segment.State == RaceInstanceChaoSegment.SegmentStates.Retired)
                return segment;

            // Make sure we have a starting elevation if it's not already set (e.g. first segment of the race)
            if (!segment.StartElevation.HasValue)
                segment.StartElevation = template.StartElevation;
            segment.SegmentTimeSeconds = 0;

            // Calculate starting stamina from chao's stats for the first leg of the race
            if (template.RaceIndex == 0)
                segment.StartStamina = CalculateStaminaForChao(chao);
            else
                segment.StartStamina = await _repo.GetRemainingStaminaForChao(segment.RaceInstanceId, segment.ChaoId);

            // It's possible to enter a segment after flying through the previous one, resulting in carryover elevation
            var flyDistance = await ProcessFlightForSegment(template, segment, chao);
            await ProcessPuzzlesForSegment(template, segment, chao);
            await ProcessTerrainForSegment(template, segment, chao, flyDistance);
            segment.TotalTimeSeconds = segment.SegmentTimeSeconds.GetValueOrDefault(0) + await _repo.GetTotalTimeForSegments(segment.RaceInstanceId, segment.ChaoId);

            return segment;
        }

        private async Task ProcessTerrainForSegment(RaceSegment template, RaceInstanceChaoSegment segment, Core.Chao chao, int flyDistance)
        {
            if (segment.State != RaceInstanceChaoSegment.SegmentStates.Completed)
            {
                // If we have more distance to cover, check how long that takes
                // Note that power-based segments use the elevation rather than ground distance
                var remainingDistance = template.TerrainDistance > 0
                    ? template.TerrainDistance - flyDistance
                    : template.EndElevation - segment.StartElevation.GetValueOrDefault(template.StartElevation); // Use the segment start elevation here if we can (we may be flying into a wall)
                var terrainTime = CalculateTerrainTime(chao, remainingDistance, template.TerrainType);
                segment.SegmentTimeSeconds += terrainTime;

                // We always end at the segment's end elevation if we're not mid-air
                if (remainingDistance > 0)
                    segment.EndElevation = template.EndElevation;

                // Terrain travel uses stamina - make sure we can actually complete the segment
                var staminaCost = (int)(terrainTime * template.StaminaLossMultiplier);
                if (template.TerrainType == RaceSegment.RaceTerrains.Swim)
                    staminaCost = staminaCost / 2; // The official engine doesn't even take stamina for swimming, and this tends to be the thing that bloats min stamina requirements
                if (staminaCost <= segment.StartStamina.GetValueOrDefault(0))
                {
                    // Carry over the stamina we have left into the next segment
                    segment.EndStamina = segment.StartStamina.GetValueOrDefault(0) - staminaCost;
                    segment.State = RaceInstanceChaoSegment.SegmentStates.Completed;
                }
                else
                {
                    // The chao couldn't make it... collapsed, fell, drowned, etc...
                    // Only count segment time/distance up until they retire
                    segment.EndStamina = 0;
                    segment.SegmentTimeSeconds = segment.SegmentTimeSeconds * segment.StartStamina / staminaCost;
                    remainingDistance = remainingDistance * segment.StartStamina.GetValueOrDefault(0) / staminaCost;
                    segment.State = RaceInstanceChaoSegment.SegmentStates.Retired;

                    // Make sure the remaining segments are marked as retired too
                    await _repo.RetireInstanceChao(chao.Id.Value, segment.RaceInstanceId);
                }
                var usedStamina = segment.StartStamina.GetValueOrDefault(0) - segment.EndStamina.GetValueOrDefault(0);

                // Raise terrain-based stat progress
                RaiseChaoStatProgress(template.TerrainType, chao, remainingDistance, usedStamina);
            }
        }

        private static void RaiseChaoStatProgress(RaceSegment.RaceTerrains terrain, Core.Chao chao, int remainingDistance, int usedStamina)
        {
            chao.RaiseStamina(usedStamina);
            switch (terrain)
            {
                case RaceSegment.RaceTerrains.Swim:
                    chao.RaiseSwim(remainingDistance);
                    break;
                case RaceSegment.RaceTerrains.Power:
                    chao.RaisePower(remainingDistance);
                    break;
                case RaceSegment.RaceTerrains.Run:
                default:
                    chao.RaiseRun(remainingDistance);
                    break;
            }
        }

        private async Task<int> ProcessFlightForSegment(RaceSegment template, RaceInstanceChaoSegment segment, Core.Chao chao)
        {
            var flyDistance = 0;
            if (segment.StartElevation > template.EndElevation)
            {
                // We're in the air! Fly until we can't anymore
                var fallSpeed = CalculateFlightFallSpeed(chao);
                var fallDistance = segment.StartElevation.GetValueOrDefault(template.EndElevation) - template.EndElevation;

                if (fallDistance > 0)
                {
                    // We're falling with style... so where do we end up?
                    var maxFlyTime = fallDistance / fallSpeed; // This is the time we have before gravity brings us down
                    flyDistance = CalculateFlightDistance(chao, maxFlyTime); // This is how far we'd fly in that time

                    // But if we will fly into the next segment, stop there - we don't know the elevation of the next segment yet
                    if (flyDistance > template.TerrainDistance)
                    {
                        // Check where our elevation ends so we keep flying in the next segment (or skip some climbing)
                        var endElevation = CalculateFlightEndElevation(segment.StartElevation.GetValueOrDefault(template.StartElevation), fallDistance, template.TerrainDistance, flyDistance);
                        
                        segment.EndElevation = endElevation >= template.EndElevation
                            ? endElevation
                            : template.EndElevation;
                        flyDistance = template.TerrainDistance; // Set the actual distance flown based on the size of the segment 
                        segment.State = RaceInstanceChaoSegment.SegmentStates.Completed; // We already know we finished the segment since there's no stamina consumption while flying
                        segment.EndStamina = segment.StartStamina;
                    }

                    // Confirm how long we're airborne and add that to our race time
                    var flyTime = CalculateFlightTime(chao, flyDistance);
                    segment.SegmentTimeSeconds += flyTime;

                    // Now raise the chao's flying progress based on the distance traveled and distance fallen
                    // Distance traveled on its own is generally way too low, considering even flying-based races only have short flying segments
                    chao.RaiseFly(flyDistance + fallDistance);
                }
            }

            return flyDistance;
        }

        private async Task ProcessPuzzlesForSegment(RaceSegment template, RaceInstanceChaoSegment segment, Core.Chao chao)
        {
            // INTELLIGENCE
            var puzzleTime = 0;
            if (template.IntelligenceRating > 0)
            {
                puzzleTime = Math.Max(1, // No matter how smart you are, you need a second to think about it
                    (int)((template.IntelligenceRating / 6) // How hard is the puzzle? e.g. rating of 30 -> 5s base time
                    * (1 - Math.Sqrt(chao.IntelligenceValue*10)/600)) // Reduce this by 0-50% depending on the chao's intelligence
                );
                chao.RaiseIntelligence(template.IntelligenceRating);
            }
            segment.SegmentTimeSeconds += puzzleTime;
            
            // LUCK
            var misfortuneTime = 0;
            if (template.LuckRating > 0)
            {
                var rand = new Random().Next(0, chao.LuckValue / 25); // This is the chao's dice roll to evade the trap
                if (rand <= template.LuckRating)
                {
                    // Failed to evade the trap
                    misfortuneTime = template.LuckRating / 6;
                }
                chao.RaiseLuck(template.LuckRating);
            }

            segment.SegmentTimeSeconds += misfortuneTime;
        }

        private int GetPrizeAmount(Core.Race race)
        {
            // This will reward anywhere from 50% to 150% of the listed prize amount for a race
            return (int)(race.PrizeRings * (0.5 + new Random().NextDouble()));
        }

        private int CalculateStaminaForChao(Core.Chao chao)
        {
            // NOTE: This might need adjustment for balance.
            // The base value is based on the duration of the beginner races.
            // The multiplier for the stat is based on the high end races including a 1 minute race with triple stamina drain.
            return 60 + chao.StaminaValue / 20;
        }

        private double CalculatePathEfficiency(Core.Chao chao)
        {
            // Chao won't always take the same path through the segment (basically never the "ideal" path)
            // Intelligence and luck have an impact here, but unpredictable obstacles (e.g. other chao) will always affect it too
            // This never affects their speed, only distance traveled
            var randomComponent = new Random().NextDouble() * 10.0; // This is the portion that models random obstacles
            var intelligenceComponent = (chao.IntelligenceValue / 400.0); // Cleverness results in better pathing by cutting corners
            var luckComponent = (chao.LuckValue / 600.0); // Luck has a very small impact, but lucky chao will have things go their way!
            var baseComponent = 80.0; // This is basically the minimum efficiency, no matter how dumb / unlucky you are
            return (baseComponent + randomComponent + intelligenceComponent + luckComponent) / 100.0;
        }

        private int CalculateFlightFallSpeed(Core.Chao chao)
        {
            // Chao always try to fly when they're falling, resulting in a constant fall velocity dependent solely on their flying skill
            return (int)(3 * Math.Exp(-0.000285 * chao.FlyValue));
        }

        private int CalculateFlightDistance(Core.Chao chao, int totalFlyTime)
        {
            return (int)(totalFlyTime / Math.Exp(-0.000114 * chao.FlyValue) * CalculatePathEfficiency(chao));
        }
        
        private int CalculateFlightTime(Core.Chao chao, int distance)
        {
            // This should be the flight distance (not the segment distance)
            // We'll use a different stat in the calculation once we land
            return (int)(distance * Math.Exp(-0.000114 * chao.FlyValue));
        }

        private int CalculateFlightEndElevation(int startElevation, int fallDistance, int segmentDistance, int totalFlyDistance)
        {
            // Our total flight creates a triangle, and the segmented portion creates a smaller similar triangle inside it
            // We can calculate the descent in this segment using: end = (segment distance) / (total flight distance) * (distance fallen)
            // Then we substract that from the start elevation to determine where we should end up
            var fallRatio = ((double)segmentDistance) / totalFlyDistance;
            return (int)(startElevation - (fallRatio * fallDistance));
        }

        private int CalculateTerrainTime(Core.Chao chao, int distance, RaceSegment.RaceTerrains terrainType)
        {
            switch (terrainType)
            {
                case RaceSegment.RaceTerrains.Power:
                    return CalculateClimbingTime(chao, distance)
                        + CalculateTripAndSlipTime(chao);
                case RaceSegment.RaceTerrains.Swim:
                    return CalculateSwimmingTime(chao, distance);
                case RaceSegment.RaceTerrains.Run:
                default:
                    return CalculateRunningTime(chao, distance)
                        + CalculateTripAndSlipTime(chao);
            }
        }

        // NOTE: The magic numbers in these functions come from tests run on the Steam version of SA2B. Might need adjustment.
        private int CalculateClimbingTime(Core.Chao chao, int distance)
        {
            return (int)((distance * Math.Exp(-0.000155 * chao.PowerValue) / CalculatePathEfficiency(chao))/3.0);
        }

        private int CalculateSwimmingTime(Core.Chao chao, int distance)
        {
            return (int)((distance * Math.Exp(-0.000436 * chao.SwimValue) / CalculatePathEfficiency(chao))*2.0);
        }

        private int CalculateRunningTime(Core.Chao chao, int distance)
        {
            return (int)((distance * Math.Exp(-0.000114 * chao.RunValue) / CalculatePathEfficiency(chao))/2.0);
        }

        private int CalculateTripAndSlipTime(Core.Chao chao)
        {
            // Check delays due to tripping / falling on a cliff
            var tripRoll = new Random().Next(0, 100 + chao.LuckValue / 20); // This is the chao's dice roll to avoid tripping
            if (tripRoll == 1) // Likelihood of tripping ranges from 1% per segment (newborn) -> 0.4% (3000 luck) -> 0.16% (9999 luck)
                return 5; // Static recovery time for tripping
            else
                return 0;
        }
    }

    public class RaceSegmentResult
    {
        public bool Complete;
        public int SegmentTimeSeconds;
    }
}