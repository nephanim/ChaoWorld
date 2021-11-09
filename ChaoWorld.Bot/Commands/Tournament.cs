using ChaoWorld.Core;
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

        public async Task ProcessMatch(Context ctx, TournamentInstanceMatch match)
        {
            await _repo.LogMessage($"Processing match {match.Id} for tournament instance {match.TournamentInstanceId}");

            // We'll use a counter to track time elapsed and manage match events
            var matchTime = 0;
            
            // Initialize the left chao's parameters
            match.Left = new TournamentCombatant();
            match.Left.Chao = await _repo.GetChao(match.LeftChaoId);
            match.Left.RemainingHealth = GetStartingHealthForChao(match.Left.Chao);
            match.Left.RemainingZeal = 100;
            match.Left.EdgeDistance = 50;
            match.Left.AttackDelay = GetAttackDelay(match.Left.Chao);
            match.Left.NextAttackIn = match.Left.AttackDelay;

            // Initialize the right chao's parameters
            match.Right = new TournamentCombatant();
            match.Right.Chao = await _repo.GetChao(match.RightChaoId);
            match.Right.RemainingHealth = GetStartingHealthForChao(match.Right.Chao);
            match.Right.RemainingZeal = 100;
            match.Right.EdgeDistance = 50;
            match.Right.AttackDelay = GetAttackDelay(match.Right.Chao);
            match.Right.NextAttackIn = match.Right.AttackDelay;

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
            while (match.State == TournamentInstance.TournamentStates.InProgress && matchTime < 300) // Putting a hard cap at 5 minutes just in case
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

                    // Check whether the match is over
                    if (defender.RemainingHealth < 0 || ringout)
                        match.WinnerChaoId = attacker.Chao.Id.Value;
                }
                else
                {
                    // Attack missed
                    // Attacker loses some zeal, no health changes
                    attackLanded = false;
                    var zealLoss = GetZealLoss(attacker.Chao);
                    attacker.RemainingZeal -= zealLoss;
                }

                // Wait to simulate real-time combat
                await Task.Delay(cycleTime * 1000);
                matchTime += cycleTime;

                if (!match.WinnerChaoId.HasValue)
                {
                    // If either chao has run out of zeal, they will lose some time regaining their composure
                    attacker.RemainingZeal = GetNormalizedZeal(attacker.RemainingZeal);
                    defender.RemainingZeal = GetNormalizedZeal(defender.RemainingZeal);
                    attackerRecovering = attacker.RemainingZeal <= 0;
                    defenderRecovering = defender.RemainingZeal <= 0;
                    if (attackerRecovering)
                    {
                        attacker.NextAttackIn += GetZealRecoveryTime(attacker.Chao) + attacker.AttackDelay;
                        attacker.RemainingZeal = 100;
                    }
                    if (defenderRecovering)
                    {
                        defender.NextAttackIn += GetZealRecoveryTime(defender.Chao) + defender.AttackDelay;
                        defender.RemainingZeal = 100;
                    }
                }
                else
                {
                    // This will be our last cycle since the match has been decided
                    match.State = TournamentInstance.TournamentStates.Completed;
                    match.ElapsedTimeSeconds = matchTime;
                }

                // Report back what happened for curious onlookers
                if (attackLanded)
                    if (defender.RemainingHealth <= 0)
                        await ctx.Reply($"{Emojis.Megaphone} {attacker.Chao.Name} strikes the finishing blow. {defender.Chao.Name} is down for the count.");
                    else if (ringout)
                        await ctx.Reply($"{Emojis.Megaphone} {attacker.Chao.Name} lands a powerful hit, launching {defender.Chao.Name} out of the ring.");
                    else if (defenderRecovering)
                        await ctx.Reply($"{Emojis.Megaphone} {attacker.Chao.Name} sent {defender.Chao.Name} to their knees. {defender.Chao.Name} is catching their breath.");
                    else
                        await ctx.Reply($"{Emojis.Megaphone} {attacker.Chao.Name} lands a clean hit, but {defender.Chao.Name} stands strong.");
                else
                    if (attackerRecovering)
                        await ctx.Reply($"{Emojis.Megaphone} {defender.Chao.Name} dodges {attacker.Chao.Name}'s attack, and {attacker.Chao.Name} stumbles.");
                    else
                        await ctx.Reply($"{Emojis.Megaphone} {defender.Chao.Name} expertly evades {attacker.Chao.Name}'s charge.");
            }

            if (!match.WinnerChaoId.HasValue)
            {
                // We aborted processing the match because it's not getting anywhere
                // The chao could both be tanks with wet noodles for arms, or something could be wrong
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

                match.State = match.WinnerChaoId.HasValue
                    ? TournamentInstance.TournamentStates.Completed
                    : TournamentInstance.TournamentStates.Canceled; // I give up, are you guys actually the same chao?
            }

            await _repo.LogMessage($"{match.State.GetDescription()} match {match.Id} for tournament instance {match.TournamentInstanceId} (elapsed time: {match.ElapsedTimeSeconds})");

            // WE NEED TO UPDATE THE DB
            // also need to make sure the next match is queued up
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
            return (int)Math.Min(1500, (
                    100 + attacker.PowerValue /
                        (2.0 + (defender.SwimValue / (1.0 + attacker.PowerValue))) * randomFactor
                ));
        }
        private int GetKnockback(Core.Chao attacker, Core.Chao defender)
        {
            // This is probably more reasonable... Consider you start with 50 units of buffer
            // Weak attacker, strong defender -- 5
            // Evenly matched -- 15
            // Strong attacker, weak defender -- 100 (instant ringout if the hit lands)
            // Additional variation of +/- 10%
            var randomFactor = new Random().Next(90, 110) / 100.0;
            return (int)Math.Min(100, (
                    5.0 + (attacker.PowerValue / (1.0 + defender.FlyValue)) * 10.0 * randomFactor
                ));
        } 

        private bool CheckDodge(Core.Chao attacker, Core.Chao defender)
        {
            // First see if we're just lucky
            var attackingLuckRoll = new Random().Next(1, attacker.LuckValue + 30);
            var defendingLuckRoll = new Random().Next(1, defender.LuckValue + 30);
            if (defendingLuckRoll > attackingLuckRoll * 2)
                return true;

            // Then see if our flying beats their running
            var attackingRunRoll = new Random().Next(1, attacker.RunValue + 30);
            var defendingFlyRoll = new Random().Next(1, defender.FlyValue + 30);
            if (defendingFlyRoll > attackingRunRoll * 2)
                return true;

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
    }
}
