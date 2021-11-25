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
        public async Task<Expedition?> GetExpeditionById(int id)
        {
            var query = new Query("expeditions").Where("id", id);
            return await _db.QueryFirst<Expedition?>(query);
        }

        public async Task<Expedition?> GetExpeditionByInstanceId(long id)
        {
            var query = new Query("expeditions").Join("expeditioninstances", "expeditions.id", "expeditioninstances.expeditionid")
                .Where("expeditioninstances.id", "=", id).Select("expeditions.*");
            return await _db.QueryFirst<Expedition?>(query);
        }

        public async Task<ExpeditionInstance?> GetExpeditionInstanceById(long id)
        {
            var query = new Query("expeditioninstances").Where("id", id);
            return await _db.QueryFirst<ExpeditionInstance?>(query);
        }

        public async Task<ExpeditionInstance?> GetJoinableInstanceForExpeditionId(int id)
        {
            var query = new Query("expeditioninstances")
                .Where("expeditionid", id)
                .Where("state", "!=", (int)ExpeditionInstance.ExpeditionStates.Completed)
                .Where("state", "!=", (int)ExpeditionInstance.ExpeditionStates.Canceled)
                .OrderByDesc("id");
            return await _db.QueryFirst<ExpeditionInstance?>(query);
        }

        public async Task<ExpeditionInstance?> GetExpeditionInstanceByName(string name)
        {
            var query = new Query("expeditioninstances").Join("expeditions", "expeditions.id", "expeditioninstances.expeditionid", "=").WhereRaw(
                "lower(expeditions.name) = lower(?)",
                name.ToLower().Replace("\"", string.Empty))
            .Select("expeditioninstances.*")
            .OrderByDesc("expeditioninstances.id")
            .Limit(1);
            return await _db.QueryFirst<ExpeditionInstance?>(query);
        }

        public async Task<ExpeditionInstance?> GetExpeditionInstanceByNameWithFuzzyMatching(string name)
        {
            var query = new Query("expeditioninstances").Join("expeditions", "expeditions.id", "expeditioninstances.expeditionid", "=")
                .Select("expeditioninstances.*")
                .OrderByRaw("similarity(expeditions.name, lower(?)) desc, expeditioninstances.id desc", name.ToLower().Replace("\"", string.Empty))
                .Limit(1);
            return await _db.QueryFirst<ExpeditionInstance?>(query);
        }

        public async Task<IEnumerable<ExpeditionInstance>> GetExpiredExpeditionInstances()
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var query = new Query("expeditioninstances")
                .Where("expeditioninstances.state", "!=", (int)ExpeditionInstance.ExpeditionStates.Completed)
                .Where("expeditioninstances.state", "!=", (int)ExpeditionInstance.ExpeditionStates.Canceled)
                .Where("expeditioninstances.expireson", "<", now);
            return await _db.Query<ExpeditionInstance>(query);
        }

        public async Task<int> GetExpeditionInstanceChaoCount(long instanceId)
        {
            var query = new Query("expeditioninstancechao").Where("expeditioninstanceid", instanceId).SelectRaw("count(chaoid)");
            return await _db.QueryFirst<int>(query);
        }

        public async Task<ExpeditionInstance?> GetActiveExpeditionByChao(long chaoId)
        {
            var query = new Query("expeditioninstances")
                .Join("expeditioninstancechao", "expeditioninstances.id", "expeditioninstancechao.expeditioninstanceid")
                .Where("expeditioninstancechao.chaoid", "=", chaoId)
                .Where("expeditioninstances.state", "!=", (int)ExpeditionInstance.ExpeditionStates.Completed)
                .Where("expeditioninstances.state", "!=", (int)ExpeditionInstance.ExpeditionStates.Canceled)
                .Select("expeditioninstances.*");
            return await _db.QueryFirst<ExpeditionInstance?>(query);
        }

        public async Task<ExpeditionInstance> CreateExpeditionInstance(Expedition expedition, int leaderId, IChaoWorldConnection? conn = null)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var expiresIn = Duration.FromMinutes(expedition.MaxDurationMinutes);
            var expiresOn = now.Plus(expiresIn);
            var query = new Query("expeditioninstances").AsInsert(new
            {
                expeditionid = expedition.Id,
                leaderid = leaderId,
                state = ExpeditionInstance.ExpeditionStates.New,
                createdon = now,
                expireson = expiresOn
            });
            var instance = await _db.QueryFirst<ExpeditionInstance>(conn, query, "returning *");
            _logger.Information($"Created instance {instance.Id} of expedition {expedition.Id} ({expedition.Name}) for garden {leaderId}");
            return instance;
        }

        public async Task DeleteExpeditionInstance(ExpeditionInstance instance, IChaoWorldConnection? conn = null)
        {
            var query = new Query("expeditioninstances").Where("id", instance.Id).AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Deleted instance {instance.Id} of expedition {instance.ExpeditionId}");
        }

        public async Task<ExpeditionInstance> UpdateExpeditionInstance(ExpeditionInstance instance, IChaoWorldConnection? conn = null)
        {
            var query = new Query("raceinstances").Where("id", instance.Id).AsUpdate(new
            {
                state = instance.State,
                completedon = instance.CompletedOn,
                mvpchaoid = instance.MVPChaoId,
                timeelapsedseconds = instance.TimeElapsedSeconds,
                totalcontribution = instance.TotalContribution
            });
            var updatedInstance = await _db.QueryFirst<ExpeditionInstance>(query, extraSql: "returning *");
            _logger.Information($"Updated instance {updatedInstance.Id} of expedition {updatedInstance.ExpeditionId}");
            return updatedInstance;
        }
        
        /*
         * TODO: Figure out how we should adjust this
         * 
        public async Task RecalculateExpeditionRewards()
        {
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update expeditions r
                set prizerings = coalesce((
	                select floor(avg(timeelapsedseconds + 300 + (100 * (r.difficulty-1)) + r.readydelayminutes*60.0)/1.5)
	                from raceinstances i
	                join chao c
	                on i.winnerchaoid = c.id
	                where i.raceid = r.id
	                and c.gardenid != 0
                ), 100)
            "));
            _logger.Information($"Updated prize amounts for races");
        }
        */

        public async Task GiveExpeditionRewards(ExpeditionInstance instance, int prizeRings)
        {
            // Only the highest contributor gets the normal prize amount, but everyone else still gets a participation prize
            await _db.Execute(conn => conn.QueryAsync<int>($@"
                update gardens g
                set ringbalance = (
		                case when i.mvpchaoid = c.id then ringbalance + {prizeRings}
                            else ringbalance + {prizeRings/10}
		                end
	                ),
                    instancelimit = instancelimit - 1
                from expeditioninstances i
                join expeditioninstancechao ric
                on i.id = ric.expeditioninstanceid
                join chao c
                on ric.chaoid = c.id
                where i.id = {instance.Id}
                and g.id = c.gardenid
            "));
            _logger.Information($"Delivered base prize of {prizeRings} rings for instance {instance.Id} of expedition {instance.ExpeditionId}");
        }

        public async Task JoinChaoToExpeditionInstance(ExpeditionInstance instance, Chao chao, IChaoWorldConnection? conn = null)
        {
            var query = new Query("expeditioninstancechao").AsInsert(new
            {
                expeditioninstanceid = instance.Id,
                chaoid = chao.Id.Value
            });
            await _db.QueryFirst<ExpeditionInstance>(conn, query, "returning *");
            _logger.Information($"Chao {chao.Id} joined instance {instance.Id} of expedition {instance.ExpeditionId}");
        }

        public async Task RemoveChaoFromExpeditionInstance(ExpeditionInstance instance, Chao chao)
        {
            var query = new Query("expeditioninstancechao")
                .Where("expeditionid", instance.Id)
                .Where("chaoid", chao.Id.Value)
                .AsDelete();
            await _db.ExecuteQuery(query);
            _logger.Information($"Chao {chao.Id} left instance {instance.Id} of expedition {instance.ExpeditionId}");
        }

        public Task<IEnumerable<Chao>> GetExpeditionInstanceChao(ExpeditionInstance instance)
        {
            var query = new Query("expeditioninstancechao").Join("chao", "expeditioninstancechao.chaoid", "chao.id")
                .Where("expeditioninstancechao.expeditioninstanceid", instance.Id)
                .Select("chao.*");
            return _db.Query<Chao>(query);
        }

        public async Task<ChaoRaceStats> GetExpeditionStats(long chaoId)
        {
            var stats = await _db.Execute(async conn => await conn.QueryFirstOrDefaultAsync<ChaoRaceStats>($@"
                select r.chaoid,
                    count(r.chaoid) totalraces,
                    sum(case when i.winnerchaoid = r.chaoid then 1 else 0 end) totalwins,
                    sum(case when r.state = 2 then 1 else 0 end) totalretires
                from raceinstancechao r
                join raceinstances i
                on r.raceinstanceid = i.id
                where chaoid = {chaoId}
                group by chaoid
            "));
            return stats;
        }

        public async Task UpdateExpeditionPingSetting(ulong accountId, bool allowPings)
        {
            await _db.Execute(conn => conn.QueryAsync(@$"
                update accounts
                set enableexpeditionpings = {allowPings}
                where uid = {accountId}
            "));
            _logger.Information($"Updated account settings for {accountId} (enable expedition pings: {allowPings}");
        }

        public async Task<IEnumerable<ulong>> GetAccountsToPingForExpedition(long instanceId)
        {
            return await _db.Execute(conn => conn.QueryAsync<ulong>(@$"
                    select a.uid
                    from expeditioninstancechao eic
                    join chao c
                    on eic.chaoid = c.id
                    join accounts a
                    on c.gardenid = a.gardenid
                    where eic.expeditioninstanceid = {instanceId}
                    and c.gardenid > 0
                    and enableexpeditionpings = true
                "));
        }
    }
}