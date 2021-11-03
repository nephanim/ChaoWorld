#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;

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

        // TODO: Add other properties in here - just don't want to troubleshoot issues with types / evolution until I get there
        public async Task UpdateChao(Chao chao, IChaoWorldConnection? conn = null)
        {
             await _db.Execute(conn => conn.QueryAsync<int>($@"
                update chao
                set
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
            "));
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
    }
}