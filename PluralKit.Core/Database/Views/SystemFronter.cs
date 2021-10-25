using NodaTime;

namespace ChaoWorld.Core
{
    public class SystemFronter
    {
        public GardenId SystemId { get; }
        public SwitchId SwitchId { get; }
        public Instant SwitchTimestamp { get; }
        public ChaoId MemberId { get; }
        public string MemberHid { get; }
        public string MemberName { get; }
    }
}