#nullable enable
using NodaTime;

namespace ChaoWorld.Core
{
    // TODO: is inheritance here correct?
    public class ListedChao: Chao
    {

    }

    public class ListedRace : RaceInstance
    {
        public string Name { get; set; }
        public string WinnerName { get; set; }
    }
}