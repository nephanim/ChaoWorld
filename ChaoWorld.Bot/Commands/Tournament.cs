using ChaoWorld.Core;
using Myriad.Rest.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChaoWorld.Bot
{
    public class Tournament
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Tournament(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task ViewTournamentInstance(Context ctx, Core.TournamentInstance target)
        {
            var tournament = await _repo.GetTournamentById(target.TournamentId);
            await ctx.Reply(embed: await _embeds.CreateTournamentEmbed(ctx, tournament, target));
        }

        public async Task UpdatePingSettings(Context ctx)
        {
            var allowPings = false;
            if (ctx.Match("enable", "on", "yes", "1", "true", "accept", "allow"))
                allowPings = true;
            await _repo.UpdateTournamentPingSetting(ctx.Author.Id, allowPings);
            if (allowPings)
                await ctx.Reply($"{Emojis.Success} Tournament pings are now enabled.");
            else
                await ctx.Reply($"{Emojis.Success} Tournament pings are now disabled.");
        }

        public async Task JoinTournament(Context ctx, Core.Chao chao, TournamentInstance instance)
        {
            ctx.CheckOwnChao(chao); //You can only enter your own chao in a tournament...

            var activeInRace = await _repo.GetActiveRaceByGarden(chao.GardenId.Value);
            var activeInTourney = await _repo.GetActiveTournamentByGarden(chao.GardenId.Value);

            if (instance.State == TournamentInstance.TournamentStates.InProgress
                || instance.State == TournamentInstance.TournamentStates.Completed
                || instance.State == TournamentInstance.TournamentStates.Canceled)
            {
                // Tourney isn't joinable - sorry!
                await ctx.Reply($"{Emojis.Error} This tournament is {Core.MiscUtils.GetDescription(instance.State).ToLower()} and can no longer be joined.");
            }
            else if (activeInRace != null)
            {
                // There's a chao in this garden that's already participating in a race.
                var race = await _repo.GetRaceById(activeInRace.RaceId);
                await ctx.Reply($"{Emojis.Error} You already have a chao participating in a {race.Name} race. Please support your chao in that race first!");
            }
            else if (activeInTourney != null)
            {
                // There's a chao in this garden that's already participating in a tournament.
                var tourney = await _repo.GetTournamentById(activeInTourney.TournamentId);
                await ctx.Reply($"{Emojis.Error} You already have a chao participating in a {tourney.Name} tournament. Please support your chao's tournament first!");
            }
            else
            {
                // Race is in a joinable stat
                // Check whether we've reached the minimum number of chao for the tournament
                var tourney = await _repo.GetTournamentByInstanceId(instance.Id);
                var currentChaoCount = await _repo.GetTournamentInstanceChaoCount(instance.Id);
                if (tourney == null)
                {
                    await ctx.Reply($"{Emojis.Error} Unable to read the requested tournament. (Tournament Instance ID: {instance.Id})");
                }
                else if (currentChaoCount >= tourney.MaximumChao)
                {
                    // The race is full - don't join it
                    await ctx.Reply($"{Emojis.Error} This tournament has reached the participant limit ({currentChaoCount}/{tourney.MaximumChao}). Please wait for the next tournament.");
                }
                else
                {
                    // The tournament is not full - join it
                    await _repo.JoinChaoToTournamentInstance(instance, chao);
                    currentChaoCount++;
                    await ctx.Reply($"{Emojis.Success} {chao.Name} has joined the {tourney.Name}. Do your best!");

                    // See whether this chao joining puts us at the required threshold to start
                    if (currentChaoCount >= tourney.MinimumChao && instance.State == TournamentInstance.TournamentStates.New)
                    {
                        // We've reached the minimum threshold, and haven't begun preparing the tournament
                        // Check how long we're supposed to wait before we start
                        instance.ReadyOn = NodaTime.SystemClock.Instance.GetCurrentInstant();
                        instance.State = TournamentInstance.TournamentStates.Preparing;
                        await _repo.UpdateTournamentInstance(instance);

                        await ctx.Reply($"{Emojis.Megaphone} The {tourney.Name} tournament will start in {tourney.ReadyDelayMinutes} minutes. Use `!tournament {instance.Id} join {{chao id/name}}` to participate.");

                        var readyDelay = TimeSpan.FromMinutes(tourney.ReadyDelayMinutes);
                        //Thread.Sleep(race.ReadyDelayMinutes * 60000);
                        await Task.Delay(readyDelay);
                        await StartTournament(ctx, tourney, instance);
                    }
                }
            }
        }

        public async Task LeaveTournament(Context ctx)
        {
            ctx.CheckGarden();

            var activeTourney = await _repo.GetActiveTournamentByGarden(ctx.Garden.Id.Value);
            if (activeTourney != null)
            {
                if (activeTourney.State == TournamentInstance.TournamentStates.New || activeTourney.State == TournamentInstance.TournamentStates.Preparing)
                {
                    var tourneyChao = await _repo.GetChaoInTournament(activeTourney);
                    var chao = tourneyChao.FirstOrDefault(x => x.GardenId == ctx.Garden.Id);
                    if (chao != null)
                        await _repo.RemoveChaoFromTournamentInstance(activeTourney, chao);
                    await ctx.Reply($"{Emojis.Success} You are no longer waiting for the tournament to start.");
                }
                else
                    await ctx.Reply($"{Emojis.Error} You can no longer withdraw from the tournament. Please wait for it to finish.");
            }
            else
                await ctx.Reply($"{Emojis.Error} None of your chao are currently participating in a tournament.");
        }

        public async Task StartTournament(Context ctx, Core.Tournament tourney, TournamentInstance instance)
        {
            // The ready delay period is over -- time to fight!
            if (instance.State == TournamentInstance.TournamentStates.Preparing || instance.State == TournamentInstance.TournamentStates.Preparing)
            {
                // Start the race!
                instance.State = TournamentInstance.TournamentStates.InProgress;
                await _repo.UpdateTournamentInstance(instance);
                await ctx.Reply($"{Emojis.Megaphone} The {tourney.Name} tournament is starting! Good luck to all participants!");

                try
                {
                    // Determine how many slots to fill with NPC chao and select random chao to fill those
                    var currentChaoCount = await _repo.GetTournamentInstanceChaoCount(instance.Id);
                    var limit = GetTourneyFillLimit(currentChaoCount, tourney.MaximumChao);
                    await _repo.LogMessage($"Tournament instance {instance.Id} has {currentChaoCount} participants. Filling to {limit}.");

                    var joiningNPCs = new List<Core.Chao>();
                    while (currentChaoCount < limit)
                    {
                        var npc = await _repo.GetRandomChao(0); // Garden 0 is a special holding place reserved for NPCs
                        if (joiningNPCs.All(x => x.Id != npc.Id))
                        {
                            joiningNPCs.Add(npc);
                            await _repo.JoinChaoToTournamentInstance(instance, npc);
                            currentChaoCount++;
                        } // else we loop again without incrementing, so we get another one
                    }
                }
                catch (Exception e)
                {
                    await _repo.LogMessage($"Failed to fill race instance {instance.Id} of tournament {tourney.Id} with NPC chao: {e.Message}");
                }

                // Now we have a full roster - sound the gong
                await ProcessTournament(ctx, tourney, instance);
            }
            else
            {
                await _repo.LogMessage($"Tried to start tournament instance {instance.Id} of tournament {instance.TournamentId}, but its state was {instance.State}");
                // Something weird happened and the tournament is already running / finished... do nothing
            }
        }

        public async Task ProcessTournament(Context ctx, Core.Tournament tourney, TournamentInstance instance)
        {
            var combatants = (await _repo.GetTournamentInstanceChao(instance));
            var roundIndex = 1;
            var matchIndex = 1;
            var chaoIndex = 0;
            instance.TotalTimeElapsedSeconds = 0;
            instance.RoundElapsedTimeSeconds = 0;

            // As long as we have more rounds to run, queue them up and process them
            instance.Rounds = (int)(Math.Log(combatants.Count()) / Math.Log(2));
            while (roundIndex <= instance.Rounds) {

                // As long as we have more matches to run in this round, queue them up and process them
                instance.Matches = (int)(Math.Pow(2, instance.Rounds - roundIndex));
                var remainingCombatants = (await _repo.GetTournamentInstanceChao(instance)).Where(x => x.State != TournamentInstance.TournamentStates.Canceled).ToArray();
                while (matchIndex <= instance.Matches)
                {
                    // Who will fight?
                    var leftChao = remainingCombatants[chaoIndex];
                    chaoIndex++;
                    var rightChao = remainingCombatants[chaoIndex];
                    chaoIndex++;

                    // Run the match
                    var match = await _repo.AddMatch(instance, leftChao.ChaoId, rightChao.ChaoId, roundIndex, matchIndex);
                    await ProcessMatch(ctx, tourney, instance, match);
                    matchIndex++;
                }

                // No more matches to run this round - report progress and go on to the next round
                await ctx.Reply(embed: await _embeds.CreateTournamentRoundResultsEmbed(ctx, tourney, instance, roundIndex));
                roundIndex++;
                instance.TotalTimeElapsedSeconds += instance.RoundElapsedTimeSeconds;
                instance.RoundElapsedTimeSeconds = 0;
                matchIndex = 1;
                chaoIndex = 0;
            }

            // All rounds are complete - report the results
            await FinalizeTournament(ctx, tourney, instance);
        }

        public async Task ProcessMatch(Context ctx, Core.Tournament tourney, TournamentInstance instance, TournamentInstanceMatch match)
        {
            // TODO: Clean up this monster method before it starts destroying villages
            await _repo.LogMessage($"Processing match {match.Id} for tournament instance {match.TournamentInstanceId}");

            // We'll use a counter to track time elapsed and manage match events
            var matchTime = 0;

            // Initialize the left chao's parameters
            match.Left = new TournamentCombatant();
            match.Left.Emoji = Emojis.BlueDiamond;
            match.Left.Chao = await _repo.GetChao(match.LeftChaoId);
            match.Left.RemainingHealth = GetStartingHealthForChao(match.Left.Chao);
            match.Left.RemainingZeal = 100;
            match.Left.EdgeDistance = 50;
            match.Left.AttackDelay = GetAttackDelay(match.Left.Chao);
            match.Left.NextAttackIn = match.Left.AttackDelay;

            // Initialize the right chao's parameters
            match.Right = new TournamentCombatant();
            match.Right.Emoji = Emojis.OrangeDiamond;
            match.Right.Chao = await _repo.GetChao(match.RightChaoId);
            match.Right.RemainingHealth = GetStartingHealthForChao(match.Right.Chao);
            match.Right.RemainingZeal = 100;
            match.Right.EdgeDistance = 50;
            match.Right.AttackDelay = GetAttackDelay(match.Right.Chao);
            match.Right.NextAttackIn = match.Right.AttackDelay;

            await _repo.LogMessage($"Combatant {match.Left.Chao.Id.Value}: {match.Left.RemainingHealth} HP / {match.Left.RemainingZeal} ZP / {match.Left.EdgeDistance}m / Attacking in {match.Left.NextAttackIn}s");
            await _repo.LogMessage($"Combatant {match.Right.Chao.Id.Value}: {match.Right.RemainingHealth} HP / {match.Right.RemainingZeal} ZP / {match.Right.EdgeDistance}m / Attacking in {match.Right.NextAttackIn}s");

            // Enter attack/defend cycle
            // In each iteration, we need to figure out:
            //  * Who's attacking / defending
            //  * How much time elapses before the attack
            //  * Whether the attack lands
            //  * Outcomes of the attack (potentially including more elapsed time)
            //  * Whether the match will continue
            TournamentCombatant attacker, defender;
            var attackerRecovering = false;
            var defenderRecovering = false;
            var attackLanded = false;
            var ringout = false;
            var matchTimeLimit = 300; // Putting a hard cap at 5 minutes just in case
            while (!match.WinnerChaoId.HasValue && matchTime < matchTimeLimit)
            {
                var cycleTime = 0;
                if (match.Left.NextAttackIn < match.Right.NextAttackIn)
                {
                    // Left will strike next
                    attacker = match.Left;
                    defender = match.Right;
                    cycleTime += match.Left.NextAttackIn;

                    // Right's next attack gets closer, left's next attack gets farther away
                    match.Right.NextAttackIn -= match.Left.NextAttackIn;
                    match.Left.NextAttackIn = match.Left.AttackDelay;
                }
                else
                {
                    // Right will strike next
                    attacker = match.Right;
                    defender = match.Left;
                    cycleTime += match.Right.NextAttackIn;

                    // Left's next attack gets closer, right's next attack gets farther away
                    match.Left.NextAttackIn -= match.Right.NextAttackIn;
                    match.Right.NextAttackIn = match.Right.AttackDelay;
                }
                await _repo.LogMessage($"{attacker.Chao.Id} is attacking {defender.Chao.Id.Value} in tournament match {match.Id} for tournament instance {match.TournamentInstanceId}");

                // We've reached the next attack - see if it lands
                if (!CheckDodge(attacker.Chao, defender.Chao))
                {
                    // Attack landed
                    // Calculate changes to health and zeal
                    attackLanded = true;
                    var damage = GetDamage(attacker.Chao, defender.Chao);
                    var zealGain = GetZealGain(attacker.Chao);
                    var zealLoss = GetZealLoss(defender.Chao);
                    defender.RemainingHealth -= damage;
                    defender.RemainingZeal -= zealLoss;
                    attacker.RemainingZeal += zealGain;

                    // Calculate knockback and check for ringout
                    // Attacker gains some of the knockback distance as edge distance
                    var knockback = GetKnockback(attacker.Chao, defender.Chao);
                    ringout = knockback >= defender.EdgeDistance;
                    defender.EdgeDistance -= knockback / 2;
                    attacker.EdgeDistance += knockback / 2;
                }
                else
                {
                    // Attack missed
                    // Attacker loses some zeal, no health changes
                    attackLanded = false;
                    var zealLoss = GetZealLoss(attacker.Chao);
                    attacker.RemainingZeal -= zealLoss;
                }

                // If either chao has run out of zeal, they will lose some time regaining their composure
                // Their remaining HP is also reduced by any zeal they need to recover
                attacker.RemainingZeal = GetNormalizedZeal(attacker.RemainingZeal);
                defender.RemainingZeal = GetNormalizedZeal(defender.RemainingZeal);
                attackerRecovering = attacker.RemainingZeal <= 0;
                defenderRecovering = defender.RemainingZeal <= 0;
                if (attackerRecovering)
                {
                    attacker.NextAttackIn += GetZealRecoveryTime(attacker.Chao) + attacker.AttackDelay;
                    attacker.RemainingHealth -= Math.Min(0, 100 - attacker.RemainingZeal);
                    attacker.RemainingZeal = 100;
                }
                if (defenderRecovering)
                {
                    defender.NextAttackIn += GetZealRecoveryTime(defender.Chao) + defender.AttackDelay;
                    defender.RemainingHealth -= Math.Min(0, 100 - defender.RemainingZeal);
                    defender.RemainingZeal = 100;
                }

                await _repo.LogMessage($"Combatant {attacker.Chao.Id.Value}: {attacker.RemainingHealth} HP / {attacker.RemainingZeal} ZP / {attacker.EdgeDistance}m / Attacking in {attacker.NextAttackIn}s");
                await _repo.LogMessage($"Combatant {attacker.Chao.Id.Value}: {attacker.RemainingHealth} HP / {attacker.RemainingZeal} ZP / {attacker.EdgeDistance}m / Attacking in {attacker.NextAttackIn}s");

                // Check whether the match is over
                if (defender.RemainingHealth < 0 || ringout)
                    match.WinnerChaoId = attacker.Chao.Id.Value; // This will allow us to exit the loop

                // Wait to simulate real-time combat
                await Task.Delay(cycleTime * 1000);
                matchTime += cycleTime;

                // Report back what happened for curious onlookers
                await ReplyWithRaceEvents(ctx, attacker, defender, attackerRecovering, defenderRecovering, attackLanded, ringout);
            }

            HandleMatchTimeout(match);

            // Give chao some stamina for their exercise (all other stat increases are baked in already)
            match.Left.Chao.RaiseStamina(matchTime / 10);
            match.Right.Chao.RaiseStamina(matchTime / 10);
            // Make sure all the stat increases are persisted
            await _repo.UpdateChao(match.Left.Chao);
            await _repo.UpdateChao(match.Right.Chao);

            match.State = TournamentInstance.TournamentStates.Completed;
            match.ElapsedTimeSeconds = Math.Min(matchTime, matchTimeLimit);
            instance.RoundElapsedTimeSeconds += match.ElapsedTimeSeconds;
            await _repo.UpdateMatch(match);

            var winner = match.WinnerChaoId.GetValueOrDefault(match.LeftChaoId) == match.LeftChaoId
                ? match.Left.Chao
                : match.Right.Chao;
            var loser = match.WinnerChaoId.GetValueOrDefault(match.LeftChaoId) == match.LeftChaoId
                ? match.Right.Chao
                : match.Left.Chao;
            await _repo.FinalizeTournamentInstanceChaoForMatch(match.TournamentInstanceId, loser.Id.Value, match.RoundNumber);

            await ctx.Reply(embed: await _embeds.CreateTournamentMatchResultsEmbed(ctx, tourney, instance, match, winner, loser));
        }

        private async Task ReplyWithRaceEvents(Context ctx, TournamentCombatant attacker, TournamentCombatant defender, bool attackerRecovering, bool defenderRecovering, bool attackLanded, bool ringout)
        {
            if (attackLanded)
                if (defender.RemainingHealth <= 0)
                    await ctx.Reply(GetFinalStrikeMessage(attacker, defender));
                else if (ringout)
                    await ctx.Reply(GetRingoutMessage(attacker, defender));
                else if (defenderRecovering)
                    await ctx.Reply(GetKnockdownMessage(attacker, defender));
                else
                    await ctx.Reply(GetNormalHitMessage(attacker, defender));
            else
                                if (attackerRecovering)
                await ctx.Reply(GetCriticalDodgeMessage(attacker, defender));
            else
                await ctx.Reply(GetNormalDodgeMessage(attacker, defender));
        }

        private static void HandleMatchTimeout(TournamentInstanceMatch match)
        {
            if (!match.WinnerChaoId.HasValue)
            {
                // We aborted processing the match because  of a timeout (it's not getting anywhere)
                // The chao could both be tanks with wet noodles for arms, math could be wrong, ...
                // Try to decide the match based on current progress
                if (match.Left.RemainingHealth > match.Right.RemainingHealth)
                    match.WinnerChaoId = match.Left.Chao.Id.Value;
                else if (match.Right.RemainingHealth > match.Left.RemainingHealth)
                    match.WinnerChaoId = match.Right.Chao.Id.Value;
                else if (match.Left.EdgeDistance > match.Right.EdgeDistance)
                    match.WinnerChaoId = match.Left.Chao.Id.Value;
                else if (match.Right.EdgeDistance > match.Left.EdgeDistance)
                    match.WinnerChaoId = match.Right.Chao.Id.Value;
                else if (match.Left.RemainingZeal > match.Right.RemainingZeal)
                    match.WinnerChaoId = match.Left.Chao.Id.Value;
                else if (match.Right.RemainingZeal > match.Left.RemainingZeal)
                    match.WinnerChaoId = match.Right.Chao.Id.Value;
                else
                    match.WinnerChaoId = match.Left.Chao.Id.Value; // This better not happen often...
            }
        }

        public async Task FinalizeTournament(Context ctx, Core.Tournament tournament, TournamentInstance instance)
        {
            // Race is done - finish it!
            var winner = (await _repo.GetTournamentInstanceChao(instance)).Where(x => x.State != TournamentInstance.TournamentStates.Canceled).FirstOrDefault();
            instance.WinnerChaoId = winner.ChaoId;
            instance.State = TournamentInstance.TournamentStates.Completed;
            await _repo.UpdateTournamentInstance(instance);

            await _repo.FinalizeTournamentInstanceChao(instance); // Set final results for each chao
            var prizeRings = GetPrizeAmount(tournament);
            await _repo.GiveTournamentRewards(instance, prizeRings); // Award the prize to the winner

            await ctx.Reply(embed: await _embeds.CreateTournamentEmbed(ctx, tournament, instance));

            var notifyList = await _repo.GetAccountsToPingForTournament(instance.Id);
            var notifyString = notifyList.Any() ? " " + string.Join(" ", notifyList.Select(x => $"<@{x}>").ToArray()) : "";
            var mentions = new AllowedMentions
            {
                Users = notifyList.ToArray()
            };

            await ctx.Reply($"{Emojis.Megaphone} The {tournament.Name} Tournament has finished. Thanks for playing!{notifyString}", mentions: mentions);
        }

        private int GetStartingHealthForChao(Core.Chao chao)
        {
            // Values range from 1000 HP (no stamina) to ~4333 HP (9999 stamina)
            // Will have to see how long it takes to chew through that much HP to avoid match timeouts
            return 1000 + chao.StaminaValue / 3;
        }

        private int GetAttackDelay(Core.Chao attacker)
        {
            // Values range from 12s delay (no running) to a minimum of 5s delay (~9999 running)
            return (int)Math.Max(5.0, 12.0 - (attacker.RunValue / 1428.0));
        }

        private int GetDamage(Core.Chao attacker, Core.Chao defender)
        {
            // This definitely may need some tuning
            // Weak attacker, strong defender -- 0
            // Evenly matched -- ~1/3 of power as damage
            // Strong attacker, weak defender -- ~1/2 of power as damage, capping at 1500
            // Additional variation of +/- 10%
            var randomFactor = new Random().Next(90, 110) / 100.0;
            var damage = (int)Math.Min(1500, (
                    100 + attacker.PowerValue /
                        (2.0 + (defender.SwimValue / (1.0 + attacker.PowerValue))) * randomFactor
                ));
            attacker.RaisePower(damage / 100); // Not sure whether to adjust the scale here further, will probably have to see in practice (right now this is like 10-20 max per fight?)
            defender.RaiseSwim(damage / 100);
            return damage;
        }

        private int GetKnockback(Core.Chao attacker, Core.Chao defender)
        {
            // This is probably more reasonable... Consider you start with 50 units of buffer
            // Weak attacker, strong defender -- 5
            // Evenly matched -- 15
            // Strong attacker, weak defender -- 100 (instant ringout if the hit lands)
            // Additional variation of +/- 10%
            var randomFactor = new Random().Next(90, 110) / 100.0;
            var knockback = (int)Math.Min(100, (
                    5.0 + (attacker.PowerValue / (1.0 + defender.FlyValue)) * 10.0 * randomFactor
                ));
            attacker.RaiseRun(knockback / 2); // Chao meet in the middle, so both travel the same distance
            defender.RaiseRun(knockback / 2);
            return knockback;
        }

        private bool CheckDodge(Core.Chao attacker, Core.Chao defender)
        {
            // First see if we're just lucky
            var attackingLuckRoll = new Random().Next(1, attacker.LuckValue + 30);
            var defendingLuckRoll = new Random().Next(1, defender.LuckValue + 30);
            if (defendingLuckRoll > attackingLuckRoll * 2)
            {
                defender.RaiseLuck(10); // Successful luck by dodge awards stat progress
                return true;
            }

            // Then see if our flying beats their running
            var attackingRunRoll = new Random().Next(1, attacker.RunValue + 30);
            var defendingFlyRoll = new Random().Next(1, defender.FlyValue + 30);
            if (defendingFlyRoll > attackingRunRoll * 2)
            {
                defender.RaiseFly(10); // Successful luck by flying awards stat progress
                return true;
            }

            // Couldn't dodge -- too bad
            return false;
        }

        private int GetZealGain(Core.Chao attacker)
        {
            // Range is 5 (no luck) -> ~60 (9999 luck)
            return (int)(5.0 * Math.Exp(0.00025 * attacker.LuckValue));
        }
        private int GetZealLoss(Core.Chao defender)
        {
            // Range is 30 (no intelligence) -> ~4 (9999 intelligence)
            return (int)(30.0 * Math.Exp(-0.0002 * defender.IntelligenceValue));
        }
        private int GetZealRecoveryTime(Core.Chao chao)
        {
            // Range is 10s (no luck) -> ~3s (9999 luck)
            return (int)(10.0 * Math.Exp(-0.0001 * chao.LuckValue));
        }

        private int GetNormalizedZeal(int zeal)
        {
            zeal = Math.Max(zeal, 0);
            zeal = Math.Min(zeal, 100);
            return zeal;
        }

        private int GetTourneyFillLimit(int currentNumber, int hardLimit)
        {
            var nextThreshold = 4;
            while (nextThreshold < currentNumber && nextThreshold < hardLimit)
            {
                nextThreshold *= 2;
            }
            return Math.Min(hardLimit, nextThreshold);
        }

        private int GetPrizeAmount(Core.Tournament tourney)
        {
            // This will reward anywhere from 50% to 150% of the listed prize amount for a race
            return (int)(tourney.PrizeRings * (0.5 + new Random().NextDouble()));
        }

        private string GetFinalStrikeMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            if (attacker.RemainingHealth <= 100)
                return $"{attacker.Emoji} {attacker.Chao.Name} brings {defender.Chao.Name} down with a final blow, but they're seeing stars.";
            if (attacker.RemainingHealth >= GetStartingHealthForChao(attacker.Chao))
                return $"{attacker.Emoji} {attacker.Chao.Name} finishes the opponent off without breaking a sweat.";
            switch(new Random().Next(1, 3))
            {
                case 1:
                    return $"{attacker.Emoji} {attacker.Chao.Name} strikes the finishing blow. {defender.Chao.Name} is down for the count.";
                case 2:
                    return $"{defender.Emoji} {defender.Chao.Name} staggers from {attacker.Chao.Name}'s next hit, falling unconscious.";
                case 3:
                default:
                    return $"{attacker.Emoji} {attacker.Chao.Name} gives it everything they've got! {defender.Chao.Name} buckles under the force and can no longer continue.";
            }
        }

        private string GetRingoutMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            if (GetKnockback(attacker.Chao, defender.Chao) >= 100)
                return $"{attacker.Emoji} {attacker.Chao.Name} effortlessly flings {defender.Chao.Name} from the ring. They didn't stand a chance.";
            if (GetKnockback(attacker.Chao, defender.Chao) <= 3)
                return $"{attacker.Emoji} As {attacker.Chao.Name} and {defender.Chao.Name} exchange blows on the very edge of the ring, {defender.Chao.Name} accidentally dodges out of bounds.";
            switch (new Random().Next(1, 4))
            {
                case 1:
                    return $"{attacker.Emoji} {attacker.Chao.Name} lands a powerful hit, launching {defender.Chao.Name} out of the ring.";
                case 2:
                    return $"{attacker.Emoji} {attacker.Chao.Name} catches {attacker.Chao.Name} off guard and throws them out of bounds.";
                case 3:
                    return $"{attacker.Emoji} {attacker.Chao.Name} knocks a weakened {defender.Chao.Name} onto their heels and shoves them out of bounds.";
                case 4:
                default:
                    return $"{defender.Emoji} {defender.Chao.Name} loses their footing trying to block {attacker.Chao.Name}'s attack. {defender.Chao.Name} falls out of the ring!";
            }
        }

        private string GetNormalHitMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            switch (new Random().Next(1, 4))
            {
                case 1:
                    return $"{attacker.Emoji} {attacker.Chao.Name} throws a punch at {defender.Chao.Name}.";
                case 2:
                    return $"{attacker.Emoji} {attacker.Chao.Name} spins and kicks {attacker.Chao.Name}.";
                case 3:
                    return $"{attacker.Emoji} {attacker.Chao.Name} headbutts {defender.Chao.Name}.";
                case 4:
                default:
                    return $"{defender.Emoji} {defender.Chao.Name} crosses their arms to block {attacker.Chao.Name}'s attack.";
            }
        }

        private string GetKnockdownMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            switch (new Random().Next(1, 4))
            {
                case 1:
                    return $"{attacker.Emoji} {attacker.Chao.Name} sent {defender.Chao.Name} to their knees. {defender.Chao.Name} is catching their breath.";
                case 2:
                    return $"{defender.Emoji} {defender.Chao.Name} is out of breath and can't defend themselves. {attacker.Chao.Name} gets a free hit in.";
                case 3:
                    return $"{attacker.Emoji} {attacker.Chao.Name} sweeps {defender.Chao.Name}'s feet out from below, bringing them to the ground.";
                case 4:
                default:
                    return $"{defender.Emoji} {defender.Chao.Name} takes a square hit and stumbles.";
            }
        }

        private string GetNormalDodgeMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            switch (new Random().Next(1, 4))
            {
                case 1:
                    return $"{defender.Emoji} {defender.Chao.Name} expertly evades {attacker.Chao.Name}'s charge.";
                case 2:
                    return $"{defender.Emoji} {defender.Chao.Name} barely sidesteps {attacker.Chao.Name}'s sudden attack";
                case 3:
                    return $"{attacker.Emoji} {attacker.Chao.Name} grabs {defender.Chao.Name} by the arm and takes a swing, but {defender.Chao.Name} breaks away.";
                case 4:
                default:
                    return $"{defender.Emoji} {defender.Chao.Name} glides backward to avoid a spinning strike.";
            }
        }

        private string GetCriticalDodgeMessage(TournamentCombatant attacker, TournamentCombatant defender)
        {
            if (defender.RemainingZeal >= 100)
                return $"{defender.Emoji} {defender.Chao.Name} seems to have total control of the situation. {attacker.Chao.Name} is out of breath.";
            switch (new Random().Next(1, 4))
            {
                case 1:
                    return $"{defender.Emoji} {defender.Chao.Name} dodges {attacker.Chao.Name}'s attack, and {attacker.Chao.Name} loses their balance.";
                case 2:
                    return $"{defender.Emoji} {defender.Chao.Name} trips {attacker.Chao.Name} while fluttering out of their path.";
                case 3:
                    return $"{defender.Emoji} {defender.Chao.Name} teases {attacker.Chao.Name} for missing. {attacker.Chao.Name} is discouraged.";
                case 4:
                default:
                    return $"{attacker.Emoji} {attacker.Chao.Name} is searching for an opening.";
            }
        }
    }
}
