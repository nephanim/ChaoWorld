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

        public async Task NewRaceInstance(Context ctx, Core.Race race)
        {
            await using var conn = await _db.Obtain();

            // Create the instance
            var raceInstance = await _repo.CreateRaceInstance(race);

            // Send confirmation
            await ctx.Reply($"{Emojis.Megaphone} {race.Name} is now available. Use `!race {raceInstance.Id} chao {{id/name}}` to participate.");
        }

        public async Task EnterChaoInRace(Context ctx, Core.Chao chao, Core.RaceInstance raceInstance)
        {
            ctx.CheckOwnChao(chao); //You can only enter your own chao in a race...

            if (raceInstance.State == RaceInstance.RaceStates.InProgress
                || raceInstance.State == RaceInstance.RaceStates.Completed
                || raceInstance.State == RaceInstance.RaceStates.Canceled)
            {
                // Race isn't joinable - sorry!
                await ctx.Reply($"{Emojis.Error} This race is {Core.MiscUtils.GetDescription(raceInstance.State).ToLower()} and can no longer be joined.");
            }
            else
            {
                // Race is in a joinable state
                // Check whether we've reached the minimum number of chao for the race
                var race = await _repo.GetRaceByInstanceId(raceInstance.Id);
                var currentChaoCount = await _repo.GetRaceInstanceChaoCount(raceInstance.Id);
                if (currentChaoCount >= race.MaximumChao)
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
                        // Check how long we're supposed to wait before we start
                        raceInstance.ReadyOn = NodaTime.SystemClock.Instance.GetCurrentInstant();
                        await _repo.UpdateRaceInstance(raceInstance);

                        var readyDelay = TimeSpan.FromMinutes(race.ReadyDelayMinutes);
                        var startTimer = new Timer(_ =>
                        {
                            var __ = StartRace(ctx, race, raceInstance);
                        }, null, readyDelay, TimeSpan.FromMinutes(1));

                        await ctx.Reply($"{Emojis.Megaphone} The {race.Name} race will start in {race.ReadyDelayMinutes} minutes. Use `!race {raceInstance.Id} chao {{id/name}}` to participate.");
                    }
                }
            }

            
            
        }

        public async Task StartRace(Context ctx, Core.Race race, Core.RaceInstance raceInstance)
        {
            // The ready delay period is over -- time to race!
            if (raceInstance.State == RaceInstance.RaceStates.Preparing || raceInstance.State == RaceInstance.RaceStates.New)
            {
                // Initialize all the race segments
                await _repo.AddSegmentsToRaceInstance(raceInstance);

                // Start the race!
                raceInstance.State = RaceInstance.RaceStates.InProgress;
                await _repo.UpdateRaceInstance(raceInstance);
                await ctx.Reply($"{Emojis.Megaphone} The {race.Name} race has started! Good luck to all participants!");

                // Queue updates for the first set of race segments
                await UpdateRaceSegments(ctx, race, raceInstance, 0);
            }
            else
            {
                // Something weird happened and the race is already running / finished... do nothing
            }
        }

        public async Task UpdateRaceSegments(Context ctx, Core.Race race, Core.RaceInstance raceInstance, int index)
        {
            var template = await _repo.GetRaceSegment(raceInstance.RaceId, index);
            var segments = await _repo.GetRaceInstanceSegments(raceInstance, index);
            if (segments.Count() == 0)
            {
                // Race is done - finish it!
                // TODO: ...
                //  * Update the raceinstancechao with their final results
                //  * Update the race state, winner, and total time
                //  * Award the prize to the winner
                //  * Announce the results
                
            }
            else
            {
                // Process this leg of the race for all participants
                var allChao = await _repo.GetRaceInstanceChao(raceInstance);

                foreach (var segment in segments)
                {
                    var chao = allChao.FirstOrDefault(x => x.Id.Value == segment.ChaoId);
                    var updatedSegment = await ProcessSegmentForChao(template, segment, chao);
                    await _repo.UpdateRaceInstanceSegment(updatedSegment);
                }

                var fastestSegment = segments.OrderBy(x => x.SegmentTimeSeconds.GetValueOrDefault(0)).FirstOrDefault();
                var fastestChao = allChao.FirstOrDefault(x => x.Id.Value == fastestSegment.ChaoId);

                // TODO: Report current status of the race with an embed instead (e.g. positions, elapsed time)
                await ctx.Reply($"{fastestChao.Name} is in the lead in the {race.Name} race!");

                var startTimer = new Timer(_ =>
                {
                    var __ = UpdateRaceSegments(ctx, race, raceInstance, index+1);
                }, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
            }
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

            // It's possible to enter a segment after flying through the previous one, resulting in carryover elevation
            var flyDistance = await ProcessFlightForSegment(template, segment, chao);
            await ProcessTerrainForSegment(template, segment, chao, flyDistance);

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
                    : template.EndElevation - template.StartElevation;
                var terrainTime = CalculateTerrainTime(chao, remainingDistance, template.TerrainType);
                segment.SegmentTimeSeconds += terrainTime;

                // We always end at the segment's end elevation if we're not mid-air
                segment.EndElevation = template.EndElevation;

                // Terrain travel uses stamina - make sure we can actually complete the segment
                var staminaCost = (int)(terrainTime * template.StaminaLossMultiplier);
                if (staminaCost <= segment.StartStamina)
                {
                    // Carry over the stamina we have left into the next segment
                    segment.EndStamina = segment.StartStamina.GetValueOrDefault(0) - staminaCost;
                    segment.State = RaceInstanceChaoSegment.SegmentStates.Completed;
                }
                else
                {
                    // The chao couldn't make it... collapsed, fell, drowned, etc...
                    segment.EndStamina = 0;
                    segment.State = RaceInstanceChaoSegment.SegmentStates.Retired;

                    // Make sure the remaining segments are marked as retired too
                    await _repo.RetireInstanceChao(chao.Id.Value, segment.RaceInstanceId);
                }
            }
        }

        private async Task<int> ProcessFlightForSegment(RaceSegment template, RaceInstanceChaoSegment segment, Core.Chao chao)
        {
            var flyDistance = 0;
            if (segment.StartElevation > template.StartElevation)
            {
                // We're in the air! Fly until we can't anymore
                var fallSpeed = CalculateFlightFallSpeed(chao);
                var fallDistance = segment.StartElevation.GetValueOrDefault(template.EndElevation) - template.EndElevation;

                if (fallDistance > 0)
                {
                    // We're falling with style... so where do we end up?
                    var maxFlyTime = fallSpeed / fallDistance; // This is the time we have before gravity brings us down
                    flyDistance = CalculateFlightDistance(chao, maxFlyTime); // This is how far we'd fly in that time

                    // But if we will fly into the next segment, stop there - we don't know the elevation of the next segment yet
                    if (flyDistance > template.TerrainDistance)
                    {
                        // Check where our elevation ends so we keep flying in the next segment (or skip some climbing)
                        segment.EndElevation = CalculateFlightEndElevation(fallDistance, template.TerrainDistance, flyDistance);
                        flyDistance = template.TerrainDistance; // Set the actual distance flown based on the size of the segment
                        segment.State = RaceInstanceChaoSegment.SegmentStates.Completed; // We already know we finished the segment since there's no stamina consumption while flying
                    }

                    // Confirm how long we're airborne and add that to our race time
                    var flyTime = CalculateFlightTime(chao, flyDistance);
                    segment.SegmentTimeSeconds += flyTime;
                }
            }

            return flyDistance;
        }

        private int CalculateStaminaForChao(Core.Chao chao)
        {
            // NOTE: This might need adjustment for balance.
            // The base value is based on the duration of the beginner races.
            // The multiplier for the stat is based on the high end races including a 1 minute race with triple stamina drain.
            return 45 + chao.StaminaValue / 20;
        }

        private int CalculateFlightFallSpeed(Core.Chao chao)
        {
            // Chao always try to fly when they're falling, resulting in a constant fall velocity dependent solely on their flying skill
            return (int)(6 * Math.Exp(-0.000285 * chao.FlyValue));
        }

        private int CalculateFlightDistance(Core.Chao chao, int totalFlyTime)
        {
            return (int)(totalFlyTime / Math.Exp(-0.000114 * chao.FlyValue));
        }
        
        private int CalculateFlightTime(Core.Chao chao, int distance)
        {
            // This should be the flight distance (not the segment distance)
            // We'll use a different stat in the calculation once we land
            return (int)(distance * Math.Exp(-0.000114 * chao.FlyValue));
        }

        private int CalculateFlightEndElevation(int fallDistance, int segmentDistance, int totalFlyDistance)
        {
            // Our total flight creates a triangle, and the segmented portion creates a smaller similar triangle inside it
            // We can calculate the end elevation using: end = (segment distance) / (total flight distance) * (distance fallen)
            return segmentDistance / totalFlyDistance * fallDistance;
        }

        private int CalculateTerrainTime(Core.Chao chao, int distance, RaceSegment.RaceTerrains terrainType)
        {
            switch (terrainType)
            {
                case RaceSegment.RaceTerrains.Power:
                    return CalculateClimbingTime(chao, distance);
                case RaceSegment.RaceTerrains.Swim:
                    return CalculateSwimmingTime(chao, distance);
                case RaceSegment.RaceTerrains.Run:
                default:
                    return CalculateRunningTime(chao, distance);
            }
        }

        // NOTE: The magic numbers in these functions come from tests run on the Steam version of SA2B. Might need adjustment.
        private int CalculateClimbingTime(Core.Chao chao, int distance)
        {
            return (int)(distance * Math.Exp(-0.000436 * chao.PowerValue));
        }

        private int CalculateSwimmingTime(Core.Chao chao, int distance)
        {
            return (int)(distance * Math.Exp(-0.000155 * chao.SwimValue));
        }

        private int CalculateRunningTime(Core.Chao chao, int distance)
        {
            return (int)(distance * Math.Exp(-0.000114 * chao.RunValue));
        }

        /*
        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var system = await _repo.GetGarden(target.GardenId);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(system, target, ctx.Guild));
        }
        */
    }
}