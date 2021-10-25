using System.Collections.Generic;
using System.Linq;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class ProxyMatcher
    {
        private static readonly char AutoproxyEscapeCharacter = '\\';
        public static readonly Duration DefaultLatchExpiryTime = Duration.FromHours(6);

        private readonly IClock _clock;
        private readonly ProxyTagParser _parser;

        public ProxyMatcher(ProxyTagParser parser, IClock clock)
        {
            _parser = parser;
            _clock = clock;
        }

        public bool TryMatch(MessageContext ctx, IReadOnlyCollection<ProxyMember> members, out ProxyMatch match, string messageContent,
                             bool hasAttachments, bool allowAutoproxy)
        {
            if (TryMatchTags(members, messageContent, hasAttachments, out match)) return true;
            return false;
        }

        private bool TryMatchTags(IReadOnlyCollection<ProxyMember> members, string messageContent, bool hasAttachments, out ProxyMatch match)
        {
            if (!_parser.TryMatch(members, messageContent, out match)) return false;

            // Edge case: If we got a match with blank inner text, we'd normally just send w/ attachments
            // However, if there are no attachments, the user probably intended something else, so we "un-match" and proceed to autoproxy
            return hasAttachments || match.Content.Trim().Length > 0;
        }

        private bool IsLatchExpired(MessageContext ctx)
        {
            if (ctx.LastMessage == null) return true;
            if (ctx.LatchTimeout == 0) return false;

            var timeout = ctx.LatchTimeout.HasValue
                ? Duration.FromSeconds(ctx.LatchTimeout.Value)
                : DefaultLatchExpiryTime;

            var timestamp = DiscordUtils.SnowflakeToInstant(ctx.LastMessage.Value);
            return _clock.GetCurrentInstant() - timestamp > timeout;
        }
    }
}