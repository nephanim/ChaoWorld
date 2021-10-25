#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Chao?> GetMember(ChaoId id)
        {
            var query = new Query("members").Where("id", id);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetMemberByHid(string hid, GardenId? system = null)
        {
            var query = new Query("members").Where("hid", hid.ToLower());
            if (system != null)
                query = query.Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetMemberByGuid(Guid uuid)
        {
            var query = new Query("members").Where("uuid", uuid);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetMemberByName(GardenId system, string name)
        {
            var query = new Query("members").WhereRaw(
                "lower(name) = lower(?)",
                name.ToLower()
            ).Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public Task<Chao?> GetMemberByDisplayName(GardenId system, string name)
        {
            var query = new Query("members").WhereRaw(
                "lower(display_name) = lower(?)",
                name.ToLower()
            ).Where("system", system);
            return _db.QueryFirst<Chao?>(query);
        }

        public async Task<Chao> CreateMember(GardenId systemId, string memberName, IPKConnection? conn = null)
        {
            var query = new Query("members").AsInsert(new
            {
                hid = new UnsafeLiteral("find_free_member_hid()"),
                system = systemId,
                name = memberName
            });
            var member = await _db.QueryFirst<Chao>(conn, query, "returning *");
            _logger.Information("Created {ChaoId} in {GardenId}: {MemberName}",
                member.Id, systemId, memberName);
            return member;
        }

        public Task<Chao> UpdateMember(ChaoId id, MemberPatch patch, IPKConnection? conn = null)
        {
            _logger.Information("Updated {ChaoId}: {@MemberPatch}", id, patch);
            var query = patch.Apply(new Query("members").Where("id", id));
            return _db.QueryFirst<Chao>(conn, query, extraSql: "returning *");
        }

        public Task DeleteMember(ChaoId id)
        {
            _logger.Information("Deleted {ChaoId}", id);
            var query = new Query("members").AsDelete().Where("id", id);
            return _db.ExecuteQuery(query);
        }
    }
}