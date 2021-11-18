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
        public async Task<Race?> GetRaceById(int id)
        {
            var query = new Query("races").Where("id", id);
            return await _db.QueryFirst<Race?>(query);
        }

        public async Task<Race?> GetRaceByInstanceId(long id)
        {
            var query = new Query("races").Join("raceinstances", "races.id", "raceinstances.raceid")
                .Where("raceinstances.id", "=", id).Select("races.*");
            return await _db.QueryFirst<Race?>(query);
        }

        public async Task<Race> ResetRaceAvailableOn(Race race, IChaoWorldConnection? conn = null)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var availableDuration = Duration.FromMinutes(race.FrequencyMinutes);
            var availableOn = now.Plus(availableDuration);
            var query = new Query("races").Where("id", race.Id).AsUpdate(new
            {
                availableon = availableOn
            });
            var updatedRace = await _db.QueryFirst<Race>(query, extraSql: "returning *");
            _logger.Information($"Updated next available time for race {race.Id} ({race.Name})");
            return updatedRace;
        }

        public async Task<RaceInstance?> GetRaceInstanceById(long id)
        {
            var query = new Query("raceinstances").Where("id", id);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance?> GetRaceInstanceByRaceId(int id)
        {
            var query = new Query("raceinstances").Where("raceid", id).OrderByDesc("id");
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance?> GetRaceInstanceByName(string name)
        {
            var query = new Query("raceinstances").Join("races", "races.id", "raceinstances.raceid", "=").WhereRaw(
                "lower(races.name) = lower(?)",
                name.ToLower().Replace("\"", string.Empty))
            .Select("raceinstances.*")
            .OrderByDesc("raceinstances.id")
            .Limit(1);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance?> GetRaceInstanceByNameWithFuzzyMatching(string name)
        {
            var query = new Query("raceinstances").Join("races", "races.id", "raceinstances.raceid", "=")
                //.Where("raceinstances.state", "!=", (int)Core.RaceInstance.RaceStates.Completed)
                //.Where("raceinstances.state", "!=", (int)Core.RaceInstance.RaceStates.Canceled)
                .Select("raceinstances.*")
                .OrderByRaw("similarity(races.name, lower(?)) desc, raceinstances.id desc", name.ToLower().Replace("\"", string.Empty))
                .Limit(1);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<IEnumerable<RaceInstance>> GetExpiredRaceInstances()
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var query = new Query("raceinstances")
                .Where("raceinstances.state", (int)Core.RaceInstance.RaceStates.New)
                .Where("raceinstances.readyon", "<", now);
            return await _db.Query<RaceInstance>(query);
        }

        public async Task<int> GetRaceInstanceChaoCount(long raceInstanceId)
        {
            var query = new Query("raceinstancechao").Where("raceinstanceid", raceInstanceId).SelectRaw("count(chaoid)");
            return await _db.QueryFirst<int>(query);
        }

        public async Task<RaceInstance?> GetActiveRaceByGarden(int gardenId)
        {
            var query = new Query("raceinstances")
                .Join("raceinstancechao", "raceinstances.id", "raceinstancechao.raceinstanceid")
                .Join("chao", "raceinstancechao.chaoid", "chao.id")
                .Where("chao.gardenid", "=", gardenId)
                .Where("raceinstances.state", "!=", (int)RaceInstance.RaceStates.Completed)
                .Where("raceinstances.state", "!=", (int)RaceInstance.RaceStates.Canceled)
                .Select("raceinstances.*");
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance> CreateRaceInstance(Race race, IChaoWorldConnection? conn = null)
        {
            var availableAt = SystemClock.Instance.GetCurrentInstant();
            var readyIn = Duration.FromMinutes(race.ReadyDelayMinutes);
            var readyAt = availableAt.Plus(readyIn);
            var query = new Query("raceinstances").AsInsert(new
            {
                raceid = race.Id,
                state = RaceInstance.RaceStates.New,
                readyon = readyAt
            });
            var raceInstance = await _db.QueryFirst<RaceInstance>(conn, query, "returning *");
            _logger.Information($"Created instance {raceInstance.Id} of race {race.Id} ({race.Name})");
            return raceInstance;
        }

        public async Task DeleteRaceInstance(RaceInstance instance, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstances").Where("id", instance.Id).AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Deleted instance {instance.Id} of race {instance.RaceId}");
        }

        public async Task<RaceInstance> UpdateRaceInstance(RaceInstance raceInstance, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstances").Where("id", raceInstance.Id).AsUpdate(new
            {
                state = raceInstance.State,
                readyon = raceInstance.ReadyOn,
                completedon = raceInstance.CompletedOn,
                winnerchaoid = raceInstance.WinnerChaoId,
                timeelapsedseconds = raceInstance.TimeElapsedSeconds
            });
            var updatedRaceInstance = await _db.QueryFirst<RaceInstance>(query, extraSql: "returning *");
            return updatedRaceInstance;
        }

        public async Task CompleteRaceInstance(RaceInstance raceInstance, IChaoWorldConnection? conn = null)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                with winner as (
                    select chaoid, totaltimeseconds
                    from raceinstancechao
                    where raceinstanceid = {raceInstance.Id}
                    and state != {(int)RaceInstanceChaoSegment.SegmentStates.Retired}
                    order by finishposition asc
                    limit 1
                )
                update raceinstances
                set completedon = current_timestamp,
                    state = (
                        case when chaoid is null then {(int)RaceInstance.RaceStates.Canceled}
                            else {(int)RaceInstance.RaceStates.Completed}
                        end),
                    winnerchaoid = chaoid,
                    timeelapsedseconds = totaltimeseconds    
                from (select 1) as _placeholder
                left join winner
                on true
                where id = {raceInstance.Id};
            "));
            _logger.Information($"Completed instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }
        
        public async Task RecalculateRaceRewards()
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update races r
                set prizerings = coalesce((
	                select floor(avg(timeelapsedseconds + r.readydelayminutes*60.0)/1.5)
	                from raceinstances i
	                join chao c
	                on i.winnerchaoid = c.id
	                where i.raceid = r.id
	                and c.gardenid != 0
                ), 100)
            "));
            _logger.Information($"Updated prize amounts for races");
        }

        public async Task GiveRaceRewards(RaceInstance raceInstance, int prizeRings)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update gardens g
                set ringbalance = ringbalance + {prizeRings}
                from raceinstances i
                join chao c
                on i.winnerchaoid = c.id
                where i.id = {raceInstance.Id}
                and g.id = c.gardenid
            "));
            _logger.Information($"Delivered prize of {prizeRings} rings for instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }

        public async Task JoinChaoToRaceInstance(RaceInstance raceInstance, Chao chao, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstancechao").AsInsert(new
            {
                raceinstanceid = raceInstance.Id,
                chaoid = chao.Id,                                             
                state = RaceInstance.RaceStates.New
            });
            await _db.QueryFirst<RaceInstanceChao>(conn, query, "returning *");
            _logger.Information($"Chao {chao.Id} joined instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }

        public async Task RemoveChaoFromRaceInstance(RaceInstance raceInstance, Chao chao)
        {
            var query = new Query("raceinstancechao")
                .Where("raceinstanceid", raceInstance.Id)
                .Where("chaoid", chao.Id)
                .AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Chao {chao.Id} left instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }

        public async Task<IEnumerable<RaceInstanceBan>> GetRaceInstanceBans(Garden garden)
        {
            var query = new Query("raceinstancebans")
                .Where("gardenid", garden.Id.Value);
            return await _db.Query<RaceInstanceBan>(query);
        }

        public async Task BanFromRaceInstance(RaceInstance instance, Garden garden, IChaoWorldConnection? conn = null)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var expireDuration = Duration.FromMinutes(10);
            var expiresOn = now.Plus(expireDuration);
            var query = new Query("raceinstancebans").AsInsert(new
            {
                raceinstanceid = instance.Id,
                gardenid = garden.Id.Value,
                expireson = expiresOn
            });
            await _db.QueryFirst<RaceInstanceBan>(conn, query, "returning *");
            _logger.Information($"Garden {garden.Id} was banned from race instance {instance.Id} of race {instance.RaceId} for leaving the queue");
        }

        public async Task ClearExpiredRaceInstanceBans()
        {
            await _db.Execute(conn => conn.QueryAsync($@"
                delete from raceinstancebans
                where expireson < current_timestamp
            "));
            _logger.Information($"Cleared expired race instance bans");
        }

        public async Task AddSegmentsToRaceInstance(RaceInstance raceInstance, IChaoWorldConnection? conn = null)
        {
            // This does the following:
            //  * determines the segments for the race
            //  * initializes that segment for each chao in the race
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                insert into raceinstancechaosegments
	                (raceinstanceid, racesegmentid, chaoid)
                select i.id, s.id, ic.chaoid
                from raceinstances i
                join racesegments s
                on i.raceid = s.raceid
                join raceinstancechao ic
                on i.id = ic.raceinstanceid
                join chao c
                on ic.chaoid = c.id
                where i.id = {raceInstance.Id}
            "));
            _logger.Information($"Added segments to instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }

        public async Task<RaceSegment> GetRaceSegment(int raceId, int index)
        {
            var query = new Query("racesegments").Where("raceid", raceId).Where("raceindex", index);
            return await _db.QueryFirst<RaceSegment>(query);
        }
        
        public async Task<IEnumerable<RaceInstanceChaoSegment>> GetRaceInstanceSegments(RaceInstance raceInstance, int index, IChaoWorldConnection? conn = null)
        {
            return await _db.Execute(conn => conn.QueryAsync<RaceInstanceChaoSegment>($@"
                select cs.*
                from raceinstancechaosegments cs
                join racesegments s
                on cs.racesegmentid = s.id
                where cs.raceinstanceid = {raceInstance.Id}
                and s.raceindex = {index}
            "));
        }

        public async Task<int> GetTotalTimeForSegments(long raceInstanceId, long chaoId)
        {
            return await _db.Execute(conn => conn.QuerySingleAsync<int>($@"
                select coalesce(sum(segmenttimeseconds), 0)
                from raceinstancechaosegments
                where raceinstanceid = {raceInstanceId}
                and chaoid = {chaoId}
            "));
        }

        public async Task<RaceInstanceChaoSegment> UpdateRaceInstanceSegment(RaceInstanceChaoSegment segment)
        {
            var query = new Query("raceinstancechaosegments").Where("raceinstanceid", segment.RaceInstanceId)
                .Where("racesegmentid", segment.RaceSegmentId).Where("chaoid", segment.ChaoId)
                .AsUpdate(new
                    {
                        state = segment.State,
                        segmenttimeseconds = segment.SegmentTimeSeconds,
                        totaltimeseconds = segment.TotalTimeSeconds,
                        startstamina = segment.StartStamina,
                        endstamina = segment.EndStamina,
                        startelevation = segment.StartElevation,
                        endelevation = segment.EndElevation
                    });
            if (segment.ChaoId == 1070)
                _logger.Information($"Segment {segment.RaceSegmentId} - End Elevation was {segment.EndElevation}");
            var updatedSegment = await _db.QueryFirst<RaceInstanceChaoSegment>(query, extraSql: "returning *");
            return updatedSegment;
        }

        public async Task<int> GetRemainingStaminaForChao(long raceInstanceId, long chaoId)
        {
            return await _db.Execute(conn => conn.QuerySingleAsync<int>($@"
                select min(endstamina)
                from raceinstancechaosegments
                where raceinstanceid = {raceInstanceId}
                and chaoid = {chaoId}
            "));
        }

        public async Task RetireInstanceChao(long chaoId, long raceInstanceId)
        {
            var query = new Query("raceinstancechaosegments")
                .Where("raceinstanceid", raceInstanceId)
                .Where("chaoid", chaoId)
                .Where("state", RaceInstanceChaoSegment.SegmentStates.NotStarted)
                .AsUpdate(new
                {
                    state = RaceInstanceChaoSegment.SegmentStates.Retired
                });
            await _db.Query<RaceInstanceChaoSegment>(query);
            _logger.Information($"Chao {chaoId} retired from race instance {raceInstanceId}");
        }

        public Task<IEnumerable<Chao>> GetRaceInstanceChao(RaceInstance instance)
        {
            var query = new Query("raceinstancechao").Join("chao", "raceinstancechao.chaoid", "chao.id")
                .Where("raceinstancechao.raceinstanceid", instance.Id)
                .Select("chao.*");
            return _db.Query<Chao>(query);
        }

        public async Task FinalizeRaceInstanceChao(RaceInstance instance)
        {
            // TODO: I don't love that this makes assumptions about the order of the SegmentStates enum to handle retired chao, but for now it will do
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                with ranks as (
	                select
                        	chaoid,
	                        max(totaltimeseconds) finishtime,
	                        max(state) finishstate,
	                        rank() over (order by
		                        (case when max(state) = {(int)RaceInstanceChaoSegment.SegmentStates.Retired} then null else max(totaltimeseconds) end)
	                        asc nulls last) finishposition
	                from raceinstancechaosegments
	                where raceinstanceid = {instance.Id}
	                group by chaoid
                )
                update raceinstancechao c
                set
	                totaltimeseconds = r.finishtime,
	                finishposition = r.finishposition,
                    state = r.finishstate
                from ranks r
                where c.chaoid = r.chaoid
                and c.raceinstanceid = {instance.Id}
            "));
            _logger.Information($"Finalized chao statistics for race instance {instance.Id} of race {instance.RaceId}");
        }

        public async Task LogMessage(string msg)
        {
            _logger.Information(msg);
        }

        public async Task<IEnumerable<Race>> GetAvailableRaces()
        {
            var races = new List<Race>();
            try
            {
                races = (await _db.Execute(async conn => await conn.QueryAsync<Race>($@"
                    select *
                    from races r
                    left join raceinstances i
                    on r.id = i.raceid
                    and i.state not in ({(int)RaceInstance.RaceStates.Completed}, {(int)RaceInstance.RaceStates.Canceled})
                    where r.isenabled = true
                    and i.id is null
                    and r.availableon < (now() at time zone 'utc')
                "))).AsList();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to read races: {e.Message}");
            }
            return races;
        }

        public async Task<ChaoRaceStats> GetRaceStats(long chaoId)
        {
            var stats = await _db.Execute(async conn => await conn.QueryFirstOrDefaultAsync<ChaoRaceStats>($@"
                select r.chaoid,
                    count(r.chaoid) totalraces,
                    sum(case when i.winnerchaoid = r.chaoid then 1 else 0 end) totalwins,
                    sum(case when r.state = 2 then 1 else 0 end) totalretires
                from raceinstancechao r
                join raceinstances i
                on r.raceinstanceid = i.id
                where chaoid = {chaoId}
                group by chaoid
            "));
            return stats;
        }

        public async Task UpdateRacePingSetting(ulong accountId, bool allowPings)
        {
            await _db.Execute(conn => conn.QueryAsync(@$"
                update accounts
                set enableracepings = {allowPings}
                where uid = {accountId}
            "));
            _logger.Information($"Updated account settings for {accountId} (enable race pings: {allowPings}");
        }

        public async Task<IEnumerable<ulong>> GetAccountsToPingForRace(long raceInstanceId)
        {
            return await _db.Execute(conn => conn.QueryAsync<ulong>(@$"
                    select a.uid
                    from raceinstancechao ric
                    join chao c
                    on ric.chaoid = c.id
                    join accounts a
                    on c.gardenid = a.gardenid
                    where ric.raceinstanceid = {raceInstanceId}
                    and c.gardenid > 0
                    and enableracepings = true
                "));
        }
    }
}