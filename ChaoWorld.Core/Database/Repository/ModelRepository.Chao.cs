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

        public async Task<Chao?> GetRandomChao(int gardenId, int lowerStatThreshold = 0, int upperStatThreshold = 10000)
        {
            var chao = await _db.Execute(conn => conn.QueryAsync<Chao?>($@"
                select *
                from chao
                where gardenid = {gardenId}
                and (swimvalue + flyvalue + runvalue + powervalue + staminavalue + intelligencevalue + luckvalue)/7 >= {lowerStatThreshold}
                and (swimvalue + flyvalue + runvalue + powervalue + staminavalue + intelligencevalue + luckvalue)/7 <= {upperStatThreshold}
            "));
            var arr = chao.AsList().ToArray();

            if (arr.Length < 1)
                return null;
            var randomIndex = new Random().Next(0, arr.Length);
            return arr[randomIndex];
        }

        public async Task<IEnumerable<Chao>> GetChaoInGarden(int gardenId)
        {
            var query = new Query("chao")
                .Where("gardenid", gardenId);
            return await _db.Query<Chao>(query);
        }

        public async Task<IEnumerable<Chao>> GetChaoReadyForFirstEvolution()
        {
            var query = new Query("chao")
                .Where("evolutionstate", Chao.EvolutionStates.Child)
                .WhereRaw("trunc(date_part('day', now() at time zone 'utc' - rebirthon)) >= 7");
            return await _db.Query<Chao>(query);
        }

        public async Task<IEnumerable<Chao>> GetChaoReadyForSecondEvolution()
        {
            var query = new Query("chao")
                .Where("evolutionstate", Chao.EvolutionStates.First)
                .WhereRaw("trunc(date_part('day', now() at time zone 'utc' - rebirthon)) >= 21");
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

        public Task<ChaoGenes?> GetChaoGenes(long chaoId)
        {
            var query = new Query("genes").Where("chaoid", chaoId);
            return _db.QueryFirst<ChaoGenes?>(query);
        }

        public async Task<ChaoGenes> CreateChaoGenes(ChaoGenes genes, IChaoWorldConnection? conn = null)
        {
            var query = new Query("genes").AsInsert(new
            {
                chaoid = genes.ChaoId,
                firstparentid = genes.FirstParentId,
                secondparentid = genes.SecondParentId,
                firstcolorid = genes.FirstColorId,
                secondcolorid = genes.SecondColorId,
                firstshiny = genes.FirstShiny,
                secondshiny = genes.SecondShiny,
                firsttwotone = genes.FirstTwoTone,
                secondtwotone = genes.SecondTwoTone,
                firstswimgrade = genes.FirstSwimGrade,
                secondswimgrade = genes.SecondSwimGrade,
                firstflygrade = genes.FirstFlyGrade,
                secondflygrade = genes.SecondFlyGrade,
                firstrungrade = genes.FirstRunGrade,
                secondrungrade = genes.SecondRunGrade,
                firstpowergrade = genes.FirstPowerGrade,
                secondpowergrade = genes.SecondPowerGrade,
                firststaminagrade = genes.FirstStaminaGrade,
                secondstaminagrade = genes.SecondStaminaGrade,
                firstintelligencegrade = genes.FirstIntelligenceGrade,
                secondintelligencegrade = genes.SecondIntelligenceGrade,
                firstluckgrade = genes.FirstLuckGrade,
                secondluckgrade = genes.SecondLuckGrade
            });
            genes = await _db.QueryFirst<ChaoGenes>(conn, query, "returning *");
            _logger.Information($"Recorded genes for {genes.ChaoId}");
            return genes;
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
                    swimfactor = {chao.SwimFactor},
                    flyfactor = {chao.FlyFactor},
                    runfactor = {chao.RunFactor},
                    powerfactor = {chao.PowerFactor},
                    staminafactor = {chao.StaminaFactor},
                    intelligencefactor = {chao.IntelligenceFactor},
                    luckfactor = {chao.LuckFactor},
                    evolutionstate = {(int)chao.EvolutionState},
                    {firstEvolution}
                    {secondEvolution}
                    alignment = {(int)chao.Alignment},
                    alignmentvalue = {chao.AlignmentValue},
                    flyswimaffinity = {chao.FlySwimAffinity},
                    runpoweraffinity = {chao.RunPowerAffinity},
                    isreversed = {chao.IsReversed},
                    isfertile = {chao.IsFertile},
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
                    luckprogress = {chao.LuckProgress},
                    energy = {chao.Energy},
                    hunger = {chao.Hunger}
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

        public async Task UpdateChaoEnergyAndHunger(IChaoWorldConnection? conn = null)
        {
            // If hunger is 0 (chao is full), chao will always get 20 energy per hour (which is the cap)
            // If chao is hungry, they will likely get less... maybe even nothing if they have 100 hunger
            // Hunger also goes up by 1 every hour no matter what
            // This stuff doesn't matter for NPC chao, so we'll just skip updating them
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update chao
                set
                    energy = least(20, energy + floor(random() * (100-hunger) / 10)),
                    hunger = case when hunger >= 100 then hunger else hunger + 1 end
                where gardenid != 0;
            "));
            _logger.Information($"Updated chao energy and hunger levels");
        }

        public async Task<IEnumerable<Chao>> ReincarnateEligibleNpcChao(IChaoWorldConnection? conn = null)
        {
            var chao = (await _db.Execute(conn => conn.QueryAsync<Chao>($@"
                update chao
                set reincarnations = reincarnations + 1,
	                swimfactor = swimfactor + 0.01,
                    flyfactor = flyfactor + 0.01,
                    runfactor = runfactor + 0.01,
                    powerfactor = powerfactor + 0.01,
                    staminafactor = staminafactor + 0.01,
                    intelligencefactor = intelligencefactor + 0.01,
                    luckfactor = luckfactor + 0.01,
	                swimlevel = 1,
	                swimvalue = floor(swimvalue*swimfactor),
	                flylevel = 1,
	                flyvalue = floor(flyvalue*flyfactor),
	                runlevel = 1,
	                runvalue = floor(runvalue*runfactor),
	                powerlevel = 1,
	                powervalue = floor(powervalue*powerfactor),
	                staminalevel = 1,
	                staminavalue = floor(staminavalue*staminafactor),
	                intelligencelevel = 1,
	                intelligencevalue = floor(intelligencevalue*intelligencefactor),
	                lucklevel = 1,
	                luckvalue = floor(luckvalue*luckfactor),
                    flyswimaffinity = floor(random()*200-100),
                    runpoweraffinity = floor(random()*200-100),
                    alignmentvalue = floor(random()*200-100),
	                rebirthon = current_timestamp
                where gardenid = 0
                    and id != 1
                    and SwimLevel = 99
                    and FlyLevel = 99
                    and RunLevel = 99
                    and PowerLevel = 99
                    and StaminaLevel = 99
                    and IntelligenceLevel = 99
                    and LuckLevel = 99
                returning *
            "))).AsList();
            if (chao.Count > 0)
                _logger.Information($"Reincarnated {chao.Count} eligible NPCs");
            return chao;
        }
    }
}