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
        public int Difficulty { get; set; }
        public double SwimPercentage { get; set; }
        public double FlyPercentage { get; set; }
        public double RunPercentage { get; set; }
        public double PowerPercentage { get; set; }
        public double IntelligencePercentage { get; set; }
        public double LuckPercentage { get; set; }

        public static string GetDifficultyString(int difficulty)
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
        public int TerrainDifficulty { get; set; }
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

    public class RaceInstanceBan
    {
        public long RaceInstanceId { get; set; }
        public int GardenId { get; set; }
        public Instant ExpiresOn { get; set; }
    }

    public class ChaoRaceStats
    {
        public long ChaoId { get; set; }
        public long TotalRaces { get; set; }
        public long TotalWins { get; set; }
        public long TotalRetires { get; set; }
        public double WinRate
        {
            get
            {
                return TotalRaces > 0
                    ? (double)TotalWins / (double)TotalRaces * 100.0
                    : 0;
            }
        }
        public double RetireRate
        {
            get
            {
                return TotalRaces > 0
                    ? (double)TotalRetires / (double)TotalRaces * 100.0
                    : 0;
            }
        }
    }
}