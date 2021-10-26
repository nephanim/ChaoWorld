#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;

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

        public async Task<Chao> CreateChao(GardenId garden, Chao chao, IPKConnection? conn = null)
        {
            var query = new Query("chao").AsInsert(new
            {
                gardenid = garden,
                name = chao.Name,
                isactive = chao.IsActive,
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

        public Task<Chao> UpdateChao(ChaoId id, GardenPatch patch, IPKConnection? conn = null)
        {
            _logger.Information("Updated {ChaoId}: {@GardenPatch}", id, patch);
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