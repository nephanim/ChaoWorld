using System.Linq;
using System.Text.RegularExpressions;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public static class ModelUtils
    {
        public static string NameFor(this Chao member, Context ctx) =>
            member.NameFor(ctx.LookupContextFor(member));

        public static string AvatarFor(this Chao member, Context ctx) =>
            member.AvatarFor(ctx.LookupContextFor(member)).TryGetCleanCdnUrl();

        public static string DisplayName(this Chao member) =>
            member.DisplayName ?? member.Name;

        public static string Reference(this Chao member) => EntityReference(member.Hid, member.Name);

        private static string EntityReference(string hid, string name)
        {
            bool IsSimple(string s) =>
                // No spaces, no symbols, allow single quote but not at the start
                Regex.IsMatch(s, "^[\\w\\d\\-_'?]+$") && !s.StartsWith("'");

            // If it's very long (>25 chars), always use hid
            if (name.Length >= 25)
                return hid;

            // If name is "simple" just use that
            if (IsSimple(name))
                return name;

            // If three or fewer "words" and they're all simple individually, quote them
            var words = name.Split(' ');
            if (words.Length <= 3 && words.All(w => w.Length > 0 && IsSimple(w)))
                // Words with double quotes are never "simple" so we're safe to naive-quote here
                return $"\"{name}\"";

            // Otherwise, just use hid
            return hid;
        }
    }
}