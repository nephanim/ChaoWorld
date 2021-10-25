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

        public override string ToString() => $"Garden #{Value}";
    }

    public class Garden
    {
        [Key] public GardenId Id { get; }
        public string Hid { get; }
        public Guid Uuid { get; private set; }
        public string Name { get; }
        public string Description { get; }
        public string Tag { get; }
        public string AvatarUrl { get; }
        public string BannerImage { get; }
        public string Color { get; }
        public string Token { get; }
        public Instant Created { get; }
        public string UiTz { get; set; }
        public bool PingsEnabled { get; }
        public int? LatchTimeout { get; }
        public int? ChaoLimitOverride { get; }
        public int? GroupLimitOverride { get; }

        [JsonIgnore] public DateTimeZone Zone => DateTimeZoneProviders.Tzdb.GetZoneOrNull(UiTz);
    }
}