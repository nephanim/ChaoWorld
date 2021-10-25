#nullable enable

using NodaTime;

namespace ChaoWorld.Core
{
    /// <summary>
    /// Model for the `message_context` PL/pgSQL function in `functions.sql`
    /// </summary>
    public class MessageContext
    {
        public GardenId? SystemId { get; }
        public ulong? LogChannel { get; }
        public bool InBlacklist { get; }
        public bool InLogBlacklist { get; }
        public bool LogCleanupEnabled { get; }
        public bool ProxyEnabled { get; }
        public AutoproxyMode AutoproxyMode { get; }
        public ChaoId? AutoproxyMember { get; }
        public ulong? LastMessage { get; }
        public ChaoId? LastMessageMember { get; }
        public ChaoId[] LastSwitchChao { get; } = new ChaoId[0];
        public Instant? LastSwitchTimestamp { get; }
        public string? SystemTag { get; }
        public string? SystemGuildTag { get; }
        public bool TagEnabled { get; }
        public string? SystemAvatar { get; }
        public bool AllowAutoproxy { get; }
        public int? LatchTimeout { get; }
    }
}