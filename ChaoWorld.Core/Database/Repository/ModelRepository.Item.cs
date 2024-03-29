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
        public async Task<Item?> GetInventoryItemByTypeId(int gardenId, int typeId)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<Item>($@"
                    select i.id, i.gardenid, i.quantity, i.createdon, t.*
                    from items i
                    join itemtypes t
                    on i.typeid = t.typeid
                    where i.gardenid = {gardenId}
                    and t.typeid = {typeId}
                    limit 1
            "));
            return item;
        }

        public async Task<Item?> GetInventoryItemByTypeName(int gardenId, string typeName)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<Item>($@"
                    select i.*, t.*
                    from items i
                    join itemtypes t
                    on i.typeid = t.typeid
                    where i.gardenid = {gardenId}
                    and lower(t.name) = lower(@typeName)
                    limit 1
            ", new { typeName }));
            return item;
        }

        public async Task<Item?> GetInventoryItemByTypeNameWithFuzzyMatching(int gardenId, string typeName)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<Item>($@"
                    select i.*, t.*
                    from items i
                    join itemtypes t
                    on i.typeid = t.typeid
                    where i.gardenid = {gardenId}
                    order by similarity(lower(t.name), lower(@typeName)) desc
                    limit 1
            ", new { typeName }));
            return item;
        }

        public async Task<IEnumerable<MarketItem>> GetMarketEnabledEggs(int limit, bool isShiny)
        {
            if (limit < 1) return new List<MarketItem>();

            var items = await _db.Execute(conn => conn.QueryAsync<MarketItem>($@"
                    select *
                    from itemtypes
                    where ismarketenabled = true
                    and categoryid = {(int)ItemBase.ItemCategories.Egg}
                    and isshiny = {isShiny}
                    order by random()
                    limit {limit}
            "));
            return items;
        }

        public async Task<IEnumerable<MarketItem>> GetMarketEnabledFruit(int limit, bool isHyper)
        {
            if (limit < 1) return new List<MarketItem>();

            var condition = isHyper ? "in" : "not in";
            var items = await _db.Execute(conn => conn.QueryAsync<MarketItem>($@"
                    select *
                    from itemtypes
                    where ismarketenabled = true
                    and categoryid = {(int)ItemBase.ItemCategories.Fruit}
                    and effecttypeid {condition} (
                        {(int)ItemBase.ItemEffects.SwimGradeIncrease},
                        {(int)ItemBase.ItemEffects.FlyGradeIncrease},
                        {(int)ItemBase.ItemEffects.RunGradeIncrease},
                        {(int)ItemBase.ItemEffects.PowerGradeIncrease},
                        {(int)ItemBase.ItemEffects.StaminaGradeIncrease},
                        {(int)ItemBase.ItemEffects.IntelligenceGradeIncrease},
                        {(int)ItemBase.ItemEffects.LuckGradeIncrease})
                    order by random()
                    limit {limit}
            "));
            return items;
        }

        public async Task<IEnumerable<MarketItem>> GetMarketEnabledSeeds(int limit)
        {
            if (limit < 1) return new List<MarketItem>();

            var items = await _db.Execute(conn => conn.QueryAsync<MarketItem>($@"
                    select *
                    from itemtypes
                    where ismarketenabled = true
                    and categoryid = {(int)ItemBase.ItemCategories.Seed}
                    order by random()
                    limit {limit}
            "));
            return items;
        }

        public async Task<IEnumerable<MarketItem>> GetMarketEnabledSpecials(int limit)
        {
            if (limit < 1) return new List<MarketItem>();

            var items = await _db.Execute(conn => conn.QueryAsync<MarketItem>($@"
                    select *
                    from itemtypes
                    where ismarketenabled = true
                    and categoryid = {(int)ItemBase.ItemCategories.Special}
                    and effecttypeid != {(int)ItemBase.ItemEffects.Reincarnation}
                    order by random()
                    limit {limit}
            "));
            return items;
        }

        public async Task<IEnumerable<MarketItem>> GetMarketEnabledPotions(int limit)
        {
            if (limit < 1) return new List<MarketItem>();

            var items = await _db.Execute(conn => conn.QueryAsync<MarketItem>($@"
                    select *
                    from itemtypes
                    where ismarketenabled = true
                    and effecttypeid = {(int)ItemBase.ItemEffects.Reincarnation}
                    order by random()
                    limit {limit}
            "));
            return items;
        }

        public async Task<ItemBase?> GetItemBaseByTypeId(int typeId)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<ItemBase>($@"
                    select *
                    from itemtypes
                    where typeid = {typeId}
                    limit 1
            "));
            return item;
        }

        public async Task<ItemBase?> GetItemBaseByTypeName(string name)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<ItemBase>($@"
                    select *
                    from itemtypes
                    where lower(name) like concat('%', lower(@name), '%')
                    limit 1
            ", new { name }));
            return item;
        }

        public async Task<ItemBase?> GetItemBaseByTypeNameWithFuzzyMatching(string name)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<ItemBase>($@"
                    select *
                    from itemtypes
                    order by similarity(lower(name), lower(@name)) desc
                    limit 1
            ", new { name }));
            return item;
        }

        public async Task<Item> AddItem(long gardenId, Item item, IChaoWorldConnection? conn = null)
        {
            var query = new Query("items").AsInsert(new
            {
                gardenid = gardenId,
                categoryid = item.CategoryId,
                typeid = item.TypeId,
                quantity = item.Quantity,
            });
            item = await _db.QueryFirst<Item>(conn, query, "returning *");
            _logger.Information($"Created item {item.Id} ({item.TypeId} - {item.Name}) for garden {item.GardenId} (quantity: {item.Quantity})");
            return item;
        }

        public async Task UpdateItem(Item item, IChaoWorldConnection? conn = null)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update items
                set
                    quantity = {item.Quantity}
                where id = {item.Id};
            "));
            _logger.Information($"Updated item {item.Id} ({item.TypeId} - {item.Name}) for garden {item.GardenId} (new quantity: {item.Quantity})");
        }

        public async Task DeleteItem(long id)
        {
            var query = new Query("items").AsDelete().Where("id", id);
            await _db.ExecuteQuery(query);
            _logger.Information("Deleted item {ItemId}", id);
        }

        public async Task UseItem(Item item, int quantity, IChaoWorldConnection? conn = null)
        {
            if (item.Quantity > quantity)
            {
                item.Quantity -= quantity;
                await UpdateItem(item);
            }
            else
            {
                item.Quantity = 0;
                await DeleteItem(item.Id);
            }
            _logger.Information($"Used item {item.Id} ({item.TypeId} - {item.Name}) ({item.Quantity} remaining)");
        }

        public async Task<MarketItem?> GetMarketItemByTypeId(int typeId)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<MarketItem>($@"
                    select m.quantity, t.*
                    from marketitems m
                    join itemtypes t
                    on m.typeid = t.typeid
                    where t.typeid = {typeId}
                    limit 1
            "));
            return item;
        }

        public async Task<MarketItem?> GetMarketItemByTypeName(string typeName)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<MarketItem>($@"
                    select m.quantity, t.*
                    from marketitems m
                    join itemtypes t
                    on m.typeid = t.typeid
                    where t.name = @typeName
                    limit 1
            ", new { typeName }));
            return item;
        }

        public async Task<MarketItem?> GetMarketItemByTypeNameWithFuzzyMatching(string typeName)
        {
            var item = await _db.Execute(conn => conn.QuerySingleOrDefaultAsync<MarketItem>($@"
                    select m.quantity, t.*
                    from marketitems m
                    join itemtypes t
                    on m.typeid = t.typeid
                    order by similarity(lower(t.name), lower(@typeName)) desc
                    limit 1
            ", new { typeName }));
            return item;
        }

        public async Task<MarketItem> AddMarketItem(MarketItem item, IChaoWorldConnection? conn = null)
        {
            var query = new Query("marketitems").AsInsert(new
            {
                categoryid = item.CategoryId,
                typeid = item.TypeId,
                quantity = item.Quantity
            });
            item = await _db.QueryFirst<MarketItem>(conn, query, "returning *");
            var baseItem = item as ItemBase;
            _logger.Information($"Listed {baseItem.Name} (x{item.Quantity}) on the Black Market");
            return item;
        }

        public async Task UpdateMarketItem(MarketItem item, IChaoWorldConnection? conn = null)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update marketitems
                set
                    quantity = {item.Quantity}
                where typeid = {item.TypeId};
            "));
            _logger.Information($"Updated market item {item.Name} ({item.TypeId}) (new quantity: {item.Quantity})");
        }

        public async Task DeleteMarketItem(MarketItem item)
        {
            var query = new Query("marketitems").AsDelete().Where("typeid", item.TypeId);
            await _db.ExecuteQuery(query);
            _logger.Information($"Deleted market item {item.Name} ({item.TypeId})");
        }

        public async Task BuyMarketItem(MarketItem item, int quantity, IChaoWorldConnection? conn = null)
        {
            if (item.Quantity > quantity)
            {
                item.Quantity -= quantity;
                await UpdateMarketItem(item);
            }
            else
            {
                await DeleteMarketItem(item);
            }
            _logger.Information($"Item {item.Name} ({item.TypeId}) was purchased ({item.Quantity} remaining)");
        }

        public async Task ClearMarketListings(IChaoWorldConnection? conn = null)
        {
            var query = new Query("marketitems").AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Cleared Black Market listings");
        }
    }
}