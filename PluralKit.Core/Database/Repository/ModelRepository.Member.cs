#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Chao?> GetChao(ChaoId id)
        {
            var query = new Query("chao").Where("id", id);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByHid(string hid, GardenId? system = null)
        {
            var query = new Query("chao").Where("hid", hid.ToLower());
            if (system != null)
                query = query.Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByGuid(Guid uuid)
        {
            var query = new Query("chao").Where("uuid", uuid);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByName(GardenId system, string name)
        {
            var query = new Query("chao").WhereRaw(
                "lower(name) = lower(?)",
                name.ToLower()
            ).Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetChaoByDisplayName(GardenId system, string name)
        {
            var query = new Query("chao").WhereRaw(
                "lower(display_name) = lower(?)",
                name.ToLower()
            ).Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public async Task<Chao> CreateChao(GardenId systemId, string chaoName, IPKConnection? conn = null)
        {
            var query = new Query("chao").AsInsert(new
            {
                hid = new UnsafeLiteral("find_free_chao_hid()"),
                system = systemId,
                name = chaoName
            });
            var chao = await _db.QueryFirst<Chao>(conn, query, "returning *");
            _logger.Information("Created {ChaoId} in {GardenId}: {ChaoName}",
                chao.Id, systemId, chaoName);
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