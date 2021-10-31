#nullable enable
using System.Linq;
using System.Text.RegularExpressions;

using NodaTime;

using Newtonsoft.Json.Linq;

using SqlKata;

namespace ChaoWorld.Core
{
    public class ChaoPatch: PatchObject
    {
        public Partial<string> Name { get; set; }

        public override Query Apply(Query q) => q.ApplyPatch(wrapper => wrapper
            .With("name", Name)
        );

        public new void AssertIsValid()
        {
            if (Name.IsPresent)
                AssertValid(Name.Value, "name", Limits.MaxChaoNameLength);
        }

#nullable disable

        public static ChaoPatch FromJSON(JObject o)
        {
            var patch = new ChaoPatch();

            if (o.ContainsKey("name") && o["name"].Type == JTokenType.Null)
                throw new ValidationError("Chao name can not be set to null.");

            if (o.ContainsKey("name")) patch.Name = o.Value<string>("name");

            return patch;
        }
    }
}