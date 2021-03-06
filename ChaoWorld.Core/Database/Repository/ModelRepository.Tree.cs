#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;
using System.Collections.Generic;
using NodaTime;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Tree?> GetTree(long id)
        {
            var query = new Query("trees").Where("id", id);
            return _db.QueryFirst<Tree?>(query);
        }
        
        public Task<IEnumerable<Tree>> GetTreesForGarden(int gardenId)
        {
            var query = new Query("trees")
                .Where("gardenid", gardenId);
            return _db.Query<Tree>(query);
        }

        public Task<Tree?> GetThirstiestTreeForGarden(int gardenId)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var query = new Query("trees")
                .Where("gardenid", gardenId)
                .Where("nextwatering", "<", now)
                .OrderBy("nextwatering")
                .Limit(1);
            return _db.QueryFirst<Tree?>(query);
        }

        public Task<Tree?> GetMostBountifulTreeForGarden(int gardenId)
        {
            var query = new Query("trees")
                .Where("gardenid", gardenId)
                .Where("fruitquantity", ">", 0)
                .OrderByDesc("fruitquantity")
                .Limit(1);
            return _db.QueryFirst<Tree?>(query);
        }

        public Task<Tree?> GetTreeByName(int gardenId, string name)
        {
            var query = new Query("trees")
                .Where("gardenid", gardenId)
                .WhereRaw("lower(name) = lower(?)", name.Replace("\"", string.Empty))
                .Limit(1);
            return _db.QueryFirst<Tree?>(query);
        }

        public Task<Tree?> GetTreeByNameWithFuzzyMatching(int gardenId, string name)
        {
            var query = new Query("trees")
                .Where("gardenid", gardenId)
                .OrderByRaw("similarity(name, lower(?)) desc", name.ToLower().Replace("\"", string.Empty))
                .Limit(1);
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

        public async Task GrowFruitForAllTrees(IChaoWorldConnection? conn = null)
        {
            await _db.Execute(conn => conn.QueryAsync($@"
                update trees
                set health = (
                        case when health < 1 then health
                            when floor(random()*100) < health then health - 1
                            else health
                        end),
	                fruitquantity = (
		                case when fruitquantity >= 10 then fruitquantity
			                when floor(random()*100) < health then fruitquantity + 1
			                else fruitquantity
		                end)
                where health > 0
            "));
            _logger.Information($"Updated tree health and fruit quantity");
        }
    }
}