#nullable enable
using System;
using System.Threading.Tasks;

using SqlKata;
using Dapper;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<Item?> GetItem(long id)
        {
            var query = new Query("items").Where("id", id);
            return _db.QueryFirst<Item?>(query);
        }

        public Task<Item?> GetItemByType(int gardenId, int typeId)
        {
            var query = new Query("items").Where("gardenid", gardenId).Where("typeid", typeId);
            return _db.QueryFirst<Item?>(query);
        }

        public async Task<Item> AddItem(long gardenId, Item item, IChaoWorldConnection? conn = null)
        {
            var query = new Query("items").AsInsert(new
            {
                gardenid = gardenId,
                categoryid = (int)item.ItemCategory,
                typeid = (int)item.ItemType,
                quantity = item.Quantity,
            });
            item = await _db.QueryFirst<Item>(conn, query, "returning *");
            _logger.Information("Created {ItemId} in {GardenId}",
                item.Id, gardenId);
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
            _logger.Information($"Updated item {item.Id} ({item.ItemType}) for garden {item.GardenId} (new quantity: {item.Quantity})");
        }

        public async Task DeleteItem(long id)
        {
            var query = new Query("items").AsDelete().Where("id", id);
            await _db.ExecuteQuery(query);
            _logger.Information("Deleted item {ItemId}", id);
        }

        public async Task UseItem(Item item, IChaoWorldConnection? conn = null)
        {
            if (item.Quantity > 1)
            {
                item.Quantity -= 1;
                await UpdateItem(item);
            } else
            {
                await DeleteItem(item.Id);
            }
            _logger.Information($"Used item {item.Id} ({item.ItemType}) ({item.Quantity} remaining)");
        }

        public Task<Item?> GetMarketItems(long id)
        {
            var query = new Query("marketitems").Where("id", id);
            return _db.QueryFirst<Item?>(query);
        }

        public Task<MarketItem?> GetMarketItemByType(ItemBase.ItemTypes type)
        {
            var query = new Query("marketitems").Where("typeid", type);
            return _db.QueryFirst<MarketItem?>(query);
        }

        public async Task<MarketItem> AddMarketItem(MarketItem item, IChaoWorldConnection? conn = null)
        {
            var query = new Query("marketitems").AsInsert(new
            {
                categoryid = (int)item.ItemCategory,
                typeid = (int)item.ItemType,
                quantity = item.Quantity,
                price = item.Price
            });
            item = await _db.QueryFirst<MarketItem>(conn, query, "returning *");
            _logger.Information($"Listed {item.ItemType} (x{item.Quantity}) on the Black Market");
            return item;
        }

        public async Task UpdateMarketItem(MarketItem item, IChaoWorldConnection? conn = null)
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update marketitems
                set
                    quantity = {item.Quantity}
                where typeid = {(int)item.ItemType};
            "));
            _logger.Information($"Updated market item {(int)item.ItemType} ({item.ItemType}) (new quantity: {item.Quantity})");
        }

        public async Task DeleteMarketItem(ItemBase.ItemTypes type)
        {
            var query = new Query("marketitems").AsDelete().Where("typeid", (int)type);
            await _db.ExecuteQuery(query);
            _logger.Information($"Deleted market item {(int)type} ({type.GetDescription()})");
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
                await DeleteMarketItem(item.ItemType);
            }
            _logger.Information($"Item {(int)item.ItemType} ({item.ItemType}) was purchased ({item.Quantity} remaining)");
        }

        public async Task ClearMarketListings(IChaoWorldConnection? conn = null)
        {
            var query = new Query("marketitems").AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Cleared Black Market listings");
        }
    }
}