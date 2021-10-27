#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;

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
            var prizeRings = new Random().Next(10, 100); //TODO: Move this somewhere else, we should really be reading a prize amount from races
            var racesInstantiated = await _db.Execute(conn => conn.QuerySingleAsync<int>($@"
                insert into raceinstances (raceid, state, prizerings)
                select r.id, {RaceInstance.RaceStates.New}, {prizeRings}
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