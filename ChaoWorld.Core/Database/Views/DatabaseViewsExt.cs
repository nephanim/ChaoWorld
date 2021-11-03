#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Dapper;

namespace ChaoWorld.Core
{
    public static class DatabaseViewsExt
    {
        public static Task<IEnumerable<ListedChao>> QueryChaoList(this IChaoWorldConnection conn, GardenId garden, ChaoListQueryOptions opts)
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

        public static Task<IEnumerable<ListedRace>> QueryRaceList(this IChaoWorldConnection conn, bool includeCompletedRaces, bool includeIncompleteRaces, string search)
        {
            var includeStates = new List<int>();
            if (includeCompletedRaces)
            {
                includeStates.Add((int)RaceInstance.RaceStates.Completed);
                includeStates.Add((int)RaceInstance.RaceStates.Canceled);
            }
            if (includeIncompleteRaces)
            {
                includeStates.Add((int)RaceInstance.RaceStates.New);
                includeStates.Add((int)RaceInstance.RaceStates.Preparing);
                includeStates.Add((int)RaceInstance.RaceStates.InProgress);
            }

            StringBuilder query;
            query = new StringBuilder(@$"
                select r.name, c.name as winnername, i.*
                from raceinstances i
                join races r
                on i.raceid = r.id
                left join chao c
                on i.winnerchaoid = c.id
                where i.state in ({string.Join(",", includeStates)})");

            if (!string.IsNullOrEmpty(search))
            {
                static string Filter(string column) => $"lower({column}) like concat('%', lower(@filter), '%')";
                query.Append($" and ({Filter("r.name")})");
            }
            query.Append(" order by i.state asc, i.createdon desc");

            return conn.QueryAsync<ListedRace>(query.ToString(), new { filter = search });
        }

        public static Task<IEnumerable<Item>> QueryItemList(this IChaoWorldConnection conn, long gardenId, Item.ItemCategories[] includeItemCategories, Item.ItemTypes[] includeItemTypes)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select *
                from items
                where items.gardenid = {gardenId}");

            if (includeItemCategories.Length > 0)
            {
                var rawItemCategories = new List<int>();
                foreach (var category in includeItemCategories)
                    rawItemCategories.Add((int)category);
                var includeCategories = string.Join(",", rawItemCategories);
                query.Append($" and items.categoryid in ({includeCategories}) ");
            }
            if (includeItemTypes.Length > 0)
            {
                var rawItemTypes = new List<int>();
                foreach (var type in includeItemTypes)
                    rawItemTypes.Add((int)type);
                var includeTypes = string.Join(",", rawItemTypes);
                query.Append($" and items.typeid in ({includeTypes}) ");
            }
            query.Append(" order by items.categoryid asc, items.typeid asc ");

            return conn.QueryAsync<Item>(query.ToString());
        }

        public static Task<IEnumerable<MarketItem>> QueryMarketList(this IChaoWorldConnection conn)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select *
                from marketitems
                order by categoryid asc, price asc");

            return conn.QueryAsync<MarketItem>(query.ToString());
        }
    }
}