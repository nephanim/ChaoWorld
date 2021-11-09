using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChaoWorld.Core
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Instant AvailableOn { get; set; }
        public int FrequencyMinutes { get; set; }
        public int ReadyDelayMinutes { get; set; }
        public bool IsEnabled { get; set; }
        public int MinimumChao { get; set; }
        public int MaximumChao { get; set; }
        public int PrizeRings { get; set; }
    }

    public class TournamentInstance
    {
        public long Id { get; set; }
        public int TournamentId { get; set; }
        public TournamentStates State { get; set; }
        public Instant CreatedOn { get; set; }
        public Instant? ReadyOn { get; set; }
        public Instant? CompletedOn { get; set; }
        public long? WinnerChaoId { get; set; }
        public enum TournamentStates
        {
            [Description("New")] New,
            [Description("Preparing")] Preparing,
            [Description("In Progress")] InProgress,
            [Description("Completed")] Completed,
            [Description("Canceled")] Canceled
        }
    }

    public class TournamentInstanceChao
    {
        public long TournamentInstanceId { get; set; }
        public long ChaoId { get; set; }
        public TournamentInstance.TournamentStates State { get; set; }
    }

    public class TournamentInstanceMatch
    {
        public long Id { get; set; }
        public long TournamentInstanceId { get; set; }
        public int RoundNumber { get; set; }
        public int RoundOrder { get; set; }
        public long LeftChaoId { get; set; }
        public long RightChaoId { get; set; }
        public TournamentCombatant Left { get; set; }
        public TournamentCombatant Right { get; set; }
        public long? WinnerChaoId { get; set; }
        public TournamentInstance.TournamentStates State { get; set; }
        public int ElapsedTimeSeconds { get; set; }
    }

    public class TournamentCombatant
    {
        public Chao Chao { get; set; }
        public int RemainingHealth { get; set; }
        public int RemainingZeal { get; set; }
        public int AttackDelay { get; set; }
        public int NextAttackIn { get; set; }
        public int EdgeDistance { get; set; }
    }
}
