using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NodaTime;
using NodaTime.Text;

namespace ChaoWorld.Core
{
    public readonly struct ChaoId: INumericId<ChaoId, int>
    {
        public int Value { get; }

        public ChaoId(int value)
        {
            Value = value;
        }

        public bool Equals(ChaoId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ChaoId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(ChaoId left, ChaoId right) => left.Equals(right);

        public static bool operator !=(ChaoId left, ChaoId right) => !left.Equals(right);

        public int CompareTo(ChaoId other) => Value.CompareTo(other.Value);

        public override string ToString() => $"Member #{Value}";
    }

    public class Chao
    {
        // Dapper *can* figure out mapping to getter-only properties, but this doesn't work
        // when trying to map to *subclasses* (eg. ListedMember). Adding private setters makes it work anyway.
        public ChaoId Id { get; private set; }
        public string Hid { get; private set; }
        public Guid Uuid { get; private set; }
        public GardenId Garden { get; private set; }
        public string Color { get; private set; }
        public string AvatarUrl { get; private set; }
        public string BannerImage { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public LocalDate? Birthday { get; private set; }
        public string Pronouns { get; private set; }
        public string Description { get; private set; }
        public ICollection<ProxyTag> ProxyTags { get; private set; }
        public bool KeepProxy { get; private set; }
        public Instant Created { get; private set; }
        public int MessageCount { get; private set; }
        public bool AllowAutoproxy { get; private set; }

        /// Returns a formatted string representing the member's birthday, taking into account that a year of "0001" or "0004" is hidden
        /// Before Feb 10 2020, the sentinel year was 0001, now it is 0004.
        [JsonIgnore]
        public string BirthdayString
        {
            get
            {
                if (Birthday == null) return null;

                var format = LocalDatePattern.CreateWithInvariantCulture("MMM dd, yyyy");
                if (Birthday?.Year == 1 || Birthday?.Year == 4) format = LocalDatePattern.CreateWithInvariantCulture("MMM dd");
                return format.Format(Birthday.Value);
            }
        }

        [JsonIgnore] public bool HasProxyTags => ProxyTags.Count > 0;
    }
}