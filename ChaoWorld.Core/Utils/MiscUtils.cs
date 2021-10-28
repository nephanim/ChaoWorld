using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ChaoWorld.Core
{
    public static class MiscUtils
    {
        public static Chao.StatGrades GenerateStatGrade()
        {
            var random = new Random();
            var roll = random.Next(1, 100);
            if (roll > 95)
                return Chao.StatGrades.S;
            if (roll > 85)
                return Chao.StatGrades.A;
            if (roll > 65)
                return Chao.StatGrades.B;
            if (roll > 30)
                return Chao.StatGrades.C;
            if (roll > 10)
                return Chao.StatGrades.D;
            return Chao.StatGrades.E;
        }

        public static bool TryMatchUri(string input, out Uri uri)
        {
            try
            {
                uri = new Uri(input);
                if (!uri.IsAbsoluteUri || (uri.Scheme != "http" && uri.Scheme != "https"))
                    return false;
            }
            catch (UriFormatException)
            {
                uri = null;
                return false;
            }

            return true;
        }

        // discord mediaproxy URLs used to be stored directly in the database, so now we cleanup image urls before using them outside of proxying
        private static readonly Regex MediaProxyUrl = new Regex(@"^https?://media.discordapp.net/attachments/(\d{17,19})/(\d{17,19})/([^/\\&\?]+)\.(png|jpg|jpeg|webp)(\?.*)?$");
        private static readonly string DiscordCdnReplacement = "https://cdn.discordapp.com/attachments/$1/$2/$3.$4";
        public static string? TryGetCleanCdnUrl(this string? url)
        {
            return url == null ? null : MediaProxyUrl.Replace(url, DiscordCdnReplacement);
        }

        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}