#nullable enable
using NodaTime;

namespace ChaoWorld.Core
{
    // TODO: is inheritance here correct?
    public class ListedChao: Chao
    {
        public ulong? LastMessage { get; }
        public Instant? LastSwitchTime { get; }

        public AnnualDate? AnnualBirthday =>
            Birthday != null
                ? new AnnualDate(Birthday.Value.Month, Birthday.Value.Day)
                : (AnnualDate?)null;
    }
}