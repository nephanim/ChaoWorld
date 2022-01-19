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
        public int Difficulty { get; set; }
    }

    public class ListedRaceRecord
    {
        public long ChaoId { get; set; }
        public string ChaoName { get; set; }
        public int TotalTimeSeconds { get; set; }
    }

    public class ListedTournament: TournamentInstance
    {
        public string Name { get; set; }
        public string WinnerName { get; set; }
    }
}