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
    public class Expedition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinimumChao { get; set; }
        public int MaximumChao { get; set; }
        public int PrizeRings { get; set; }
        public int Difficulty { get; set; }
        public int MinDurationMinutes { get; set; }
        public int MaxDurationMinutes { get; set; }
        public int ProgressRequired { get; set; }
        public int SwimRating { get; set; }
        public int FlyRating { get; set; }
        public int RunRating { get; set; }
        public int PowerRating { get; set; }
        public int StaminaRating { get; set; }
        public int IntelligenceRating { get; set; }
        public int LuckRating { get; set; }
    }

    public class ExpeditionPrerequisite
    {
        public int ExpeditionId { get; set; }
        public int PrerequisiteId { get; set; }
    }

    public class GardenExpedition
    {
        public int GardenId { get; set; }
        public int ExpeditionId { get; set; }
        public bool IsComplete { get; set; }
    }

    public class ExpeditionInstance
    {
        public long Id { get; set; }
        public int ExpeditionId { get; set; }
        public int LeaderId { get; set; }
        public ExpeditionStates State { get; set; }
        public Instant CreatedOn { get; set; }
        public Instant ExpiresOn { get; set; }
        public Instant? CompletedOn { get; set; }
        public long? MVPChaoId { get; set; }
        public int? TimeElapsedSeconds { get; set; }
        public int? TotalContribution { get; set; }
        public Duration TimeElapsed {
            get
            {
                return Duration.FromSeconds(TimeElapsedSeconds.GetValueOrDefault(0));
            }
        }
        public string TimeRemaining
        {
            get
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                if (ExpiresOn < now)
                {
                    return "N/A";
                }
                else
                {
                    var duration = ExpiresOn - now;
                    var timeRemaining = "24 hours";
                    if (duration.Hours >= 2)
                        timeRemaining = $"{duration.Hours} hours";
                    else if (duration.Hours >= 1)
                        timeRemaining = $"1 hour";
                    else if (duration.Minutes >= 2)
                        timeRemaining = $"{duration.Minutes} minutes";
                    else if (duration.Minutes >= 1)
                        timeRemaining = $"1 minute";
                    else
                        timeRemaining = $"{duration.Seconds} seconds";

                    return timeRemaining;
                }
            }
        }
        public enum ExpeditionStates
        {
            [Description("New")] New,
            [Description("Preparing")] Preparing,
            [Description("In Progress")] InProgress,
            [Description("Completed")] Completed,
            [Description("Canceled")] Canceled
        }
    }

    public class ExpeditionInstanceChao
    {
        public long ExpeditionInstanceId { get; set; }
        public long ChaoId { get; set; }
        public Instant CreatedOn { get; set; }
        public int Contribution { get; set; }
        public int? FinishRank { get; set; }
    }
}