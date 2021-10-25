#nullable enable
using System.Collections.Generic;

namespace ChaoWorld.Core
{
    /// <summary>
    /// Model for the `proxy_chao` PL/pgSQL function in `functions.sql`
    /// </summary>
    public class ProxyMember
    {
        public ChaoId Id { get; }
        public IReadOnlyCollection<ProxyTag> ProxyTags { get; } = new ProxyTag[0];
        public bool KeepProxy { get; }

        public string? ServerName { get; }
        public string? DisplayName { get; }
        public string Name { get; } = "";

        public string? ServerAvatar { get; }
        public string? Avatar { get; }


        public bool AllowAutoproxy { get; }
        public string? Color { get; }

        public string ProxyName(MessageContext ctx)
        {
            var chaoName = ServerName ?? DisplayName ?? Name;
            if (!ctx.TagEnabled)
                return chaoName;

            if (ctx.SystemGuildTag != null)
                return $"{chaoName} {ctx.SystemGuildTag}";
            else if (ctx.SystemTag != null)
                return $"{chaoName} {ctx.SystemTag}";
            else return chaoName;
        }
        public string? ProxyAvatar(MessageContext ctx) => ServerAvatar ?? Avatar ?? ctx.SystemAvatar;

        public ProxyMember() { }

        public ProxyMember(string name, params ProxyTag[] tags)
        {
            Name = name;
            ProxyTags = tags;
        }
    }
}