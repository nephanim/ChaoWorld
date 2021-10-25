#nullable enable
namespace ChaoWorld.Core
{
    public class MemberGuildSettings
    {
        public ChaoId Member { get; }
        public ulong Guild { get; }
        public string? DisplayName { get; }
        public string? AvatarUrl { get; }
    }
}