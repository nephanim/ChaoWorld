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
        public PrivacyLevel DescriptionPrivacy { get; }
        public PrivacyLevel MemberListPrivacy { get; }
        public PrivacyLevel FrontPrivacy { get; }
        public PrivacyLevel FrontHistoryPrivacy { get; }
        public PrivacyLevel GroupListPrivacy { get; }
        public int? MemberLimitOverride { get; }
        public int? GroupLimitOverride { get; }

        [JsonIgnore] public DateTimeZone Zone => DateTimeZoneProviders.Tzdb.GetZoneOrNull(UiTz);
    }

    public static class PKSystemExt
    {
        public static string DescriptionFor(this Garden system, LookupContext ctx) =>
            system.DescriptionPrivacy.Get(ctx, system.Description);

        public static JObject ToJson(this Garden system, LookupContext ctx)
        {
            var o = new JObject();
            o.Add("id", system.Hid);
            o.Add("name", system.Name);
            o.Add("description", system.DescriptionFor(ctx));
            o.Add("tag", system.Tag);
            o.Add("avatar_url", system.AvatarUrl.TryGetCleanCdnUrl());
            o.Add("banner", system.DescriptionPrivacy.Get(ctx, system.BannerImage).TryGetCleanCdnUrl());
            o.Add("color", system.Color);
            o.Add("created", system.Created.FormatExport());
            // todo: change this to "timezone"
            o.Add("tz", system.UiTz);
            // todo: just don't include these if not ByOwner
            o.Add("description_privacy", ctx == LookupContext.ByOwner ? system.DescriptionPrivacy.ToJsonString() : null);
            o.Add("member_list_privacy", ctx == LookupContext.ByOwner ? system.MemberListPrivacy.ToJsonString() : null);
            o.Add("front_privacy", ctx == LookupContext.ByOwner ? system.FrontPrivacy.ToJsonString() : null);
            o.Add("front_history_privacy", ctx == LookupContext.ByOwner ? system.FrontHistoryPrivacy.ToJsonString() : null);
            return o;
        }
    }
}