#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Garden?> GetGarden(long id)
        {
            var query = new Query("gardens").Where("id", id);
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<Garden?> GetGardenByAccount(ulong accountId)
        {
            var query = new Query("accounts").Select("gardens.*").LeftJoin("gardens", "gardens.id", "accounts.gardenid", "=").Where("uid", accountId);
            return _db.QueryFirst<Garden?>(query);
        }

        public Task<IEnumerable<ulong>> GetGardenAccounts(GardenId garden)
        {
            var query = new Query("accounts").Select("uid").Where("gardenid", garden);
            return _db.Query<ulong>(query);
        }

        public IAsyncEnumerable<Chao> GetGardenChao(GardenId garden)
        {
            var query = new Query("chao").Where("gardenid", garden);
            return _db.QueryStream<Chao>(query);
        }

        public Task<int> GetGardenChaoCount(GardenId garden)
        {
            var query = new Query("chao").SelectRaw("count(*)").Where("gardenid", garden);

            return _db.QueryFirst<int>(query);
        }

        public async Task<Garden> CreateGarden(IChaoWorldConnection? conn = null)
        {
            var now = NodaTime.SystemClock.Instance.GetCurrentInstant();
            var query = new Query("gardens").AsInsert(new
            {
                ringbalance = 0,
                createdon = now,
                nextcollecton = now
            });
            var garden = await _db.QueryFirst<Garden>(conn, query, extraSql: "returning *");
            _logger.Information("Created {GardenId}", garden.Id);
            return garden;
        }

        public async Task<Garden> UpdateGarden(Garden garden)
        {
            var query = new Query("gardens").Where("id", garden.Id.Value).AsUpdate(new
            {
                ringbalance = garden.RingBalance,
                nextcollecton = garden.NextCollectOn
            });
            var updatedGarden = await _db.QueryFirst<Garden>(query, extraSql: "returning *");
            return updatedGarden;
        }

        public Task<Garden> UpdateGarden(GardenId id, ChaoPatch patch, IChaoWorldConnection? conn = null)
        {
            _logger.Information("Updated {GardenId}: {@ChaoPatch}", id, patch);
            var query = patch.Apply(new Query("gardens").Where("id", id));
            return _db.QueryFirst<Garden>(conn, query, extraSql: "returning *");
        }

        public Task AddAccount(GardenId garden, ulong accountId, IChaoWorldConnection? conn = null)
        {
            // We have "on conflict do nothing" since linking an account when it's already linked to the same garden is idempotent
            // This is used in import/export, although the !link command checks for this case beforehand

            var query = new Query("accounts").AsInsert(new
            {
                gardenid = garden,
                uid = accountId,
            });

            _logger.Information("Linked account {UserId} to {GardenId}", accountId, garden);
            return _db.ExecuteQuery(conn, query, extraSql: "on conflict do nothing");
        }

        public async Task RemoveAccount(GardenId garden, ulong accountId)
        {
            var query = new Query("accounts").AsDelete().Where("uid", accountId).Where("gardenid", garden);
            await _db.ExecuteQuery(query);
            _logger.Information("Unlinked account {UserId} from {GardenId}", accountId, garden);
        }

        public Task DeleteGarden(GardenId id)
        {
            var query = new Query("gardens").AsDelete().Where("id", id);
            _logger.Information("Deleted {GardenId}", id);
            return _db.ExecuteQuery(query);
        }
    }
}