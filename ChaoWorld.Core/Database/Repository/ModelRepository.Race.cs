#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;
using System.Collections.Generic;

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

        public async Task<RaceInstance?> GetRaceInstanceById(long id)
        {
            var query = new Query("raceinstances").Where("id", id);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance?> GetRaceInstanceByRaceId(int id)
        {
            var query = new Query("raceinstances").Where("raceid", id);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<RaceInstance?> GetRaceInstanceByName(string name)
        {
            var query = new Query("raceinstances").Join("races", "races.id", "raceinstances.raceid", "=").WhereRaw(
                "lower(races.name) = lower(?)",
                name.ToLower().Replace("\"", string.Empty))
            .Select("raceinstances.*")
            .OrderByDesc("createdon").Limit(1);
            return await _db.QueryFirst<RaceInstance?>(query);
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
            var query = new Query("raceinstances").AsInsert(new
            {
                raceid = race.Id,
                state = RaceInstance.RaceStates.New
            });
            var raceInstance = await _db.QueryFirst<RaceInstance>(conn, query, "returning *");
            _logger.Information($"Created instance {raceInstance.Id} of race {race.Id} ({race.Name})");
            return raceInstance;
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

        public Task<RaceSegment> GetRaceSegment(int raceId, int index)
        {
            var query = new Query("racesegments").Where("raceid", raceId).Where("raceindex", index);
            return _db.QueryFirst<RaceSegment>(query);
        }
        
        public Task<IEnumerable<RaceInstanceChaoSegment>> GetRaceInstanceSegments(RaceInstance raceInstance, int index, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstancechaosegments")
                .Join("racesegments", "racesegmentid", "id", "=")
                .Where("raceinstanceid", raceInstance.Id)
                .Where("raceindex", index);
            return _db.Query<RaceInstanceChaoSegment>(query);
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
            "));
            _logger.Information($"Finalized chao statistics for race instance {instance.Id} of race {instance.RaceId}");
        }

        public async Task LogMessage(string msg)
        {
            _logger.Information(msg);
        }

        public async Task InstantiateRaces()
        {
            try
            {
                var instances = await _db.Execute(async conn => await conn.QueryAsync<RaceInstance>($@"
                    insert into raceinstances (raceid, state)
                    select r.id, {(int)RaceInstance.RaceStates.New}
                    from races r
                    left join raceinstances i
                    on r.id = i.raceid
                    and i.state not in ({(int)RaceInstance.RaceStates.Completed}, {(int)RaceInstance.RaceStates.Canceled})
                    where i.id is null
                    and r.availableon < current_timestamp
                    returning *
                "));
                var instanceCount = instances.AsList().Count;
                _logger.Information($"Created {instanceCount} new race instances");
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to instantiate races: {e.Message}");
            }
            
        }
    }
}