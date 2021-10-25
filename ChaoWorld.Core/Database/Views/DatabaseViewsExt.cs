#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Dapper;

namespace ChaoWorld.Core
{
    public static class DatabaseViewsExt
    {
        public static Task<IEnumerable<ListedChao>> QueryChaoList(this IPKConnection conn, GardenId garden, ChaoListQueryOptions opts)
        {
            StringBuilder query;
            query = new StringBuilder("select * from chao where gardenid = @garden");

            if (opts.Search != null)
            {
                static string Filter(string column) => $"(position(lower(@filter) in lower(coalesce({column}, ''))) > 0)";

                query.Append($" and ({Filter("name")})");
            }

            return conn.QueryAsync<ListedChao>(query.ToString(), new { garden, filter = opts.Search });
        }

        public struct ChaoListQueryOptions
        {
            public string? Search;
        }
    }
}