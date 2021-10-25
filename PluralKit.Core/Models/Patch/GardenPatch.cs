#nullable enable
using System.Linq;
using System.Text.RegularExpressions;

using NodaTime;

using Newtonsoft.Json.Linq;

using SqlKata;

namespace ChaoWorld.Core
{
    public class GardenPatch: PatchObject
    {
        public Partial<string> Name { get; set; }
        public Partial<string?> DisplayName { get; set; }
        public Partial<string?> BannerImage { get; set; }
        public Partial<string?> Color { get; set; }
        public Partial<LocalDate?> Birthday { get; set; }
        public Partial<string?> Pronouns { get; set; }
        public Partial<string?> Description { get; set; }

        public override Query Apply(Query q) => q.ApplyPatch(wrapper => wrapper
            .With("name", Name)
            .With("display_name", DisplayName)
            .With("banner_image", BannerImage)
            .With("color", Color)
            .With("birthday", Birthday)
            .With("pronouns", Pronouns)
            .With("description", Description)
        );

        public new void AssertIsValid()
        {
            if (Name.IsPresent)
                AssertValid(Name.Value, "name", Limits.MaxChaoNameLength);
            if (DisplayName.Value != null)
                AssertValid(DisplayName.Value, "display_name", Limits.MaxChaoNameLength);
            if (BannerImage.Value != null)
                AssertValid(BannerImage.Value, "banner", Limits.MaxUriLength,
                    s => MiscUtils.TryMatchUri(s, out var bannerUri));
            if (Color.Value != null)
                AssertValid(Color.Value, "color", "^[0-9a-fA-F]{6}$");
            if (Pronouns.Value != null)
                AssertValid(Pronouns.Value, "pronouns", Limits.MaxPronounsLength);
            if (Description.Value != null)
                AssertValid(Description.Value, "description", Limits.MaxDescriptionLength);
        }

#nullable disable

        public static GardenPatch FromJSON(JObject o)
        {
            var patch = new GardenPatch();

            if (o.ContainsKey("name") && o["name"].Type == JTokenType.Null)
                throw new ValidationError("Chao name can not be set to null.");

            if (o.ContainsKey("name")) patch.Name = o.Value<string>("name");
            if (o.ContainsKey("color")) patch.Color = o.Value<string>("color").NullIfEmpty()?.ToLower();
            if (o.ContainsKey("display_name")) patch.DisplayName = o.Value<string>("display_name").NullIfEmpty();
            if (o.ContainsKey("banner")) patch.BannerImage = o.Value<string>("banner").NullIfEmpty();

            if (o.ContainsKey("birthday"))
            {
                var str = o.Value<string>("birthday").NullIfEmpty();
                var res = DateTimeFormats.DateExportFormat.Parse(str);
                if (res.Success) patch.Birthday = res.Value;
                else if (str == null) patch.Birthday = null;
                else throw new ValidationError("birthday");
            }

            if (o.ContainsKey("pronouns")) patch.Pronouns = o.Value<string>("pronouns").NullIfEmpty();
            if (o.ContainsKey("description")) patch.Description = o.Value<string>("description").NullIfEmpty();

            return patch;
        }
    }
}