using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;
using NodaTime.Text;

namespace ChaoWorld.Core
{
    public class Race
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

    public class RaceSegment
    {
        public int Id { get; set; }
        public int RaceId { get; set; }
        public int RaceIndex { get; set; }
        public string Description { get; set; }
        public RaceTerrains TerrainType { get; set; }
        public int StartElevation { get; set; }
        public int EndElevation { get; set; }
        public int TerrainDistance { get; set; }
        public double StaminaLossMultiplier { get; set; }
        public int SwimRating { get; set; }
        public int FlyRating { get; set; }
        public int RunRating { get; set; }
        public int PowerRating { get; set; }
        public int IntelligenceRating { get; set; }
        public int LuckRating { get; set; }

        public enum RaceTerrains
        {
            Run, Swim, Power
        }
    }

    public class RaceInstance
    {
        public long Id { get; set; }
        public int RaceId { get; set; }
        public RaceStates State { get; set; }
        public Instant CreatedOn { get; set; }
        public Instant? ReadyOn { get; set; }
        public Instant? CompletedOn { get; set; }
        public long? WinnerChaoId { get; set; }
        public int? TimeElapsedSeconds { get; set; }
        public Duration TimeElapsed {
            get
            {
                return Duration.FromSeconds(TimeElapsedSeconds.GetValueOrDefault(0));
            }
        }
        public enum RaceStates
        {
            [Description("New")] New,
            [Description("Preparing")] Preparing,
            [Description("In Progress")] InProgress,
            [Description("Completed")] Completed,
            [Description("Canceled")] Canceled
        }
    }

    public class RaceInstanceChao
    {
        public long RaceInstanceId { get; set; }
        public long ChaoId { get; set; }
        public RaceInstance.RaceStates State { get; set; }
        public int? TotalTimeSeconds { get; set; }
        public Duration TotalTime
        {
            get
            {
                return Duration.FromSeconds(TotalTimeSeconds.GetValueOrDefault(0));
            }
        }
        public int? FinishPosition { get; set; }
    }

    public class RaceInstanceChaoSegment
    {
        public long RaceInstanceId { get; set; }
        public int RaceSegmentId { get; set; }
        public long ChaoId { get; set; }
        public SegmentStates State { get; set; }
        public int? SegmentTimeSeconds { get; set; }
        public Duration SegmentTime
        {
            get
            {
                return Duration.FromSeconds(SegmentTimeSeconds.GetValueOrDefault(0));
            }
        }
        public int? TotalTimeSeconds { get; set; }
        public Duration TotalTime
        {
            get
            {
                return Duration.FromSeconds(TotalTimeSeconds.GetValueOrDefault(0));
            }
        }
        public int? StartStamina { get; set; }
        public int? EndStamina { get; set; }
        public int? StartElevation { get; set; }
        public int? EndElevation { get; set; }

        public enum SegmentStates 
        {
            [Description("Not Started")] NotStarted,
            [Description("Completed")] Completed,
            [Description("Retired")] Retired
        }
    }
}