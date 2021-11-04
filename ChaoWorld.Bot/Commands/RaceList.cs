using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class RaceList
    {
        private readonly IDatabase _db;

        public RaceList(IDatabase db)
        {
            _db = db;
        }

        public async Task RaceInstanceList(Context ctx)
        {
            // Check what we should include
            var includeCompletedRaces = false;
            var includeIncompleteRaces = true;
            var search = "";
            var title = "Incomplete Races";
            if (ctx.HasNext())
            {
                var includeTypes = ctx.PopArgument();
                search = ctx.RemainderOrNull();
                
                switch (includeTypes)
                {
                    case "all":
                    default:
                        includeCompletedRaces = true;
                        includeIncompleteRaces = true;
                        title = "All Races";
                        break;
                    case "complete":
                    case "completed":
                    case "finished":
                    case "past":
                        includeCompletedRaces = true;
                        includeIncompleteRaces = false;
                        title = "Completed Races";
                        break;
                    case "incomplete":
                    case "new":
                    case "active":
                        includeCompletedRaces = false;
                        includeIncompleteRaces = true;
                        title = "Incomplete Races";
                        break;
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Replace("\"", "");
                    title = $"Filtered Races: {search}";
                }
            }

            await ctx.RenderRaceList(_db, includeCompletedRaces, includeIncompleteRaces, title, search);
        }
    }

    public class RaceProgressListItem
    {
        public long ChaoId { get; set; }
        public string ChaoName { get; set; }
        public RaceInstanceChaoSegment.SegmentStates Status { get; set; }
        public int Position { get; set; }
        public int TotalTimeSeconds { get; set; }
    }
}