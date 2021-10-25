#nullable enable
namespace ChaoWorld.Core
{
    public class ChaoGuildSettings
    {
        public ChaoId Chao { get; }
        public ulong Guild { get; }
        public string? DisplayName { get; }
        public string? AvatarUrl { get; }
    }
}