using System.Threading;
using System.Threading.Tasks;

using Dapper;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public async Task UpdateStats()
        {
            await _db.Execute(conn => conn.ExecuteAsync("update info set garden_count = (select count(*) from gardens)"));
            await _db.Execute(conn => conn.ExecuteAsync("update info set chao_count = (select count(*) from chao)"));
        }

        public Task<Counts> GetStats()
            => _db.Execute(conn => conn.QuerySingleAsync<Counts>("select * from info"));

        public async Task<int> GetJackpot()
        {
            return await _db.Execute(conn => conn.QuerySingleAsync<int>("select jackpotbalance from casino limit 1"));
        }

        public async Task UpdateJackpot(int amount)
        {
            await _db.Execute(conn => conn.ExecuteAsync($"update casino set jackpotbalance = jackpotbalance + {amount}"));
        }

        public async Task ResetJackpot()
        {
            await _db.Execute(conn => conn.ExecuteAsync($"update casino set jackpotbalance = 30000"));
        }

        public class Counts
        {
            public int GardenCount { get; }
            public int ChaoCount { get; }
        }
    }
}