#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Garden?> GetSystem(GardenId id)
        {
            var query = new Query("systems").Where("id", id);
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<Garden?> GetSystemByGuid(Guid id)
        {
            var query = new Query("systems").Where("uuid", id);
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<Garden?> GetSystemByAccount(ulong accountId)
        {
            var query = new Query("accounts").Select("systems.*").LeftJoin("systems", "systems.id", "accounts.system", "=").Where("uid", accountId);
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<Garden?> GetSystemByHid(string hid)
        {
            var query = new Query("systems").Where("hid", hid.ToLower());
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<IEnumerable<ulong>> GetSystemAccounts(GardenId system)
        {
            var query = new Query("accounts").Select("uid").Where("system", system);
            return _db.Query<ulong>(query);
        }

        public IAsyncEnumerable<Chao> GetSystemMembers(GardenId system)
        {
            var query = new Query("members").Where("system", system);
            return _db.QueryStream<Chao>(query);
        }

        public Task<int> GetSystemMemberCount(GardenId system, PrivacyLevel? privacyFilter = null)
        {
            var query = new Query("members").SelectRaw("count(*)").Where("system", system);
            if (privacyFilter != null)
                query.Where("member_visibility", (int)privacyFilter.Value);

            return _db.QueryFirst<int>(query);
        }

        public Task<int> GetSystemGroupCount(GardenId system, PrivacyLevel? privacyFilter = null)
        {
            var query = new Query("groups").SelectRaw("count(*)").Where("system", system);
            if (privacyFilter != null)
                query.Where("visibility", (int)privacyFilter.Value);

            return _db.QueryFirst<int>(query);
        }

        public async Task<Garden> CreateSystem(string? systemName = null, IPKConnection? conn = null)
        {
            var query = new Query("systems").AsInsert(new
            {
                hid = new UnsafeLiteral("find_free_system_hid()"),
                name = systemName
            });
            var system = await _db.QueryFirst<Garden>(conn, query, extraSql: "returning *");
            _logger.Information("Created {GardenId}", system.Id);
            return system;
        }

        public Task<Garden> UpdateSystem(GardenId id, SystemPatch patch, IPKConnection? conn = null)
        {
            _logger.Information("Updated {GardenId}: {@SystemPatch}", id, patch);
            var query = patch.Apply(new Query("systems").Where("id", id));
            return _db.QueryFirst<Garden>(conn, query, extraSql: "returning *");
        }

        public Task AddAccount(GardenId system, ulong accountId, IPKConnection? conn = null)
        {
            // We have "on conflict do nothing" since linking an account when it's already linked to the same system is idempotent
            // This is used in import/export, although the pk;link command checks for this case beforehand

            var query = new Query("accounts").AsInsert(new
            {
                system = system,
                uid = accountId,
            });

            _logger.Information("Linked account {UserId} to {GardenId}", accountId, system);
            return _db.ExecuteQuery(conn, query, extraSql: "on conflict do nothing");
        }

        public async Task RemoveAccount(GardenId system, ulong accountId)
        {
            var query = new Query("accounts").AsDelete().Where("uid", accountId).Where("system", system);
            await _db.ExecuteQuery(query);
            _logger.Information("Unlinked account {UserId} from {GardenId}", accountId, system);
        }

        public Task DeleteSystem(GardenId id)
        {
            var query = new Query("systems").AsDelete().Where("id", id);
            _logger.Information("Deleted {GardenId}", id);
            return _db.ExecuteQuery(query);
        }
    }
}