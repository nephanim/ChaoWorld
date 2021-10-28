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
            var query = new Query("raceinstances").Join("races", "raceid", "id", "=")
                .Where("raceinstances.id", "=", id);
            return await _db.QueryFirst<Race?>(query, "returning races.*");
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
                name.ToLower()
            ).OrderByDesc("createdon").Limit(1);
            return await _db.QueryFirst<RaceInstance?>(query);
        }

        public async Task<int> GetRaceInstanceChaoCount(long raceInstanceId)
        {
            var query = new Query("raceinstancechao").Where("raceinstanceid", raceInstanceId).Select("count(raceinstanceid)");
            return await _db.QueryFirst<int>(query);
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

        public async Task<RaceInstance> CompleteRaceInstance(RaceInstance raceInstance, IChaoWorldConnection? conn = null)
        {
            // TODO: This is really inefficient, combine those subqueries when you can wrap your brain around it please
            //  Other problems - we should be updating the raceinstance chao too, so probably do that first and query it
            //      Also, this won't handle the case where all the chao retire early, one would just win by default
            var query = new Query(@$"
                update raceinstances
                set completedon = current_timestamp,
                    state = {RaceInstance.RaceStates.Completed},
                    winnerchaoid = (
                        select ics.chaoid
                        from raceinstancechaosegments ics
	                    join racesegments rs
	                    on ics.racesegmentid = rs.id
	                    where ics.raceinstanceid = {raceInstance.Id}
	                    order by rs.raceindex desc, ics.totaltimeseconds asc
	                    limit 1
                    ),
                    totaltimeseconds = (
                        select min(totaltimeseconds)
                        from raceinstancechaosegments
                        where raceinstanceid = {raceInstance.Id}
                        and state != {RaceInstanceChaoSegment.SegmentStates.Retired}
                    )
                where id = {raceInstance.Id}
            ");
            var updatedInstance = await _db.QueryFirst<RaceInstance>(query, extraSql: "returning *");
            _logger.Information($"Completed instance {raceInstance.Id} of race {raceInstance.RaceId}");
            return updatedInstance;
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
            var query = new Query(@$"
                insert into raceinstancechaosegments
	                (raceinstanceid, racesegmentid, chaoid)
                select i.id, s.id, c.chaoid
                from raceinstances i
                join racesegments s
                on i.raceid = s.raceid
                join raceinstancechao ic
                on i.id = ic.raceinstanceid
                join chao c
                on ic.chaoid = c.id
                where i.id = {raceInstance.Id}
            ");
            await _db.ExecuteQuery(query);
            _logger.Information($"Added segments to instance {raceInstance.Id} of race {raceInstance.RaceId}");
        }

        public Task<RaceSegment> GetRaceSegment(int raceId, int index)
        {
            var query = new Query("racesegments").Where("raceid", raceId).Where("raceindex", index);
            return _db.QueryFirst<RaceSegment>(query);
        }
        
        public Task<IEnumerable<RaceInstanceChaoSegment>> GetRaceInstanceSegments(RaceInstance raceInstance, int index, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstancechaosegments").Where("raceinstanceid", raceInstance.Id)
                .Where("raceindex", index);
            return _db.Query<RaceInstanceChaoSegment>(query);
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

        public async Task UpdateRaceProgress(IChaoWorldConnection? conn = null)
        {
            await UpdateInstanceSegments();
            await InstantiateRaces();
        }

        private async Task UpdateInstanceSegments()
        {
            
        }

        private async Task InstantiateRaces()
        {
            var racesInstantiated = await _db.Execute(conn => conn.QuerySingleAsync<int>($@"
                insert into raceinstances (raceid, state)
                select r.id, {RaceInstance.RaceStates.New}
                from races r
                left join raceinstances i
                on r.id = i.raceid
                and i.state not in ({RaceInstance.RaceStates.Completed}, {RaceInstance.RaceStates.Canceled})
                where i.id is null
                and r.availableon < current_timestamp
                returning count(*)
            "));
            _logger.Information($"Created {racesInstantiated} new instances for races");
        }
    }
}