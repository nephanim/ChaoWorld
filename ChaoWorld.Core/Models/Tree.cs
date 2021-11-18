using System;

using Dapper.Contrib.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;

namespace ChaoWorld.Core
{
    public class Tree
    {
        public long Id { get; }
        public int GardenId { get; set; }
        public string Name { get; set; }
        public int FruitTypeId { get; set; }
        public int FruitQuantity { get; set; }
        public int Health { get; set; }
        public Instant CreatedOn { get; set; }
        public int Age
        {
            get
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                var elapsed = now - CreatedOn;
                return (elapsed.Days / 7);
            }
        }
        public Instant NextWatering { get; set; }
        public string TimeUntilWatering
        {
            get
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                if (NextWatering < now)
                {
                    return "Ready";
                }
                else
                {
                    var duration = NextWatering - now;
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
    }
}