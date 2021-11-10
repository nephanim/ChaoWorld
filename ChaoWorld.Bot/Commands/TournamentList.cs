using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class TournamentList
    {
        private readonly IDatabase _db;

        public TournamentList(IDatabase db)
        {
            _db = db;
        }

        public async Task TournamentInstanceList(Context ctx)
        {
            // Check what we should include
            var includeCompletedTournaments = false;
            var includeIncompleteTournaments = true;
            var search = "";
            var title = "Incomplete Tournaments";
            if (ctx.HasNext())
            {
                var includeTypes = ctx.PopArgument();
                search = ctx.RemainderOrNull();
                
                switch (includeTypes)
                {
                    case "all":
                    default:
                        includeCompletedTournaments = true;
                        includeIncompleteTournaments = true;
                        title = "All Tournaments";
                        break;
                    case "complete":
                    case "completed":
                    case "finished":
                    case "past":
                        includeCompletedTournaments = true;
                        includeIncompleteTournaments = false;
                        title = "Completed Tournaments";
                        break;
                    case "incomplete":
                    case "new":
                    case "active":
                        includeCompletedTournaments = false;
                        includeIncompleteTournaments = true;
                        title = "Incomplete Tournaments";
                        break;
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Replace("\"", "");
                    title = $"Filtered Tournaments: {search}";
                }
            }

            await ctx.RenderTournamentList(_db, includeCompletedTournaments, includeIncompleteTournaments, title, search);
        }
    }
}