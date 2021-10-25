using Myriad.Types;

namespace Myriad.Gateway
{
    public record GuildCreateEvent: Guild, IGatewayEvent
    {
        public Channel[] Channels { get; init; }
        public GuildMember[] Chao { get; init; }
        public Channel[] Threads { get; init; }
    }
}