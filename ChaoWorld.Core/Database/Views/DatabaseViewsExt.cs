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

        public static Task<IEnumerable<ListedChao>> QueryChaoRankedList(this IChaoWorldConnection conn)
        {
            StringBuilder query;
            query = new StringBuilder("select * from chao order by swimvalue + flyvalue + runvalue + powervalue + staminavalue + intelligencevalue + luckvalue desc");

            return conn.QueryAsync<ListedChao>(query.ToString());
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
                select r.name, r.difficulty, c.name as winnername, i.*
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

        public static Task<IEnumerable<ListedRaceRecord>> QueryRecordsByRace(this IChaoWorldConnection conn, int raceId)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select ri.winnerchaoid chaoid, c.name chaoname, min(ri.timeelapsedseconds) totaltimeseconds
                from raceinstances ri
                join chao c
                on ri.winnerchaoid = c.id
                where ri.raceid = {raceId}
                group by ri.winnerchaoid, c.name
                order by min(timeelapsedseconds)");

            return conn.QueryAsync<ListedRaceRecord>(query.ToString());
        }

        public static Task<IEnumerable<ListedTournament>> QueryTournamentList(this IChaoWorldConnection conn, bool includeCompletedRaces, bool includeIncompleteRaces, string search)
        {
            var includeStates = new List<int>();
            if (includeCompletedRaces)
            {
                includeStates.Add((int)TournamentInstance.TournamentStates.Completed);
                includeStates.Add((int)TournamentInstance.TournamentStates.Canceled);
            }
            if (includeIncompleteRaces)
            {
                includeStates.Add((int)TournamentInstance.TournamentStates.New);
                includeStates.Add((int)TournamentInstance.TournamentStates.Preparing);
                includeStates.Add((int)TournamentInstance.TournamentStates.InProgress);
            }

            StringBuilder query;
            query = new StringBuilder(@$"
                select t.name, c.name as winnername, i.*
                from tournamentinstances i
                join tournaments t
                on i.tournamentid = t.id
                left join chao c
                on i.winnerchaoid = c.id
                where i.state in ({string.Join(",", includeStates)})");

            if (!string.IsNullOrEmpty(search))
            {
                static string Filter(string column) => $"lower({column}) like concat('%', lower(@filter), '%')";
                query.Append($" and ({Filter("t.name")})");
            }
            query.Append(" order by i.state asc, i.createdon desc");

            return conn.QueryAsync<ListedTournament>(query.ToString(), new { filter = search });
        }

        public static Task<IEnumerable<Item>> QueryItemList(this IChaoWorldConnection conn, long gardenId, Item.ItemCategories[] includeItemCategories, string search)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select *
                from items i
                join itemtypes t
                on i.typeid = t.typeid
                where i.gardenid = {gardenId}");

            if (includeItemCategories.Length > 0)
            {
                var rawItemCategories = new List<int>();
                foreach (var category in includeItemCategories)
                    rawItemCategories.Add((int)category);
                var includeCategories = string.Join(",", rawItemCategories);
                query.Append($" and t.categoryid in ({includeCategories}) ");
            }
            if (!string.IsNullOrEmpty(search))
            {
                static string Filter(string column) => $"lower({column}) like concat('%', lower(@filter), '%')";
                query.Append($" and ({Filter("t.name")})");
            }
            query.Append(" order by t.name asc ");

            return conn.QueryAsync<Item>(query.ToString(), new { filter = search });
        }

        public static Task<IEnumerable<MarketItem>> QueryMarketList(this IChaoWorldConnection conn)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select i.quantity, t.*
                from marketitems i
                join itemtypes t
                on i.typeid = t.typeid
                order by t.categoryid asc, t.marketprice asc");

            return conn.QueryAsync<MarketItem>(query.ToString());
        }

        public static Task<IEnumerable<Tree>> QueryTreeList(this IChaoWorldConnection conn, long gardenId, string search)
        {
            StringBuilder query;
            query = new StringBuilder(@$"
                select *
                from trees
                where gardenid = {gardenId}");

            if (!string.IsNullOrEmpty(search))
            {
                static string Filter(string column) => $"lower({column}) like concat('%', lower(@filter), '%')";
                query.Append($" and ({Filter("name")})");
            }
            query.Append(" order by name asc ");

            return conn.QueryAsync<Tree>(query.ToString(), new { filter = search });
        }
    }
}