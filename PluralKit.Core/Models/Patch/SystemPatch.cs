#nullable enable
using System;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using NodaTime;

using SqlKata;

namespace ChaoWorld.Core
{
    public class SystemPatch: PatchObject
    {
        public Partial<string?> Name { get; set; }
        public Partial<string?> Hid { get; set; }
        public Partial<string?> Description { get; set; }
        public Partial<string?> Tag { get; set; }
        public Partial<string?> AvatarUrl { get; set; }
        public Partial<string?> BannerImage { get; set; }
        public Partial<string?> Color { get; set; }
        public Partial<string?> Token { get; set; }
        public Partial<string> UiTz { get; set; }
        public Partial<bool> PingsEnabled { get; set; }
        public Partial<int?> LatchTimeout { get; set; }
        public Partial<int?> ChaoLimitOverride { get; set; }
        public Partial<int?> GroupLimitOverride { get; set; }

        public override Query Apply(Query q) => q.ApplyPatch(wrapper => wrapper
            .With("name", Name)
            .With("hid", Hid)
            .With("description", Description)
            .With("tag", Tag)
            .With("avatar_url", AvatarUrl)
            .With("banner_image", BannerImage)
            .With("color", Color)
            .With("token", Token)
            .With("ui_tz", UiTz)
            .With("pings_enabled", PingsEnabled)
            .With("latch_timeout", LatchTimeout)
            .With("chao_limit_override", ChaoLimitOverride)
            .With("group_limit_override", GroupLimitOverride)
        );

        public new void AssertIsValid()
        {
            if (Name.Value != null)
                AssertValid(Name.Value, "name", Limits.MaxSystemNameLength);
            if (Description.Value != null)
                AssertValid(Description.Value, "description", Limits.MaxDescriptionLength);
            if (Tag.Value != null)
                AssertValid(Tag.Value, "tag", Limits.MaxSystemTagLength);
            if (AvatarUrl.Value != null)
                AssertValid(AvatarUrl.Value, "avatar_url", Limits.MaxUriLength,
                    s => MiscUtils.TryMatchUri(s, out var avatarUri));
            if (BannerImage.Value != null)
                AssertValid(BannerImage.Value, "banner", Limits.MaxUriLength,
                    s => MiscUtils.TryMatchUri(s, out var bannerUri));
            if (Color.Value != null)
                AssertValid(Color.Value, "color", "^[0-9a-fA-F]{6}$");
            if (UiTz.IsPresent && DateTimeZoneProviders.Tzdb.GetZoneOrNull(UiTz.Value) == null)
                throw new ValidationError("avatar_url");
        }

        public static SystemPatch FromJSON(JObject o)
        {
            var patch = new SystemPatch();
            if (o.ContainsKey("name")) patch.Name = o.Value<string>("name").NullIfEmpty();
            if (o.ContainsKey("description")) patch.Description = o.Value<string>("description").NullIfEmpty();
            if (o.ContainsKey("tag")) patch.Tag = o.Value<string>("tag").NullIfEmpty();
            if (o.ContainsKey("avatar_url")) patch.AvatarUrl = o.Value<string>("avatar_url").NullIfEmpty();
            if (o.ContainsKey("banner")) patch.BannerImage = o.Value<string>("banner").NullIfEmpty();
            if (o.ContainsKey("color")) patch.Color = o.Value<string>("color").NullIfEmpty();
            if (o.ContainsKey("timezone")) patch.UiTz = o.Value<string>("tz") ?? "UTC";

            // legacy: APIv1 uses "tz" instead of "timezone"
            // todo: remove in APIv2
            if (o.ContainsKey("tz")) patch.UiTz = o.Value<string>("tz") ?? "UTC";

            return patch;
        }
    }
}