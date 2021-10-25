#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Dapper;

namespace ChaoWorld.Core
{
    public static class DatabaseViewsExt
    {
        public static Task<IEnumerable<ListedChao>> QueryChaoList(this IPKConnection conn, GardenId system, ChaoListQueryOptions opts)
        {
            StringBuilder query;
            query = new StringBuilder("select * from chao_list where system = @system");

            if (opts.Search != null)
            {
                static string Filter(string column) => $"(position(lower(@filter) in lower(coalesce({column}, ''))) > 0)";

                query.Append($" and ({Filter("name")} or {Filter("display_name")}");
                if (opts.SearchDescription)
                {
                    // We need to account for the possibility of description privacy when searching
                    // If we're looking up from the outside, only search "public_description" (defined in the view; null if desc is private)
                    // If we're the owner, just search the full description
                    query.Append($"or {Filter("description")}");
                }
                query.Append(")");
            }

            return conn.QueryAsync<ListedChao>(query.ToString(), new { system, filter = opts.Search });
        }

        public struct ChaoListQueryOptions
        {
            public string? Search;
            public bool SearchDescription;
        }
    }
}