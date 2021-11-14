#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;
using System.Collections.Generic;
using NodaTime;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public async Task<Tournament?> GetTournamentById(int id)
        {
            var query = new Query("tournaments").Where("id", id);
            return await _db.QueryFirst<Tournament?>(query);
        }

        public async Task<Tournament?> GetTournamentByInstanceId(long id)
        {
            var query = new Query("tournaments").Join("tournamentinstances", "tournaments.id", "tournamentinstances.tournamentid")
                .Where("tournamentinstances.id", "=", id).Select("tournaments.*");
            return await _db.QueryFirst<Tournament?>(query);
        }

        public async Task<Tournament> ResetTournamentAvailableOn(Tournament tourney, IChaoWorldConnection? conn = null)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var availableDuration = Duration.FromMinutes(tourney.FrequencyMinutes);
            var availableOn = now.Plus(availableDuration);
            var query = new Query("tournaments").Where("id", tourney.Id).AsUpdate(new
            {
                availableon = availableOn
            });
            var updatedTourney = await _db.QueryFirst<Tournament>(query, extraSql: "returning *");
            _logger.Information($"Updated instance {tourney.Id} of tournament {tourney.Name}");
            return updatedTourney;
        }

        public async Task<TournamentInstance?> GetTournamentInstanceById(long id)
        {
            var query = new Query("tournamentinstances").Where("id", id);
            return await _db.QueryFirst<TournamentInstance?>(query);
        }

        public async Task<TournamentInstance?> GetTournamentInstanceByRaceId(int id)
        {
            var query = new Query("tournamentinstances").Where("tournamentid", id);
            return await _db.QueryFirst<TournamentInstance?>(query);
        }

        public async Task<TournamentInstance?> GetTournamentInstanceByName(string name)
        {
            var query = new Query("tournamentinstances").Join("tournaments", "tournaments.id", "tournamentinstances.tournamentid", "=").WhereRaw(
                "lower(tournaments.name) = lower(?)",
                name.ToLower().Replace("\"", string.Empty))
            .Select("tournamentinstances.*")
            .OrderByDesc("tournamentinstances.id")
            .Limit(1);
            return await _db.QueryFirst<TournamentInstance?>(query);
        }

        public async Task<TournamentInstance?> GetTournamentInstanceByNameWithFuzzyMatching(string name)
        {
            var query = new Query("tournamentinstances").Join("tournaments", "tournaments.id", "tournamentinstances.tournamentid", "=")
                .Where("tournamentinstances.state", "!=", (int)TournamentInstance.TournamentStates.Completed)
                .Where("tournamentinstances.state", "!=", (int)TournamentInstance.TournamentStates.Canceled)
                .Select("tournamentinstances.*")
                .OrderByRaw("similarity(tournaments.name, lower(?)) desc", name.ToLower().Replace("\"", string.Empty))
                .Limit(1);
            return await _db.QueryFirst<TournamentInstance?>(query);
        }

        public async Task<int> GetTournamentInstanceChaoCount(long instanceId)
        {
            var query = new Query("tournamentinstancechao").Where("tournamentinstanceid", instanceId).SelectRaw("count(chaoid)");
            return await _db.QueryFirst<int>(query);
        }

        public Task<IEnumerable<Chao>> GetChaoInTournament(TournamentInstance instance)
        {
            var query = new Query("tournamentinstancechao").Join("chao", "tournamentinstancechao.chaoid", "chao.id")
                .Where("tournamentinstancechao.tournamentinstanceid", instance.Id)
                .Select("chao.*");
            return _db.Query<Chao>(query);
        }

        public Task<IEnumerable<TournamentInstanceChao>> GetTournamentInstanceChao(TournamentInstance instance)
        {
            var query = new Query("tournamentinstancechao").Join("chao", "tournamentinstancechao.chaoid", "chao.id")
                .Where("tournamentinstancechao.tournamentinstanceid", instance.Id)
                .Select("tournamentinstancechao.*")
                .OrderByRandom(instance.Id.ToString());
            return _db.Query<TournamentInstanceChao>(query);
        }

        public async Task<TournamentInstance?> GetActiveTournamentByGarden(int gardenId)
        {
            var query = new Query("tournamentinstances")
                .Join("tournamentinstancechao", "tournamentinstances.id", "tournamentinstancechao.tournamentinstanceid")
                .Join("chao", "tournamentinstancechao.chaoid", "chao.id")
                .Where("chao.gardenid", "=", gardenId)
                .Where("tournamentinstances.state", "!=", (int)TournamentInstance.TournamentStates.Completed)
                .Where("tournamentinstances.state", "!=", (int)TournamentInstance.TournamentStates.Canceled)
                .Select("tournamentinstances.*");
            return await _db.QueryFirst<TournamentInstance?>(query);
        }

        public async Task<TournamentInstance> CreateTournamentInstance(Tournament tourney, IChaoWorldConnection? conn = null)
        {
            var query = new Query("tournamentinstances").AsInsert(new
            {
                tournamentid = tourney.Id,
                state = TournamentInstance.TournamentStates.New
            });
            var instance = await _db.QueryFirst<TournamentInstance>(conn, query, "returning *");
            _logger.Information($"Created instance {instance.Id} of tournament {tourney.Id} ({tourney.Name})");
            return instance;
        }

        public async Task<TournamentInstance> UpdateTournamentInstance(TournamentInstance instance, IChaoWorldConnection? conn = null)
        {
            var query = new Query("tournamentinstances").Where("id", instance.Id).AsUpdate(new
            {
                state = instance.State,
                readyon = instance.ReadyOn,
                completedon = instance.CompletedOn,
                totaltimeelapsedseconds = instance.TotalTimeElapsedSeconds,
                winnerchaoid = instance.WinnerChaoId
            });
            var updatedInstance = await _db.QueryFirst<TournamentInstance>(query, extraSql: "returning *");
            _logger.Information($"Updated instance {instance.Id} of tournament {instance.TournamentId}");
            return updatedInstance;
        }

        public async Task FinalizeTournamentInstanceChao(TournamentInstance instance)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update tournamentinstancechao
                set
	                iswinner = true,
                    highestround = {instance.Rounds},
                    state = {(int)TournamentInstance.TournamentStates.Completed}
                where chaoid = {instance.WinnerChaoId}
                and tournamentinstanceid = {instance.Id}
            "));
            _logger.Information($"Finalized chao statistics for tournament instance {instance.Id} of tournament {instance.TournamentId}");
        }

        public async Task RecalculateTournamentRewards()
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update tournaments t
                set prizerings = (
	                select floor(avg(totaltimeelapsedseconds + t.readydelayminutes*60.0)/1.5)
	                from tournamentinstances i
	                join chao c
	                on i.winnerchaoid = c.id
	                where i.tournamentid = t.id
	                and c.gardenid != 0
                )
            "));
            _logger.Information($"Updated prize amounts for tournaments");
        }

        public async Task GiveTournamentRewards(TournamentInstance instance, int prizeRings)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update gardens g
                set ringbalance = ringbalance + {prizeRings}
                from tournamentinstances i
                join chao c
                on i.winnerchaoid = c.id
                where i.id = {instance.Id}
                and g.id = c.gardenid
            "));
            _logger.Information($"Delivered prize of {prizeRings} rings for instance {instance.Id} of tournament {instance.TournamentId}");
        }

        public async Task JoinChaoToTournamentInstance(TournamentInstance instance, Chao chao, IChaoWorldConnection? conn = null)
        {
            var query = new Query("tournamentinstancechao").AsInsert(new
            {
                tournamentinstanceid = instance.Id,
                chaoid = chao.Id,                                             
                state = TournamentInstance.TournamentStates.New
            });
            await _db.QueryFirst<TournamentInstanceChao>(conn, query, "returning *");
            _logger.Information($"Chao {chao.Id} joined instance {instance.Id} of tournament {instance.TournamentId}");
        }

        public async Task RemoveChaoFromTournamentInstance(TournamentInstance instance, Chao chao)
        {
            var query = new Query("tournamentinstancechao")
                .Where("tournamentinstanceid", instance.Id)
                .Where("chaoid", chao.Id)
                .AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Chao {chao.Id} left instance {instance.Id} of tournament {instance.TournamentId}");
        }

        public async Task<TournamentInstanceMatch> AddMatch(TournamentInstance instance, long leftChaoId, long rightChaoId, int round, int order, IChaoWorldConnection? conn = null)
        {
            var query = new Query("tournamentinstancematches").AsInsert(new
            {
                tournamentinstanceid = instance.Id,
                state = TournamentInstance.TournamentStates.New,
                roundnumber = round,
                roundorder = order,
                leftchaoid = leftChaoId,
                rightchaoid = rightChaoId
            });
            var match = await _db.QueryFirst<TournamentInstanceMatch>(conn, query, "returning *");
            _logger.Information($"Added match {order} for round {round} between chao {leftChaoId} / {rightChaoId} to instance {instance.Id} of tournament {instance.TournamentId}");
            return match;
        }

        public async Task<TournamentInstanceMatch> UpdateMatch(TournamentInstanceMatch match)
        {
            var query = new Query("tournamentinstancematches")
                .Where("tournamentinstanceid", match.TournamentInstanceId)
                .Where("roundnumber", match.RoundNumber)
                .Where("roundorder", match.RoundOrder)
                .AsUpdate(new
                    {
                        state = (int)match.State,
                        resulttype = (int)match.ResultType.GetValueOrDefault(TournamentInstanceMatch.TournamentResultTypes.TimedOut),
                        elapsedtimeseconds = match.ElapsedTimeSeconds,
                        winnerchaoid = match.WinnerChaoId
                    });
            var updatedMatch = await _db.QueryFirst<TournamentInstanceMatch>(query, extraSql: "returning *");
            _logger.Information($"Updated match {match.RoundOrder} for round {match.RoundNumber} in instance {match.TournamentInstanceId}");
            return updatedMatch;
        }

        public async Task FinalizeTournamentInstanceChaoForMatch(long instanceId, long loserChaoId, int round)
        {
            var instances = await _db.Execute(async conn => await conn.QueryAsync<int>($@"
                    update tournamentinstancechao
                    set state = {(int)TournamentInstance.TournamentStates.Canceled},
                        iswinner = false,
                        highestround = {round}
                    where tournamentinstanceid = {instanceId}
                    and chaoid = {loserChaoId}
                "));
            var instanceCount = instances.AsList().Count;
            _logger.Information($"Retired chao {loserChaoId} from tournament instance {instanceId}");
        }

        public async Task<IEnumerable<Tournament>> GetAvailableTournaments()
        {
            var tournaments = new List<Tournament>();
            try
            {
                tournaments = (await _db.Execute(async conn => await conn.QueryAsync<Tournament>($@"
                    select *
                    from tournaments t
                    left join tournamentinstances i
                    on t.id = i.tournamentid
                    and i.state not in ({(int)TournamentInstance.TournamentStates.Completed}, {(int)TournamentInstance.TournamentStates.Canceled})
                    where t.isenabled = true
                    and i.id is null
                    and t.availableon < (now() at time zone 'utc')
                "))).AsList();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to read tournaments: {e.Message}");
            }
            return tournaments;
        }

        public async Task UpdateTournamentPingSetting(ulong accountId, bool allowPings)
        {
            await _db.Execute(conn => conn.QueryAsync(@$"
                update accounts
                set enabletournamentpings = {allowPings}
                where uid = {accountId}
            "));
            _logger.Information($"Updated account settings for {accountId} (enable tournament pings: {allowPings}");
        }

        public async Task<IEnumerable<ulong>> GetAccountsToPingForTournament(long instanceId)
        {
            return await _db.Execute(conn => conn.QueryAsync<ulong>(@$"
                    select a.uid
                    from tournamentinstancechao tic
                    join chao c
                    on tic.chaoid = c.id
                    join accounts a
                    on c.gardenid = a.gardenid
                    where tic.tournamentinstanceid = {instanceId}
                    and c.gardenid > 0
                    and enabletournamentpings = true
                "));
        }
    }
}