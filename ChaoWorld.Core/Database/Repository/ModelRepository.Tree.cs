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
        public Task<Tree?> GetTree(long id)
        {
            var query = new Query("trees").Where("id", id);
            return _db.QueryFirst<Tree?>(query);
        }

        public async Task<Tree> CreateTree(Tree tree, IChaoWorldConnection? conn = null)
        {
            var query = new Query("trees").AsInsert(new
            {
                gardenid = tree.GardenId,
                fruittypeid = tree.FruitTypeId,
                name = tree.Name
            });
            tree = await _db.QueryFirst<Tree>(conn, query, "returning *");
            _logger.Information($"Added tree {tree.Id} with fruit {tree.FruitTypeId} to garden {tree.GardenId}");
            return tree;
        }

        public async Task<Tree> UpdateTree(Tree tree)
        {
            var query = new Query("trees").Where("id", tree.Id).AsUpdate(new
            {
                fruitquantity = tree.FruitQuantity,
                nextwatering = tree.NextWatering,
                health = tree.Health
            });
            tree = await _db.QueryFirst<Tree>(query, extraSql: "returning *");
            return tree;
        }

        public async Task DeleteTree(Tree tree)
        {
            var query = new Query("trees").AsDelete().Where("id", tree.Id);
            await _db.ExecuteQuery(query);
            _logger.Information($"Deleted tree {tree.Id} from garden {tree.GardenId}");
        }

        public Task<int> GetGardenTreeCount(int gardenId)
        {
            var query = new Query("trees").SelectRaw("count(*)").Where("gardenid", gardenId);
            return _db.QueryFirst<int>(query);
        }
    }
}