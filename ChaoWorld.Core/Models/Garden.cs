using System;

using Dapper.Contrib.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;

namespace ChaoWorld.Core
{

    public readonly struct GardenId: INumericId<GardenId, int>
    {
        public int Value { get; }

        public GardenId(int value)
        {
            Value = value;
        }

        public bool Equals(GardenId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is GardenId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(GardenId left, GardenId right) => left.Equals(right);

        public static bool operator !=(GardenId left, GardenId right) => !left.Equals(right);

        public int CompareTo(GardenId other) => Value.CompareTo(other.Value);

        public override string ToString() => $"#{Value:D5}";
    }

    public class Garden
    {
        [Key] public GardenId Id { get; }

        public long RingBalance { get; set; }
        public Instant CreatedOn { get; set; }
        public Instant NextCollectOn { get; set; }
        public long? ActiveChao { get; set; }
        public int InstanceLimit { get; set; }
    }
}