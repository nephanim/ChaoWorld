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
        public Task<Chao?> GetChao(long id)
        {
            var query = new Query("chao").Where("id", id);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByName(GardenId garden, string name)
        {
            var query = new Query("chao").WhereRaw(
                "lower(name) = lower(?)",
                name.ToLower()
            ).Where("gardenid", garden);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByName(string name)
        {
            var query = new Query("chao").WhereRaw(
                "lower(name) = lower(?)",
                name.ToLower()
            );
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByNameWithFuzzyMatching(string name)
        {
            var query = new Query("chao")
                .OrderByRaw("similarity(name, lower(?)) desc", name.ToLower().Replace("\"", string.Empty))
                .Limit(1);
            return _db.QueryFirst<Chao?>(query);
        }

        public async Task<Chao?> GetRandomChao(int gardenId)
        {
            var chao = await _db.Execute(conn => conn.QueryAsync<Chao?>($@"
                    select *
                    from chao
                    where gardenid = {gardenId}
            "));
            var arr = chao.AsList().ToArray();
            
            var randomIndex = new Random().Next(0, arr.Length - 1);
            return arr[randomIndex];
        }

        public async Task<IEnumerable<Chao>> GetChaoReadyForFirstEvolution()
        {
            var query = new Query("chao")
                .Where("evolutionstate", Chao.EvolutionStates.Child)
                .WhereRaw("trunc(date_part('day', now() at time zone 'utc' - rebirthon)) >= 7");
            return await _db.Query<Chao>(query);
        }

        public async Task<Chao> CreateChao(GardenId garden, Chao chao, IChaoWorldConnection? conn = null)
        {
            var query = new Query("chao").AsInsert(new
            {
                gardenid = garden,
                name = chao.Name,
                isshiny = chao.IsShiny,
                istwotone = chao.IsTwoTone,
                primarycolor = (int)chao.PrimaryColor,
                secondarycolor = (int?)chao.SecondaryColor,
                swimgrade = chao.SwimGrade,
                flygrade = chao.FlyGrade,
                rungrade = chao.RunGrade,
                powergrade = chao.PowerGrade,
                staminagrade = chao.StaminaGrade,
                intelligencegrade = chao.IntelligenceGrade,
                luckgrade = chao.LuckGrade
            });
            chao = await _db.QueryFirst<Chao>(conn, query, "returning *");
            _logger.Information("Created {ChaoId} in {GardenId}: {ChaoName}",
                chao.Id, garden, chao.Name);
            return chao;
        }

        public async Task UpdateChao(Chao chao, IChaoWorldConnection? conn = null)
        {
            var firstEvolution = chao.FirstEvolutionType.HasValue
                ? $"firstevolutiontype = {(int)chao.FirstEvolutionType.Value}, "
                : $"firstevolutiontype = null, ";
            var secondEvolution = chao.SecondEvolutionType.HasValue
                ? $"secondevolutiontype = {(int)chao.SecondEvolutionType.Value}, "
                : $"secondevolutiontype = null, ";

             await _db.Execute(conn => conn.QueryAsync<int>($@"
                update chao
                set
                    tag = @tag,
                    rebirthon = '{chao.RebirthOn}',
                    reincarnations = {chao.Reincarnations},
                    reincarnationstatfactor = {chao.ReincarnationStatFactor},
                    evolutionstate = {(int)chao.EvolutionState},
                    {firstEvolution}
                    {secondEvolution}
                    alignment = {(int)chao.Alignment},
                    alignmentvalue = {chao.AlignmentValue},
                    flyswimaffinity = {chao.FlySwimAffinity},
                    runpoweraffinity = {chao.RunPowerAffinity},
                    isreversed = {chao.IsReversed},
                    swimgrade = {(int)chao.SwimGrade},
                    swimlevel = {chao.SwimLevel},
                    swimvalue = {chao.SwimValue},
                    swimprogress = {chao.SwimProgress},
                    flygrade = {(int)chao.FlyGrade},
                    flylevel = {chao.FlyLevel},
                    flyvalue = {chao.FlyValue},
                    flyprogress = {chao.FlyProgress},
                    rungrade = {(int)chao.RunGrade},
                    runlevel = {chao.RunLevel},
                    runvalue = {chao.RunValue},
                    runprogress = {chao.RunProgress},
                    powergrade = {(int)chao.PowerGrade},
                    powerlevel = {chao.PowerLevel},
                    powervalue = {chao.PowerValue},
                    powerprogress = {chao.PowerProgress},
                    staminagrade = {(int)chao.StaminaGrade},
                    staminalevel = {chao.StaminaLevel},
                    staminavalue = {chao.StaminaValue},
                    staminaprogress = {chao.StaminaProgress},
                    intelligencegrade = {(int)chao.IntelligenceGrade},
                    intelligencelevel = {chao.IntelligenceLevel},
                    intelligencevalue = {chao.IntelligenceValue},
                    intelligenceprogress = {chao.IntelligenceProgress},
                    luckgrade = {(int)chao.LuckGrade},
                    lucklevel = {chao.LuckLevel},
                    luckvalue = {chao.LuckValue},
                    luckprogress = {chao.LuckProgress}
                where id = {chao.Id.Value};
            ", new { tag = chao.Tag }));
            _logger.Information($"Updated chao {chao.Id.Value} ({chao.Name}) for garden {chao.GardenId}");
        }

        public Task<Chao> UpdateChao(ChaoId id, ChaoPatch patch, IChaoWorldConnection? conn = null)
        {
            _logger.Information("Updated {ChaoId}: {@ChaoPatch}", id, patch);
            var query = patch.Apply(new Query("chao").Where("id", id));
            return _db.QueryFirst<Chao>(conn, query, extraSql: "returning *");
        }

        public Task DeleteChao(ChaoId id)
        {
            _logger.Information("Deleted {ChaoId}", id);
            var query = new Query("chao").AsDelete().Where("id", id);
            return _db.ExecuteQuery(query);
        }

        public async Task<IEnumerable<Chao>> ReincarnateEligibleNpcChao(IChaoWorldConnection? conn = null)
        {
            var chao = (await _db.Execute(conn => conn.QueryAsync<Chao>($@"
                update chao
                set reincarnations = reincarnations + 1,
	                reincarnationstatfactor = reincarnationstatfactor + 0.01,
	                swimlevel = 1,
	                swimvalue = floor(swimvalue*reincarnationstatfactor),
	                flylevel = 1,
	                flyvalue = floor(flyvalue*reincarnationstatfactor),
	                runlevel = 1,
	                runvalue = floor(runvalue*reincarnationstatfactor),
	                powerlevel = 1,
	                powervalue = floor(powervalue*reincarnationstatfactor),
	                staminalevel = 1,
	                staminavalue = floor(staminavalue*reincarnationstatfactor),
	                intelligencelevel = 1,
	                intelligencevalue = floor(intelligencevalue*reincarnationstatfactor),
	                lucklevel = 1,
	                luckvalue = floor(luckvalue*reincarnationstatfactor),
	                rebirthon = current_timestamp
                where gardenid = 0
                    and id != 1
                    and swimlevel = 99
                    and flylevel = 99
                    and runlevel = 99
                    and powerlevel = 99
                    and staminalevel = 99
                    and intelligencelevel = 99
                    and lucklevel = 99
                    )
                returning *
            "))).AsList();
            if (chao.Count > 0)
                _logger.Information($"Reincarnated {chao.Count} eligible NPCs");
            return chao;
        }
    }
}