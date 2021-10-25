namespace ChaoWorld.Core
{
    public enum AutoproxyMode
    {
        Off = 1,
        Front = 2,
        Latch = 3,
        Chao = 4
    }

    public class SystemGuildSettings
    {
        public ulong Guild { get; }
        public GardenId System { get; }
        public bool ProxyEnabled { get; } = true;

        public AutoproxyMode AutoproxyMode { get; } = AutoproxyMode.Off;
        public ChaoId? AutoproxyChao { get; }

        public string? Tag { get; }
        public bool TagEnabled { get; }
    }
}